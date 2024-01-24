using System;

namespace GSR.Emu;

public interface IAudioDispatcher
{
	void DispatchAudio(ReadOnlySpan<short> samples);
}
