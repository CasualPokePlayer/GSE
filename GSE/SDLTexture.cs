// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Runtime.InteropServices;

using static SDL2.SDL;

using GSE.Emu;

namespace GSE;

/// <summary>
/// Wraps an SDL texture and caches its various state
/// Managing the texture should be done via this class
/// </summary>
internal sealed class SDLTexture : IDisposable
{
	private readonly SDLRenderer _sdlRenderer;
	private readonly uint _pixelFormat;
	private readonly SDL_TextureAccess _textureAccess;
	private readonly SDL_ScaleMode _scaleMode;
	private readonly SDL_BlendMode _blendMode;
	private readonly Action _onRecreate;

	public readonly nint TextureId;
	public readonly bool IsRenderTarget;

	public SDLTexture(SDLRenderer sdlRenderer, uint pixelFormat, SDL_TextureAccess textureAccess,
		SDL_ScaleMode scaleMode, SDL_BlendMode blendMode, Action onRecreate = null)
	{
		_sdlRenderer = sdlRenderer;
		_pixelFormat = pixelFormat;
		_textureAccess = textureAccess;
		_scaleMode = scaleMode;
		_blendMode = blendMode;
		_onRecreate = onRecreate;

		TextureId = _sdlRenderer.CreateTextureId(this);
		IsRenderTarget = _textureAccess == SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET;
	}

	public nint _texture;

	public int Width { get; private set; }
	public int Height { get; private set; }

	// note: this is only public so SDLRenderer can use it
	// do not use this otherwise
	public nint GetNativeTexture()
	{
		return _texture;
	}

	public void RecreateTexture()
	{
		var firstCreation = _texture == 0;
		Dispose();

		do
		{
			_texture = _sdlRenderer.CreateNativeTexture(_pixelFormat, _textureAccess, Width, Height);
			if (_texture == 0 && !_sdlRenderer.CheckDeviceLost())
			{
				throw new($"Failed to create video texture, SDL error: {SDL_GetError()}");
			}
		} while (_texture == 0);

		if (SDL_SetTextureScaleMode(_texture, _scaleMode) != 0)
		{
			throw new($"Failed to set texture scaling mode, SDL error: {SDL_GetError()}");
		}

		if (SDL_SetTextureBlendMode(_texture, _blendMode) != 0)
		{
			throw new($"Failed to set texture blend mode, SDL error: {SDL_GetError()}");
		}

		if (!firstCreation)
		{
			// only called if we actually re-created the texture, and this isn't just the first creation
			_onRecreate?.Invoke();
		}
	}

	public void SetVideoDimensions(int width, int height)
	{
		if (_texture == 0 || Width != width || Height != height)
		{
			Width = width;
			Height = height;
			RecreateTexture();
		}
	}

	private readonly ref struct SDLTextureLock
	{
		private readonly SDLTexture _sdlTexture;
		public readonly nint Pixels;
		public readonly int Pitch;

		public SDLTextureLock(SDLTexture sdlTexture, SDLRenderer sdlRenderer)
		{
			_sdlTexture = sdlTexture;
			while (SDL_LockTexture(_sdlTexture._texture, 0, out Pixels, out Pitch) != 0)
			{
				if (!sdlRenderer.CheckDeviceLost())
				{
					throw new($"Failed to lock SDL texture, SDL error {SDL_GetError()}");
				}
			}
		}

		public void Dispose()
		{
			SDL_UnlockTexture(_sdlTexture._texture);
		}
	}

	public unsafe void ResetTexture(int width, int height)
	{
		SetVideoDimensions(width, height);

		using var texLock = new SDLTextureLock(this, _sdlRenderer);
		new Span<byte>((void*)texLock.Pixels, texLock.Pitch * height).Clear();
	}

	public unsafe void SetEmuVideoBuffer(EmuVideoBuffer emuVideoBuffer)
	{
		SetVideoDimensions(emuVideoBuffer.Width, emuVideoBuffer.Height);

		using var texLock = new SDLTextureLock(this, _sdlRenderer);
		if (texLock.Pitch == emuVideoBuffer.Pitch) // identical pitch, fast case (probably always the case?)
		{
			emuVideoBuffer.VideoBuffer.CopyTo(new((void*)texLock.Pixels, emuVideoBuffer.VideoBuffer.Length));
		}
		else // different pitch, slow case (indicates padding between lines)
		{
			var videoBufferAsBytes = MemoryMarshal.AsBytes(emuVideoBuffer.VideoBuffer);
			for (var i = 0; i < Height; i++)
			{
				videoBufferAsBytes.Slice(i * emuVideoBuffer.Pitch, emuVideoBuffer.Pitch)
					.CopyTo(new((void*)(texLock.Pixels + i * texLock.Pitch), emuVideoBuffer.Pitch));
			}
		}
	}

	/// <summary>
	/// This should only be used for SDL_TEXTUREACCESS_STATIC textures (i.e. ImGui font textures)
	/// </summary>
	public void UpdateTexture(int width, int height, nint pixels, int pitch)
	{
		SetVideoDimensions(width, height);

		while (SDL_UpdateTexture(_texture, 0, pixels, pitch) != 0)
		{
			if (!_sdlRenderer.CheckDeviceLost())
			{
				throw new($"Failed to update SDL texture! SDL error: {SDL_GetError()}");
			}
		}
	}

	public void Dispose()
	{
		if (_texture != 0)
		{
			SDL_DestroyTexture(_texture);
			_texture = 0;
		}
	}
}
