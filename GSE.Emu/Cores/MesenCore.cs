// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.IO;

using static GSE.Emu.Cores.Mesen;

namespace GSE.Emu.Cores;

internal sealed class MesenCore : IEmuCore
{
	private readonly nint _opaque;
	private readonly uint[] _videoBuffer = new uint[240 * 160];
	private readonly short[] _audioBuffer = new short[0x2000 * 2];
	private readonly byte[] _savBuffer = new byte[0x20000 + 19];
	private readonly string _savPath;
	private readonly Action _resetCallback;

	private byte[] _stateBuffer = [];
	private bool _resetPressed;

	private readonly string _inputLogPath;
	private readonly string _romName;
	private readonly string _emuVersion;
	private readonly bool _disableGbaRtc;
	private EmuInputLog _emuInputLog;

	public MesenCore(EmuLoadArgs loadArgs)
	{
		var gbaStartTime = GetUnixTime();
		_opaque = mesen_create(loadArgs.RomData.Span, loadArgs.RomData.Length, loadArgs.BiosData.Span, loadArgs.BiosData.Length, loadArgs.DisableGbaRtc, gbaStartTime);
		if (_opaque == 0)
		{
			throw new("Failed to create core opaque state!");
		}

		try
		{
			mesen_setcolorlut(_opaque,
				loadArgs.ApplyColorCorrection ? GBColors.GetLut(GBPlatform.GBA) : GBColors.TrueColorLut);

			var savPath = Path.Combine(loadArgs.SaveFilePath, loadArgs.RomName) + ".sav";
			var savFi = new FileInfo(savPath);
			if (savFi.Exists)
			{
				using var sav = savFi.OpenRead();
				var numRead = sav.Read(_savBuffer);
				mesen_loadsavedata(_opaque, _savBuffer, numRead, gbaStartTime);
			}

			_savPath = savPath;
			_resetCallback = loadArgs.HardResetCallback;

			_inputLogPath = loadArgs.InputLogPath;
			_romName = loadArgs.RomName;
			_emuVersion = loadArgs.EmuVersion;
			_disableGbaRtc = loadArgs.DisableGbaRtc;
			RestartInputLog([]);
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	private static long GetUnixTime()
	{
		return (long)(DateTime.Now - DateTime.UnixEpoch).TotalSeconds;
	}

	private void RestartInputLog(ReadOnlySpan<byte> state)
	{
		var saveDataLength = 0;
		if (state.IsEmpty)
		{
			saveDataLength = mesen_savesavedata(_opaque, _savBuffer);
		}

		_emuInputLog?.Dispose();
		_emuInputLog = new(
			basePath: _inputLogPath,
			romName: _romName,
			emuVersion: _emuVersion,
			gbPlatform: GBPlatform.GBA,
			isGba: true,
			disableGbaRtc: _disableGbaRtc,
			gbaRtcTime: mesen_getrtctime(_opaque),
			gbRtcDividers: 0,
			startsFromSaveState: !state.IsEmpty,
			stateOrSaveFile: state.IsEmpty ? _savBuffer.AsSpan()[..saveDataLength] : state);
	}

	public void Dispose()
	{
		FlushSave();
		mesen_destroy(_opaque);
		_emuInputLog?.Dispose();
	}

	public void FlushSave()
	{
		try
		{
			if (_savPath == null)
			{
				return;
			}

			var saveDataLength = mesen_savesavedata(_opaque, _savBuffer);
			if (saveDataLength > 0)
			{
				using var sav = File.Create(_savPath);
				sav.Write(_savBuffer.AsSpan()[..saveDataLength]);
			}
		}
		catch
		{
			// ignored
		}
	}

	private void DoReset()
	{
		FlushSave();
		mesen_reset(_opaque);
		_resetCallback();
		_emuInputLog.SubmitHardReset();
	}

	public void Advance(EmuControllerState controllerState, out bool completedFrame, out uint samples, out uint cpuCycles)
	{
		var pressingReset = controllerState.HardReset;
		var doReset = !_resetPressed && pressingReset;
		_resetPressed = pressingReset;

		if (doReset)
		{
			DoReset();
		}

		mesen_advance(_opaque, (Buttons)controllerState.GBAInputState, _videoBuffer, _audioBuffer, out var samplesRan, out var cpuCyclesRan);
		_emuInputLog.SubmitInput(cpuCyclesRan, controllerState.GBAInputState);
		completedFrame = true;
		samples = samplesRan;
		cpuCycles = cpuCyclesRan;
	}

	public bool LoadSave(ReadOnlySpan<byte> sav)
	{
		var saveDataLength = Math.Min(sav.Length, _savBuffer.Length);
		mesen_loadsavedata(_opaque, sav, saveDataLength, GetUnixTime());
		RestartInputLog([]);
		DoReset();
		return true;
	}

	public ReadOnlySpan<byte> SaveState()
	{
		var stateSize = mesen_getsavestatelength(_opaque);
		if (_stateBuffer.Length < stateSize)
		{
			_stateBuffer = new byte[stateSize];
		}

		if (!mesen_savestate(_opaque, _stateBuffer))
		{
			throw new("Failed to create a savestate!");
		}

		return _stateBuffer.AsSpan()[..stateSize];
	}

	public bool LoadState(ReadOnlySpan<byte> state)
	{
		var success = mesen_loadstate(_opaque, state, state.Length);
		if (success)
		{
			RestartInputLog(state);
		}

		return success;
	}

	public void GetMemoryExport(ExportHelper.MemExport which, out nint ptr, out nuint len)
	{
		var block = which switch
		{
			ExportHelper.MemExport.GBA_IWRAM => MemoryBlocks.IWRAM,
			ExportHelper.MemExport.GBA_EWRAM => MemoryBlocks.EWRAM,
			ExportHelper.MemExport.GBA_SRAM => MemoryBlocks.SRAM,
			_ => MemoryBlocks.END
		};

		if (block == MemoryBlocks.END)
		{
			ptr = 0;
			len = 0;
		}
		else
		{
			mesen_getmemoryblock(_opaque, block, out ptr, out len);
		}
	}

	public void SetColorCorrectionEnable(bool enable)
	{
		mesen_setcolorlut(_opaque, enable ? GBColors.GetLut(GBPlatform.GBA) : GBColors.TrueColorLut);
	}

	public ReadOnlySpan<uint> VideoBuffer => _videoBuffer;
	public int VideoWidth => 240;
	public int VideoHeight => 160;

	public ReadOnlySpan<short> AudioBuffer => _audioBuffer;
	public int AudioFrequency => 48000;

	public uint CpuFrequency => 16777216;
}
