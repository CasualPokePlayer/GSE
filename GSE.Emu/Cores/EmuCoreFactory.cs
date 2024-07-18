// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;

namespace GSE.Emu.Cores;

internal static class EmuCoreFactory
{
	public static IEmuCore CreateEmuCore(EmuLoadArgs loadArgs) => loadArgs.CoreType switch
	{
		EmuCoreType.Gambatte => new GambatteCore(loadArgs),
		EmuCoreType.mGBA => new MGBACore(loadArgs),
		_ => throw new InvalidOperationException()
	};
}
