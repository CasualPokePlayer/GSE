// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

namespace GSE.Emu;

/// <summary>
/// Helper ref struct for temporary emu pausing in an RAII fashion
/// This won't unpause in the end if the emu was already paused
/// </summary>
/// <param name="emuManager">Emu manager to pause/unpause</param>
public readonly ref struct EmuPause(EmuManager emuManager)
{
	private readonly bool _didPause = emuManager.Pause();

	public void Dispose()
	{
		if (_didPause)
		{
			emuManager.Unpause();
		}
	}
}
