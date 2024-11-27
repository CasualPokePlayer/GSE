// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;

using static GSE.Emu.Cores.MGBA;

namespace GSE.Emu.Cores;

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

	private readonly string _inputLogPath;
	private readonly string _romName;
	private readonly string _emuVersion;
	private readonly bool _disableGbaRtc;
	private EmuInputLog _emuInputLog;

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

	private void RestartInputLog(ReadOnlySpan<byte> state)
	{
		var saveDataLength = mgba_getsavedatalength(_opaque);
		if (state.IsEmpty && saveDataLength > 0)
		{
			mgba_savesavedata(_opaque, _savBuffer);
		}

		_emuInputLog?.Dispose();
		_emuInputLog = new(
			basePath: _inputLogPath,
			romName: _romName,
			emuVersion: _emuVersion,
			gbPlatform: GBPlatform.GBA,
			isGba: true,
			disableGbaRtc: _disableGbaRtc,
			gbRtcDividers: 0,
			startsFromSaveState: !state.IsEmpty,
			stateOrSaveFile: state.IsEmpty ? _savBuffer.AsSpan()[..saveDataLength] : state);
	}

	public void Dispose()
	{
		WriteSav();
		mgba_destroy(_opaque);
		_emuInputLog?.Dispose();
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
				using var sav = File.Create(_savPath);
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

		mgba_advance(_opaque, (Buttons)controllerState.GBAInputState, _videoBuffer, _audioBuffer, out var samplesRan, out var cpuCyclesRan);
		_emuInputLog.SubmitInput(cpuCyclesRan, controllerState.GBAInputState);
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
			RestartInputLog([]);
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
		RestartInputLog([]);
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

	private static readonly ImmutableArray<byte> _pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

	public bool LoadState(ReadOnlySpan<byte> state)
	{
		try
		{
			if (state[..8].SequenceEqual(_pngSignature.AsSpan()))
			{
				// upstream mGBA states might be in a PNG
				// our mGBA doesn't have libpng
				// so we need to manually extract the state out
				state = state[8..];
				byte[] mainState = null, extState = null;
				// we only care about savedata extdata
				const int EXTDATA_SAVEDATA = 2;
				while (mainState == null || extState == null)
				{
					var chunkLength = BinaryPrimitives.ReadUInt32BigEndian(state[..4]);
					var chunkType = state.Slice(4, 4);
					if (mainState == null && chunkType.SequenceEqual("gbAs"u8))
					{
						using var compressedStateStream = new MemoryStream(
							state.Slice(8, (int)chunkLength).ToArray(), writable: false);
						using var ds = new ZLibStream(compressedStateStream, CompressionMode.Decompress);
						using var ms = new MemoryStream();
						ds.CopyTo(ms);
						mainState = ms.ToArray();
					}

					if (extState == null && chunkType.SequenceEqual("gbAx"u8))
					{
						var extStateTag = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(8, 4));
						if (extStateTag != EXTDATA_SAVEDATA)
						{
							continue;
						}

						using var compressedStateStream = new MemoryStream(
							state.Slice(8 + 4 + 4, (int)chunkLength - 4 - 4).ToArray(), writable: false);
						using var ds = new ZLibStream(compressedStateStream, CompressionMode.Decompress);
						using var ms = new MemoryStream();
						ds.CopyTo(ms);
						extState = ms.ToArray();
					}

					if (chunkType.SequenceEqual("IEND"u8))
					{
						break;
					}

					// the chunk length does not include the chunk length itself, chunk type, nor crc32
					state = state[(8 + (int)chunkLength + 4)..];
				}

				if (mainState == null)
				{
					throw new("Failed to find savestate in PNG");
				}

				// terminating ext state header
				Span<byte> extNoneStateHeader = stackalloc byte[4 + 4 + 8];
				extNoneStateHeader.Clear();

				if (extState == null)
				{
					state = (byte[])[..mainState, ..extNoneStateHeader];
				}
				else
				{
					Span<byte> extStateHeader = stackalloc byte[4 + 4 + 8];
					BinaryPrimitives.WriteInt32LittleEndian(extStateHeader[..4], EXTDATA_SAVEDATA);
					BinaryPrimitives.WriteInt32LittleEndian(extStateHeader.Slice(4, 4), extState.Length);
					BinaryPrimitives.WriteInt64LittleEndian(extStateHeader.Slice(4 + 4, 8), mainState.Length + extStateHeader.Length);
					state = (byte[])[..mainState, ..extStateHeader, ..extState, ..extNoneStateHeader];
				}
			}
		}
		catch (Exception e)
		{
			Console.Error.WriteLine(e);
			return false;
		}

		var success = mgba_loadstate(_opaque, state, state.Length);
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
