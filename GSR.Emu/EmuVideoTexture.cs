using System;
using System.Runtime.InteropServices;

using static SDL2.SDL;

namespace GSR.Emu;

internal sealed class EmuVideoTexture : IDisposable
{
	public readonly IntPtr Texture;
	private readonly int _width;
	private readonly int _height;

	public EmuVideoTexture(IntPtr sdlRenderer, int width, int height)
	{
		Texture = SDL_CreateTexture(sdlRenderer, SDL_PIXELFORMAT_ABGR8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, width, height);
		if (Texture == IntPtr.Zero)
		{
			throw new($"Failed to create video texture, SDL error: {SDL_GetError()}");
		}

		_width = width;
		_height = height;
	}

	public unsafe void DrawVideo(ReadOnlySpan<uint> videoBuffer)
	{
		if (SDL_LockTexture(Texture, IntPtr.Zero, out var pixels, out var pitch) != 0)
		{
			// this should never happen
			throw new($"Failed to lock SDL texture, SDL error {SDL_GetError()}");
		}

		if (pitch == _width * 4) // pitch == Width * 4, fast case (probably always the case?)
		{
			videoBuffer.CopyTo(new((void*)pixels, videoBuffer.Length));
		}
		else // pitch != Width * 4, slow case (indicates padding between lines)
		{
			var videoBufferAsBytes = MemoryMarshal.AsBytes(videoBuffer);
			var videoBufferPitch = _width * 4;
			for (var i = 0; i < _height; i++)
			{
				videoBufferAsBytes.Slice(i * videoBufferPitch, videoBufferPitch)
					.CopyTo(new((void*)(pixels + i * pitch), videoBufferPitch));
			}
		}

		SDL_UnlockTexture(Texture);
	}

	public void Dispose()
	{
		SDL_DestroyTexture(Texture);
	}
}
