using System;

namespace GSR.Emu.Cores;

internal static class EmuCoreFactory
{
	public static IEmuCore CreateEmuCore(EmuLoadArgs loadArgs) => loadArgs.CoreType switch
	{
		EmuCoreType.Gambatte => new GambatteCore(loadArgs),
		EmuCoreType.mGBA => new MGBACore(loadArgs),
		_ => throw new InvalidOperationException()
	};
}
