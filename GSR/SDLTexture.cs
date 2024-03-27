// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Runtime.InteropServices;

using static SDL2.SDL;

using GSR.Emu;

namespace GSR;

/// <summary>
/// Wraps an SDL texture and caches its various state
/// Currently, the texture is assumed to be SDL_PIXELFORMAT_ARGB8888
/// </summary>
public sealed class SDLTexture(IntPtr sdlRenderer, SDL_TextureAccess textureAccess, SDL_ScaleMode scaleMode, SDL_BlendMode blendMode) : IDisposable
{
	public IntPtr Texture { get; private set; }
	public int Width { get; private set; }
	public int Height { get; private set; }

	public void SetVideoDimensions(int width, int height)
	{
		if (width != Width || height != Height)
		{
			Dispose();
			Texture = SDL_CreateTexture(sdlRenderer, SDL_PIXELFORMAT_ARGB8888, (int)textureAccess, width, height);
			if (Texture == IntPtr.Zero)
			{
				throw new($"Failed to create video texture, SDL error: {SDL_GetError()}");
			}

			if (SDL_SetTextureScaleMode(Texture, scaleMode) != 0)
			{
				throw new($"Failed to set texture scaling mode, SDL error: {SDL_GetError()}");
			}

			if (SDL_SetTextureBlendMode(Texture, blendMode) != 0)
			{
				throw new($"Failed to set texture blend mode, SDL error: {SDL_GetError()}");
			}

			Width = width;
			Height = height;
		}
	}

	public unsafe void SetEmuVideoBuffer(EmuVideoBuffer emuVideoBuffer)
	{
		SetVideoDimensions(emuVideoBuffer.Width, emuVideoBuffer.Height);

		if (SDL_LockTexture(Texture, IntPtr.Zero, out var pixels, out var pitch) != 0)
		{
			// this should never happen
			throw new($"Failed to lock SDL texture, SDL error {SDL_GetError()}");
		}

		if (pitch == emuVideoBuffer.Pitch) // identical pitch, fast case (probably always the case?)
		{
			emuVideoBuffer.VideoBuffer.CopyTo(new((void*)pixels, emuVideoBuffer.VideoBuffer.Length));
		}
		else // different pitch, slow case (indicates padding between lines)
		{
			var videoBufferAsBytes = MemoryMarshal.AsBytes(emuVideoBuffer.VideoBuffer);
			for (var i = 0; i < Height; i++)
			{
				videoBufferAsBytes.Slice(i * emuVideoBuffer.Pitch, emuVideoBuffer.Pitch)
					.CopyTo(new((void*)(pixels + i * pitch), emuVideoBuffer.Pitch));
			}
		}

		SDL_UnlockTexture(Texture);
	}

	public void Dispose()
	{
		if (Texture != IntPtr.Zero)
		{
			SDL_DestroyTexture(Texture);
			Texture = IntPtr.Zero;
		}
	}
}
