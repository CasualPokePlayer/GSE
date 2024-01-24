using System;

namespace GSR.Emu.Cores;

internal sealed class NullCore : IEmuCore
{
	public static readonly NullCore Singleton = new();

	private static readonly short[] _nullAudioBuffer = new short[48000 / 60 * 2];

	private NullCore()
	{
	}

	public void Dispose()
	{
	}

	public int ResetFadeoutCounter => 0;

	public void Advance(EmuControllerState controllerState, out bool completedFrame, out uint samples, out uint cpuCycles)
	{
		completedFrame = false;
		cpuCycles = samples = 48000 / 60;
	}

	// no need to actually provide these, these will never be used for this core
	public ReadOnlySpan<uint> VideoBuffer => ReadOnlySpan<uint>.Empty;
	public int VideoWidth => 0;
	public int VideoHeight => 0;

	// we still need to provide a dummy audio buffer however
	public ReadOnlySpan<short> AudioBuffer => _nullAudioBuffer.AsSpan();
	public int AudioFrequency => 48000;

	public uint CpuFrequency => 48000;
}
