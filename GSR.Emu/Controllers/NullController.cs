// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

namespace GSR.Emu.Controllers;

internal sealed class NullController : IEmuController
{
	public static readonly NullController Singleton = new();

	private NullController()
	{
	}

	public EmuControllerState GetState(bool immediateUpdate) => default;
}
