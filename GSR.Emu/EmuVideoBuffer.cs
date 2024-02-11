using System;

namespace GSR.Emu;

public readonly ref struct EmuVideoBuffer(ReadOnlySpan<uint> VideoBuffer, int Width, int Height)
{
	public readonly ReadOnlySpan<uint> VideoBuffer = VideoBuffer;
	public readonly int Width = Width;
	public readonly int Height = Height;
	public readonly int Pitch = Width * sizeof(uint);
}
