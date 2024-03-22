// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

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
}

public sealed record EmuLoadArgs(
	EmuCoreType CoreType,
	IEmuController EmuController,
	ReadOnlyMemory<byte> RomData,
	ReadOnlyMemory<byte> BiosData,
	string RomName,
	string SaveFilePath,
	string SaveStatePath,
	Action HardResetCallback,
	GBPlatform GbPlatform,
	bool ApplyColorCorrection,
	bool DisableGbaRtc
);
