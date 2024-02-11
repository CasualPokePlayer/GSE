using System;

namespace GSR.Emu.Cores;

internal interface IEmuCore : IDisposable
{
	void Advance(EmuControllerState controllerState, out bool completedFrame, out uint samples, out uint cpuCycles);
	ReadOnlySpan<byte> SaveState();
	bool LoadState(ReadOnlySpan<byte> state);
	void SetColorCorrectionEnable(bool enable);

	ReadOnlySpan<uint> VideoBuffer { get; }
	int VideoWidth { get; }
	int VideoHeight { get; }

	ReadOnlySpan<short> AudioBuffer { get; }
	int AudioFrequency { get; }

	uint CpuFrequency { get; }
}
