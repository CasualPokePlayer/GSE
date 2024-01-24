using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using static GSR.Emu.Cores.Gambatte;

namespace GSR.Emu.Cores;

internal sealed class GambatteCore : IEmuCore
{
	private readonly IntPtr _opaque;
	private readonly uint[] _videoBuffer = new uint[160 * 144];
	private readonly uint[] _audioBuffer = new uint[35112 + 2064];

	private Buttons CurrentButtons;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static Buttons InputGetter(IntPtr userdata)
	{
		var core = (GambatteCore)GCHandle.FromIntPtr(userdata).Target!;
		return core.CurrentButtons;
	}

	public GambatteCore(byte[] romBuffer, byte[] biosBuffer)
	{
		_opaque = gambatte_create();
		if (_opaque == IntPtr.Zero)
		{
			throw new("Failed to create core opaque state!");
		}

		try
		{
			// TODO: Don't hardcode CGB/GBA mode, non-PSR runners might not want this
			const LoadFlags loadFlags = LoadFlags.CGB_MODE | LoadFlags.GBA_FLAG | LoadFlags.READONLY_SAV;
			var loadRes = gambatte_loadbuf(_opaque, romBuffer, (uint)romBuffer.Length, loadFlags);
			if (loadRes != 0)
			{
				throw new($"Failed to load ROM! Core returned {loadRes}");
			}

			loadRes = gambatte_loadbiosbuf(_opaque, biosBuffer, (uint)biosBuffer.Length);
			if (loadRes != 0)
			{
				throw new($"Failed to load BIOS! Core returned {loadRes}");
			}

			unsafe
			{
				var handle = GCHandle.Alloc(this, GCHandleType.Weak);
				gambatte_setinputgetter(_opaque, &InputGetter, GCHandle.ToIntPtr(handle));
			}
		}
		catch
		{
			Dispose();
			throw;
		}	
	}

	public void Dispose()
	{
		gambatte_destroy(_opaque);
	}

	private volatile int _resetFadeoutCounter;

	public int ResetFadeoutCounter => _resetFadeoutCounter;

	public void Advance(EmuControllerState controllerState, out bool completedFrame, out uint samples, out uint cpuCycles)
	{
		var resetFadeoutCounter = _resetFadeoutCounter;
		if (resetFadeoutCounter < 0)
		{
			gambatte_reset(_opaque, 101 * (2 << 14));
			resetFadeoutCounter = 0;
		}

		if (controllerState.HardReset && resetFadeoutCounter == 0)
		{
			resetFadeoutCounter = 1234567 + RandomNumberGenerator.GetInt32(35112);
		}

		CurrentButtons = (Buttons)controllerState.GBInputState;
		var samplesToRun = 35112u;
		if (resetFadeoutCounter > 0 && resetFadeoutCounter < samplesToRun)
		{
			samplesToRun = (uint)resetFadeoutCounter;
		}

		var frameCompletedSample = gambatte_runfor(_opaque, _videoBuffer, 160, _audioBuffer, ref samplesToRun);
		completedFrame = frameCompletedSample != -1;
		samples = samplesToRun;
		cpuCycles = samplesToRun; // maybe frameCompletedSample might be better?

		if (resetFadeoutCounter > 0)
		{
			resetFadeoutCounter -= (int)samplesToRun;
			_resetFadeoutCounter = resetFadeoutCounter;
		}
	}

	public ReadOnlySpan<uint> VideoBuffer => _videoBuffer.AsSpan();
	public int VideoWidth => 160;
	public int VideoHeight => 144;

	public ReadOnlySpan<short> AudioBuffer => MemoryMarshal.Cast<uint, short>(_audioBuffer.AsSpan());
	public int AudioFrequency => 2097152;

	public uint CpuFrequency => 2097152;
}
