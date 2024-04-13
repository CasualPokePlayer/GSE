// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.IO;

using static GSR.Emu.Cores.MGBA;

namespace GSR.Emu.Cores;

internal sealed class MGBACore : IEmuCore
{
	private readonly nint _opaque;
	private readonly uint[] _videoBuffer = new uint[240 * 160];
	private readonly short[] _audioBuffer = new short[1024 * 2];
	private readonly byte[] _savBuffer = new byte[0x20000 + 16];
	private readonly string _savPath;
	private readonly Action _resetCallback;

	private byte[] _stateBuffer = [];
	private bool _resetPressed;

	public MGBACore(EmuLoadArgs loadArgs)
	{
		_opaque = mgba_create(loadArgs.RomData.Span, loadArgs.RomData.Length, loadArgs.BiosData.Span, loadArgs.BiosData.Length, loadArgs.DisableGbaRtc);
		if (_opaque == 0)
		{
			throw new("Failed to create core opaque state!");
		}

		try
		{
			mgba_setcolorlut(_opaque,
				loadArgs.ApplyColorCorrection ? GBColors.GetLut(GBPlatform.GBA) : GBColors.TrueColorLut);

			var savPath = Path.Combine(loadArgs.SaveFilePath, loadArgs.RomName) + ".sav";
			var savFi = new FileInfo(savPath);
			if (savFi.Exists)
			{
				using var sav = savFi.OpenRead();
				mgba_savesavedata(_opaque, _savBuffer);
				sav.Read(_savBuffer);
				mgba_loadsavedata(_opaque, _savBuffer);
			}

			_savPath = savPath;
			_resetCallback = loadArgs.HardResetCallback;
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	public void Dispose()
	{
		WriteSav();
		mgba_destroy(_opaque);
	}

	private void WriteSav()
	{
		try
		{
			if (_savPath == null)
			{
				return;
			}

			var saveDataLength = mgba_getsavedatalength(_opaque);
			if (saveDataLength > 0)
			{
				using var sav = File.OpenWrite(_savPath);
				mgba_savesavedata(_opaque, _savBuffer);
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
		WriteSav();
		mgba_reset(_opaque);
		_resetCallback();
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

		mgba_advance(_opaque, (Buttons)controllerState.GBAInputState, _videoBuffer, _audioBuffer, out var samplesRan, out var cpuCyclesRan);
		completedFrame = true;
		samples = samplesRan;
		cpuCycles = cpuCyclesRan;
	}

	public bool LoadSave(ReadOnlySpan<byte> sav)
	{
		var saveDataLength = mgba_getsavedatalength(_opaque);
		if (saveDataLength == 0)
		{
			return false;
		}

		if (sav.Length >= saveDataLength)
		{
			// if we're large enough, we can send the buffer in directly
			mgba_loadsavedata(_opaque, sav);
			DoReset();
			return true;
		}

		var savBuffer = _savBuffer.AsSpan();
		// make sure we don't trash RTC/etc state
		var footerLength = saveDataLength & 0xFF;
		if (footerLength != 0)
		{
			// update the footer
			mgba_savesavedata(_opaque, savBuffer);
			sav.CopyTo(savBuffer);
			var remainingSaveLength = saveDataLength - sav.Length - footerLength;
			if (remainingSaveLength > 0)
			{
				savBuffer.Slice(sav.Length, remainingSaveLength).Fill(0xFF);
			}
		}
		else
		{
			sav.CopyTo(savBuffer);
			savBuffer[sav.Length..saveDataLength].Fill(0xFF);
		}

		mgba_loadsavedata(_opaque, savBuffer);
		DoReset();
		return true;
	}

	public ReadOnlySpan<byte> SaveState()
	{
		var stateSize = mgba_getsavestatelength(_opaque);
		if (_stateBuffer.Length < stateSize)
		{
			_stateBuffer = new byte[stateSize];
		}

		if (!mgba_savestate(_opaque, _stateBuffer))
		{
			throw new("Failed to create a savestate!");
		}

		return _stateBuffer.AsSpan()[..stateSize];
	}

	public bool LoadState(ReadOnlySpan<byte> state)
	{
		return mgba_loadstate(_opaque, state, state.Length);
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
			mgba_getmemoryblock(_opaque, block, out ptr, out len);
		}
	}

	public void SetColorCorrectionEnable(bool enable)
	{
		mgba_setcolorlut(_opaque, enable ? GBColors.GetLut(GBPlatform.GBA) : GBColors.TrueColorLut);
	}

	public ReadOnlySpan<uint> VideoBuffer => _videoBuffer;
	public int VideoWidth => 240;
	public int VideoHeight => 160;

	public ReadOnlySpan<short> AudioBuffer => _audioBuffer;
	public int AudioFrequency => 32768;

	public uint CpuFrequency => 16777216;
}
