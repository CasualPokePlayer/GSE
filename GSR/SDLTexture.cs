// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;

using static SDL2.SDL;

namespace GSR;

/// <summary>
/// Wraps an SDL texture and caches its various state
/// Currently, the texture is always set to have no blending and is assumed to be SDL_PIXELFORMAT_ARGB8888
/// </summary>
public sealed class SDLTexture(IntPtr sdlRenderer, SDL_TextureAccess textureAccess, SDL_ScaleMode scaleMode) : IDisposable
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

			if (SDL_SetTextureBlendMode(Texture, SDL_BlendMode.SDL_BLENDMODE_NONE) != 0)
			{
				throw new($"Failed to set texture blend mode, SDL error: {SDL_GetError()}");
			}

			Width = width;
			Height = height;
		}
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
