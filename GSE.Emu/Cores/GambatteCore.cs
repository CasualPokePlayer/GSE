// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static GSE.Emu.Cores.Gambatte;

namespace GSE.Emu.Cores;

internal sealed class GambatteCore : IEmuCore
{
	private readonly nint _opaque;
	private readonly GBPlatform _gbPlatform;

	private readonly uint[] _videoBuffer;
	private readonly bool _hasSgbBorder;
	private readonly uint _gbVideoOffset;

	private readonly uint[] _audioBuffer = new uint[35112 + 2064];
	// blank buffer for resetting (i.e. fadeout would cut off audio while the game is still running)
	private readonly short[] _resetAudioBuffer = new short[(35112 + 2064) * 2];

	private GCHandle _inputGetterUserData;

	private readonly byte[] _stateBuffer;

	private readonly byte[] _savBuffer;
	private readonly string _savPath;

	private readonly string _inputLogPath;
	private readonly string _romName;
	private readonly string _emuVersion;
	private EmuInputLog _emuInputLog;

	private enum ResetStage
	{
		None,
		Fadeout,
		Stall,
	}

	private readonly Action _resetCallback;
	private readonly int _resetFadeout;
	private readonly int _resetStall;
	private ResetStage _resetStage;
	private int _resetCounter;
	private bool _resetPressed;

	private Buttons CurrentButtons;

	[UnmanagedCallersOnly(CallConvs = [ typeof(CallConvCdecl) ])]
	private static Buttons InputGetter(nint userdata)
	{
		var core = (GambatteCore)GCHandle.FromIntPtr(userdata).Target!;
		return core.CurrentButtons;
	}

	public GambatteCore(EmuLoadArgs loadArgs)
	{
		_opaque = gambatte_create();
		if (_opaque == 0)
		{
			throw new("Failed to create core opaque state!");
		}

		try
		{
			_gbPlatform = loadArgs.GbPlatform;

			if (_gbPlatform == GBPlatform.GBP)
			{
				_resetFadeout = 1234567;
			}

			_resetStall = _gbPlatform switch
			{
				GBPlatform.GBP => 101 * (2 << 14),
				GBPlatform.SGB2 => 128 * (2 << 14),
				_ => 0
			};

			if (_gbPlatform == GBPlatform.SGB2)
			{
				_hasSgbBorder = true;
				_gbVideoOffset = 256 * ((224 - 144) / 2) + (256 - 160) / 2;
				VideoWidth = 256;
				VideoHeight = 224;
			}
			else
			{
				VideoWidth = 160;
				VideoHeight = 144;
			}

			_videoBuffer = new uint[VideoWidth * VideoHeight];

			var loadFlags = _gbPlatform switch
			{
				GBPlatform.GB => LoadFlags.READONLY_SAV,
				GBPlatform.GBC => LoadFlags.CGB_MODE | LoadFlags.READONLY_SAV,
				GBPlatform.GBA or GBPlatform.GBP => LoadFlags.CGB_MODE | LoadFlags.GBA_FLAG | LoadFlags.READONLY_SAV,
				GBPlatform.SGB2 => LoadFlags.SGB_MODE | LoadFlags.READONLY_SAV,
				_ => throw new InvalidOperationException(),
			};

			var loadRes = gambatte_loadbuf(_opaque, loadArgs.RomData.Span, (uint)loadArgs.RomData.Length, loadFlags);
			if (loadRes != 0)
			{
				throw new($"Failed to load ROM! Core returned {loadRes}");
			}

			static ReadOnlySpan<byte> PatchBiosForGbcGba(ReadOnlySpan<byte> gbcBiosData)
			{
				var patchedBios = gbcBiosData.ToArray();
				patchedBios[0xF3] ^= 0x03;
				Buffer.BlockCopy(patchedBios, 0xF6, patchedBios, 0xF5, 0xFB - 0xF5);
				patchedBios[0xFB] ^= 0x74;
				return patchedBios;
			}

			var biosData = _gbPlatform switch
			{
				GBPlatform.GBA or GBPlatform.GBP => PatchBiosForGbcGba(loadArgs.BiosData.Span),
				_ => loadArgs.BiosData.Span
			};

			loadRes = gambatte_loadbiosbuf(_opaque, biosData, (uint)biosData.Length);
			if (loadRes != 0)
			{
				throw new($"Failed to load BIOS! Core returned {loadRes}");
			}

			_inputGetterUserData = GCHandle.Alloc(this, GCHandleType.Weak);

			unsafe
			{
				gambatte_setinputgetter(_opaque, &InputGetter, GCHandle.ToIntPtr(_inputGetterUserData));
			}

			gambatte_setcgbpalette(_opaque,
				loadArgs.ApplyColorCorrection ? GBColors.GetLut(_gbPlatform) : GBColors.TrueColorLut);

			_stateBuffer = new byte[gambatte_savestate(_opaque, [], 0, [])];

			var savPath = Path.Combine(loadArgs.SaveFilePath, loadArgs.RomName) + ".sav";
			var savBuffer = new byte[gambatte_getsavedatalength(_opaque)];
			if (savBuffer.Length > 0)
			{
				var savFi = new FileInfo(savPath);
				if (savFi.Exists)
				{
					using var sav = savFi.OpenRead();
					gambatte_savesavedata(_opaque, savBuffer);
					sav.Read(savBuffer);
					gambatte_loadsavedata(_opaque, savBuffer);
				}
			}

			_savBuffer = savBuffer;
			_savPath = savPath;

			_inputLogPath = loadArgs.InputLogPath;
			_romName = loadArgs.RomName;
			_emuVersion = loadArgs.EmuVersion;
			RestartInputLog([]);

			_resetCallback = loadArgs.HardResetCallback;
		}
		catch
		{
			Dispose();
			throw;
		}	
	}

	private void RestartInputLog(ReadOnlySpan<byte> state)
	{
		// kind of a hack to enforce movie determinism without an explicit initial time setting...
		var rtcDividers = 0UL;
		if (state.IsEmpty)
		{
			// save buffer needs to be up to date to send to the input log
			gambatte_savesavedata(_opaque, _savBuffer);
			if ((_savBuffer.Length & 0x1FF) != 0)
			{
				// Gambatte's RTC footer begins with 8 bytes for base time, ignore this
				var rtcDataLength = (_savBuffer.Length & 0x1FF) - 8;
				var rtcData = _savBuffer.AsSpan()[(_savBuffer.Length - rtcDataLength)..];
				switch (rtcData.Length)
				{
					// HuC3 RTC
					case 0x100 + 4:
					{
						// first 4 bytes are RTC cycles (runs double divider rate)
						rtcDividers = BinaryPrimitives.ReadUInt32BigEndian(rtcData[..4]) / 2;
						var minutes = (ulong)((rtcData[4 + 0x10] & 0x0F) | ((rtcData[4 + 0x11] & 0x0F) << 4) | ((rtcData[4 + 0x12] & 0x0F) << 8));
						var days = (ulong)((rtcData[4 + 0x13] & 0x0F) | ((rtcData[4 + 0x14] & 0x0F) << 4) | ((rtcData[4 + 0x15] & 0x0F) << 8));
						rtcDividers += minutes * 60 * GB_DIVIDERS_PER_SECOND;
						rtcDividers += days * 86400 * GB_DIVIDERS_PER_SECOND;
						break;
					}
					// MBC3 RTC
					case 14:
					{
						// RTC cycles (runs double divider rate)
						rtcDividers = BinaryPrimitives.ReadUInt32BigEndian(rtcData.Slice(5, 4)) / 2;
						// RTC seconds
						rtcDividers += rtcData[4] * GB_DIVIDERS_PER_SECOND;
						// RTC minutes
						rtcDividers += rtcData[3] * 60UL * GB_DIVIDERS_PER_SECOND;
						// RTC hours
						rtcDividers += rtcData[2] * 3600UL * GB_DIVIDERS_PER_SECOND;
						// RTC days
						var days = rtcData[1] | ((ulong)(rtcData[0] & 1) << 8);
						if ((rtcData[0] & 0x80) != 0)
						{
							// trigger RTC overflow
							days += 512;
						}

						rtcDividers += days * 86400UL * GB_DIVIDERS_PER_SECOND;
						break;
					}
					default:
						throw new InvalidOperationException("Unknown RTC data length");
				}

				gambatte_settime(_opaque, rtcDividers);
			}
		}

		_emuInputLog?.Dispose();
		_emuInputLog = new(
			basePath: _inputLogPath,
			romName: _romName,
			emuVersion: _emuVersion,
			gbPlatform: _gbPlatform,
			isGba: false,
			disableGbaRtc: false,
			gbRtcDividers: rtcDividers,
			startsFromSaveState: !state.IsEmpty,
			stateOrSaveFile: state.IsEmpty ? _savBuffer : state);
	}

	private void WriteSav()
	{
		try
		{
			if (_savBuffer is { Length: > 0 })
			{
				using var sav = File.Create(_savPath);
				gambatte_savesavedata(_opaque, _savBuffer);
				sav.Write(_savBuffer);
			}
		}
		catch
		{
			// ignored
		}
	}

	public void Dispose()
	{
		WriteSav();
		gambatte_destroy(_opaque);

		if (_inputGetterUserData.IsAllocated)
		{
			_inputGetterUserData.Free();
		}

		_emuInputLog?.Dispose();
	}

	private void DoReset()
	{
		WriteSav();
		gambatte_reset(_opaque, (uint)_resetStall);
		_resetStage = _resetStall == 0 ? ResetStage.None : ResetStage.Stall;
		_resetCounter = _resetStall;
		_emuInputLog.SubmitHardReset();
	}

	private void ResetStepPre(bool tryReset, ref uint samplesToRun)
	{
		switch (_resetStage)
		{
			case ResetStage.None:
			{
				if (tryReset)
				{
					if (_resetFadeout != 0)
					{
						_resetStage = ResetStage.Fadeout;
						_resetCounter = _resetFadeout + GSERandom.GetInt32(35112);
					}
					else
					{
						DoReset();
						_resetCallback();
					}
				}

				break;
			}
			case ResetStage.Fadeout:
			{
				var resetCounter = (uint)Math.Max(0, _resetCounter);
				samplesToRun = Math.Min(samplesToRun, resetCounter);
				break;
			}
			case ResetStage.Stall:
				// nothing to do here
				break;
			default:
				throw new InvalidOperationException();
		}
	}

	private void ResetStepPost(uint samplesRan, bool completedFrame)
	{
		if (_resetStage == ResetStage.None)
		{
			return;
		}

		_resetCounter -= (int)samplesRan;
		if (_resetCounter <= 0)
		{
			switch (_resetStage)
			{
				case ResetStage.Fadeout:
					DoReset();
					break;
				case ResetStage.Stall:
					_resetStage = ResetStage.None;
					_resetCounter = 0;
					_resetCallback();
					break;
				case ResetStage.None: // should not be reachable
				default:
					throw new InvalidOperationException();
			}
		}

		if (_resetStage != ResetStage.None && completedFrame)
		{
			// apply fadeout
			var alpha = 0.0;
			if (_resetStage == ResetStage.Fadeout)
			{
				// formula taken from original GSE
				var part = _resetFadeout / 9.0;
				alpha = (_resetCounter - part) / (_resetFadeout - 2 * part);
				alpha = Math.Clamp(alpha, 0, 1);
			}

			for (var i = 0; i < _videoBuffer.Length; i++)
			{
				var pixel = _videoBuffer[i];
				var r = (uint)((pixel >> 16 & 0xFF) * alpha);
				var g = (uint)((pixel >> 8 & 0xFF) * alpha);
				var b = (uint)((pixel & 0xFF) * alpha);
				_videoBuffer[i] = 0xFFu << 24 | r << 16 | g << 8 | b;
			}
		}
	}

	public unsafe void Advance(EmuControllerState controllerState, out bool completedFrame, out uint samples, out uint cpuCycles)
	{
		CurrentButtons = (Buttons)controllerState.GBInputState;

		// kind of sticky logic applied here
		// TODO: should this really be done?
		var pressingReset = controllerState.HardReset;
		var tryReset = !_resetPressed && pressingReset;
		_resetPressed = pressingReset;

		var samplesToRun = 35112u;
		ResetStepPre(tryReset, ref samplesToRun);

		_emuInputLog.SubmitInput(samplesToRun, controllerState.GBInputState);

		fixed (uint* videoBuffer = _videoBuffer)
		{
			var frameCompletedSample = gambatte_runfor(_opaque, videoBuffer + _gbVideoOffset, VideoWidth, _audioBuffer, ref samplesToRun);
			completedFrame = frameCompletedSample != -1;

			if (_hasSgbBorder && completedFrame)
			{
				_ = gambatte_updatescreenborder(_opaque, videoBuffer, VideoWidth);
			}
		}

		samples = samplesToRun;
		cpuCycles = samplesToRun;
		ResetStepPost(samplesToRun, completedFrame);
	}

	public bool LoadSave(ReadOnlySpan<byte> sav)
	{
		if (_resetStage != ResetStage.None || _savBuffer.Length == 0)
		{
			return false;
		}

		if (sav.Length >= _savBuffer.Length)
		{
			// if we're large enough, we can send the buffer in directly
			gambatte_loadsavedata(_opaque, sav);
			RestartInputLog([]);
			DoReset();
			if (_resetStage == ResetStage.None)
			{
				_resetCallback();
			}

			return true;
		}

		var savBuffer = _savBuffer.AsSpan();
		// make sure we don't trash RTC/etc state
		var footerLength = savBuffer.Length & 0x1FF;
		if (footerLength != 0)
		{
			// update the footer
			gambatte_savesavedata(_opaque, savBuffer);
			sav.CopyTo(savBuffer);
			var remainingSavLength = savBuffer.Length - sav.Length - footerLength;
			if (remainingSavLength > 0)
			{
				savBuffer.Slice(sav.Length, remainingSavLength).Fill(0xFF);
			}
		}
		else
		{
			sav.CopyTo(savBuffer);
			savBuffer[sav.Length..].Fill(0xFF);
		}

		gambatte_loadsavedata(_opaque, savBuffer);
		RestartInputLog([]);
		DoReset();
		if (_resetStage == ResetStage.None)
		{
			_resetCallback();
		}

		return true;
	}

	public ReadOnlySpan<byte> SaveState()
	{
		gambatte_savestate(_opaque, [], 0, _stateBuffer);
		return _stateBuffer;
	}

	public bool LoadState(ReadOnlySpan<byte> state)
	{
		// no loading a state while resetting!
		var success = _resetStage == ResetStage.None && gambatte_loadstate(_opaque, state, state.Length);
		if (success)
		{
			RestartInputLog(state);
		}

		return success;
	}

	public void GetMemoryExport(ExportHelper.MemExport which, out nint ptr, out nuint len)
	{
		var area = which switch
		{
			ExportHelper.MemExport.GB_WRAM => MemoryAreas.WRAM,
			ExportHelper.MemExport.GB_SRAM => MemoryAreas.CARTRAM,
			ExportHelper.MemExport.GB_HRAM => MemoryAreas.HRAM,
			_ => MemoryAreas.END
		};

		ptr = 0;
		len = 0;

		if (area != MemoryAreas.END && gambatte_getmemoryarea(_opaque, area, out var data, out var length))
		{
			ptr = data;
			len = (uint)length;
		}
	}

	public void SetColorCorrectionEnable(bool enable)
	{
		gambatte_setcgbpalette(_opaque, enable ? GBColors.GetLut(_gbPlatform) : GBColors.TrueColorLut);
	}

	public ReadOnlySpan<uint> VideoBuffer => _videoBuffer;
	public int VideoWidth { get; }
	public int VideoHeight { get; }

	public ReadOnlySpan<short> AudioBuffer => _resetStage == ResetStage.None ?  MemoryMarshal.Cast<uint, short>(_audioBuffer) : _resetAudioBuffer;
	public int AudioFrequency => 2097152;

	public uint CpuFrequency => 2097152;
}
