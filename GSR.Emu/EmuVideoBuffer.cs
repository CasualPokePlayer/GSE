// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;

namespace GSR.Emu;

public readonly ref struct EmuVideoBuffer(ReadOnlySpan<uint> VideoBuffer, int Width, int Height)
{
	public readonly ReadOnlySpan<uint> VideoBuffer = VideoBuffer;
	public readonly int Width = Width;
	public readonly int Height = Height;
	public readonly int Pitch = Width * sizeof(uint);
}
