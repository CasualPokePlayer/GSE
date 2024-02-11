using System;
using System.IO;

namespace GSR.Emu.Cores;

internal static class EmuCoreFactory
{
	// TODO: probably need to use some firmware callback if this is expanded to melonDS
	// (Or perhaps a general file callback, handling rom/bios/initial save data, that'd also could be overloaded to act like a system selector)
	public static IEmuCore CreateEmuCore(EmuLoadArgs loadArgs) => loadArgs.CoreType switch
	{
		EmuCoreType.Gambatte => new GambatteCore(loadArgs),
		EmuCoreType.mGBA => new MGBACore(loadArgs),
		_ => throw new InvalidOperationException()
	};
}
