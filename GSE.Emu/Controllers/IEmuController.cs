// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

namespace GSE.Emu.Controllers;

public interface IEmuController
{
	EmuControllerState GetState(bool immediateUpdate);
}
