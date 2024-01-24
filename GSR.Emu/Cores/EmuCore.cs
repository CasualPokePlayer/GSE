using System;

namespace GSR.Emu.Cores;

public enum EmuCoreType
{
	Gambatte,
	mGBA,
}

internal interface IEmuCore : IDisposable
{
	int ResetFadeoutCounter { get; }
	void Advance(EmuControllerState controllerState, out bool completedFrame, out uint samples, out uint cpuCycles);

	ReadOnlySpan<uint> VideoBuffer { get; }
	int VideoWidth { get; }
	int VideoHeight { get; }

	ReadOnlySpan<short> AudioBuffer { get; }
	int AudioFrequency { get; }

	uint CpuFrequency { get; }
}

internal static class EmuCoreFactory
{
	// TODO: probably need to use some firmware callback if this is expanded to melonDS
	// (Or perhaps a general file callback, handling rom/bios/initial save data, that'd also could be overloaded to act like a system selector)
	public static IEmuCore CreateEmuCore(EmuCoreType coreType, byte[] romBuffer, byte[] biosBuffer) => coreType switch
	{
		EmuCoreType.Gambatte => new GambatteCore(romBuffer, biosBuffer),
		EmuCoreType.mGBA => new MGBACore(romBuffer, biosBuffer),
		_ => throw new InvalidOperationException()
	};
}
