using System;
using System.IO;

using static GSR.Emu.Cores.MGBA;
using static GSR.Emu.ExportHelper;

namespace GSR.Emu.Cores;

internal sealed class MGBACore : IEmuCore
{
	private readonly IntPtr _opaque;
	private readonly uint[] _videoBuffer = new uint[240 * 160];
	private readonly short[] _audioBuffer = new short[1024 * 2];
	private readonly byte[] _saveBuffer = new byte[0x20000 + 16];
	private readonly string _savPath;
	private readonly Action _resetCallback;

	private byte[] _stateBuffer = [];
	private bool _resetPressed;

	public MGBACore(EmuLoadArgs loadArgs)
	{
		_opaque = mgba_create(loadArgs.RomData.Span, loadArgs.RomData.Length, loadArgs.BiosData.Span, loadArgs.BiosData.Length, loadArgs.DisableGbaRtc);
		if (_opaque == IntPtr.Zero)
		{
			throw new("Failed to create core opaque state!");
		}

		try
		{
			mgba_setcolorlut(_opaque,
				loadArgs.ApplyColorCorrection ? GBColors.GetLut(GBPlatform.GBA) : GBColors.TrueColorLut);

			mgba_getmemoryblock(_opaque, MemoryBlocks.IWRAM, out var iwramPtr, out var iwramLen);
			mgba_getmemoryblock(_opaque, MemoryBlocks.EWRAM, out var ewramPtr, out var ewramLen);
			mgba_getmemoryblock(_opaque, MemoryBlocks.SRAM, out var sramPtr, out var sramLen);
			export_helper_set_mem_export(MemExportType.GBA_IWRAM, iwramPtr, iwramLen);
			export_helper_set_mem_export(MemExportType.GBA_EWRAM, ewramPtr, ewramLen);
			export_helper_set_mem_export(MemExportType.GBA_SRAM, sramPtr, sramLen);

			var savPath = Path.Combine(loadArgs.RomDirectory, loadArgs.RomName) + ".sav";
			var savFi = new FileInfo(savPath);
			if (savFi.Exists)
			{
				using var sav = savFi.OpenRead();
				mgba_savesavedata(_opaque, _saveBuffer);
				sav.Read(_saveBuffer);
				mgba_loadsavedata(_opaque, _saveBuffer);
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
				mgba_savesavedata(_opaque, _saveBuffer);
				sav.Write(_saveBuffer.AsSpan()[..saveDataLength]);
			}
		}
		catch
		{
			// ignored
		}
	}

	public void Advance(EmuControllerState controllerState, out bool completedFrame, out uint samples, out uint cpuCycles)
	{
		var pressingReset = controllerState.HardReset;
		var doReset = !_resetPressed && pressingReset;
		_resetPressed = pressingReset;

		if (doReset)
		{
			WriteSav();
			mgba_reset(_opaque);
			_resetCallback();
		}

		mgba_advance(_opaque, (Buttons)controllerState.GBAInputState, _videoBuffer, _audioBuffer, out var samplesRan, out var cpuCyclesRan);
		completedFrame = true;
		samples = samplesRan;
		cpuCycles = cpuCyclesRan;
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
