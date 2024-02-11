using System;

using GSR.Emu.Controllers;
using GSR.Emu.Cores;

namespace GSR.Emu;

public enum GBPlatform
{
	GB,
	GBC,
	GBA,
	GBP,
	SGB2,
};

public sealed record EmuLoadArgs(
	EmuCoreType CoreType,
	IEmuController EmuController,
	ReadOnlyMemory<byte> RomData,
	ReadOnlyMemory<byte> BiosData,
	string RomDirectory,
	string RomName,
	Action HardResetCallback,
	GBPlatform GbPlatform,
	bool ApplyColorCorrection,
	bool DisableGbaRtc
);
