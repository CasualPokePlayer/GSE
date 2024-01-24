using System;

using static GSR.Emu.Cores.MGBA;

namespace GSR.Emu.Cores;

internal sealed class MGBACore : IEmuCore
{
	private readonly IntPtr _opaque;
	private readonly uint[] _videoBuffer = new uint[240 * 160];
	private readonly short[] _audioBuffer = new short[1024 * 2];

	public MGBACore(byte[] romBuffer, byte[] biosBuffer)
	{
		_opaque = mgba_create(romBuffer, romBuffer.Length, biosBuffer, biosBuffer.Length);
		if (_opaque == IntPtr.Zero)
		{
			throw new("Failed to create core opaque state!");
		}
	}

	public void Dispose()
	{
		mgba_destroy(_opaque);
	}

	public int ResetFadeoutCounter => 0;

	public void Advance(EmuControllerState controllerState, out bool completedFrame, out uint samples, out uint cpuCycles)
	{
		if (controllerState.HardReset)
		{
			mgba_reset(_opaque);
		}

		mgba_advance(_opaque, (Buttons)controllerState.GBAInputState, _videoBuffer, _audioBuffer, out var samplesRan, out var cpuCyclesRan);
		completedFrame = true;
		samples = samplesRan;
		cpuCycles = cpuCyclesRan;
	}

	public ReadOnlySpan<uint> VideoBuffer => _videoBuffer.AsSpan();
	public int VideoWidth => 240;
	public int VideoHeight => 160;

	public ReadOnlySpan<short> AudioBuffer => _audioBuffer.AsSpan();
	public int AudioFrequency => 32768;

	public uint CpuFrequency => 16777216;
}
