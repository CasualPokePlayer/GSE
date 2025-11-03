// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
using System.Linq;

using static SDL3.SDL;

namespace GSE;

/// <summary>
/// Wraps an SDL renderer and manages textures created from that renderer
/// Textures may need to be reset at times due to the native device object being reset
/// Managing the renderer should be done via this class
/// </summary>
internal sealed class SDLRenderer(nint sdlRenderer) : IDisposable
{
	// used for tracking if we're in a device reset call
	// we might end up calling this function recursively
	// if we are, we need to bail out immediately
	private bool _inDeviceResetCall;
	private sealed class RecursiveDeviceResetCallException : Exception;

	private nint _nextTextureId;
	private readonly Dictionary<nint, SDLTexture> _textureIdMap = [];

	private readonly SDL_Event[] _sdlEvent = new SDL_Event[1];
	private SDLTexture _currentRenderTarget;
	private bool _vsyncEnabled;

	public nint CreateTextureId(SDLTexture sdlTexture)
	{
		if (sdlTexture.TextureId != 0)
		{
			throw new("Tried to set texture id for already created SDLTexture");
		}

		// don't let 0 be a texture id (we need this to be 0 to indicate no texture)
		if (_nextTextureId == 0)
		{
			_nextTextureId = 1;
		}

		_textureIdMap.Add(_nextTextureId, sdlTexture);
		return _nextTextureId++;
	}

	// only for SDLTexture use (creates an SDL_Texture*)
	public unsafe nint CreateNativeTexture(SDL_PixelFormat pixelFormat, SDL_TextureAccess textureAccess, int width, int height)
	{
		return (nint)SDL_CreateTexture(sdlRenderer, pixelFormat, textureAccess, width, height);
	}

	/// <summary>
	/// Should only be called if an SDL renderer/texture call fails for whatever reason
	/// </summary>
	/// <returns>true if device was recovered (and affected textures were reset), false otherwise</returns>
	public bool CheckDeviceReset()
	{
		// make sure we don't trash error state here (in case we didn't actually lose the device, and thus this error is the real issue)
		var oldError = SDL_GetError();
		if (_inDeviceResetCall)
		{
			// if we're here, this means device recovery failed
			// this might be due to the device being lost again
			// peek at (not get!) events to see if this is in fact the case
			if (SDL_PeepEvents(_sdlEvent, _sdlEvent.Length, SDL_EventAction.SDL_PEEKEVENT,
				    (uint)SDL_EventType.SDL_EVENT_RENDER_TARGETS_RESET, (uint)SDL_EventType.SDL_EVENT_RENDER_DEVICE_RESET) > 0)
			{
				throw new RecursiveDeviceResetCallException();
			}

			SDL_SetError(oldError);
			return false;
		}

		while (true)
		{
			bool resetRts = false, resetAll = false;
			while (SDL_PeepEvents(_sdlEvent, _sdlEvent.Length, SDL_EventAction.SDL_GETEVENT,
				(uint)SDL_EventType.SDL_EVENT_RENDER_TARGETS_RESET, (uint)SDL_EventType.SDL_EVENT_RENDER_DEVICE_RESET) > 0)
			{
				resetRts |= _sdlEvent[0].type == (uint)SDL_EventType.SDL_EVENT_RENDER_TARGETS_RESET;
				resetAll |= _sdlEvent[0].type == (uint)SDL_EventType.SDL_EVENT_RENDER_DEVICE_RESET;
			}

			if (!resetAll && !resetRts)
			{
				SDL_SetError(oldError);
				return false;
			}

			try
			{
				_inDeviceResetCall = true;
				foreach (var sdlTexture in _textureIdMap.Values
					         .Where(t => t.GetNativeTexture() != 0 && (resetAll || t.IsRenderTarget)))
				{
					sdlTexture.RecreateTexture();
				}

				SetRenderTarget(_currentRenderTarget);
				_inDeviceResetCall = false;
				return true;
			}
			catch (RecursiveDeviceResetCallException)
			{
				// swallow the exception (we'll just re-loop our logic)
			}
			catch // other unexpected exceptions
			{
				_inDeviceResetCall = false;
				throw;
			}
		}
	}

	public void SetRenderTarget(SDLTexture sdlTexture)
	{
		_currentRenderTarget = sdlTexture;
		while (!SDL_SetRenderTarget(sdlRenderer, sdlTexture?.GetNativeTexture() ?? 0))
		{
			if (!CheckDeviceReset())
			{
				throw new($"Failed to set render target, SDL error: {SDL_GetError()}");
			}
		}
	}

	public void SetRenderDrawColor(byte r, byte g, byte b, byte a)
	{
		if (!SDL_SetRenderDrawColor(sdlRenderer, r, g, b, a))
		{
			// never can error due to device reset
			throw new($"Failed to set render draw color, SDL error: {SDL_GetError()}");
		}
	}

	public void RenderClear()
	{
		if (!SDL_RenderClear(sdlRenderer))
		{
			// never can error due to device reset
			throw new($"Failed to clear render target, SDL error: {SDL_GetError()}");
		}
	}

	public void RenderTexture(SDLTexture src, in SDL_Rect srcRect, in SDL_Rect dstRect)
	{
		var srcFRect = new SDL_FRect { x = srcRect.x, y = srcRect.y, w = srcRect.w, h = srcRect.h };
		var dstFRect = new SDL_FRect { x = dstRect.x, y = dstRect.y, w = dstRect.w, h = dstRect.h };
		while (!SDL_RenderTexture(sdlRenderer, src.GetNativeTexture(), ref srcFRect, ref dstFRect))
		{
			if (!CheckDeviceReset())
			{
				throw new($"Failed to copy texture to render target, SDL error: {SDL_GetError()}");
			}
		}
	}

	public void GetRenderOutputSize(out int w, out int h)
	{
		if (!SDL_GetRenderOutputSize(sdlRenderer, out w, out h))
		{
			throw new($"Failed to obtain renderer output size, SDL error: {SDL_GetError()}");
		}
	}

	public bool RenderClipEnabled()
	{
		SDL_ClearError();
		var ret = SDL_RenderClipEnabled(sdlRenderer);
		if (!ret)
		{
			var error = SDL_GetError();
			if (error != string.Empty)
			{
				throw new($"Failed to query clip enabled state, SDL error: {error}");
			}
		}

		return ret;
	}

	public void GetRenderViewport(out SDL_Rect rect)
	{
		while (!SDL_GetRenderViewport(sdlRenderer, out rect))
		{
			if (!CheckDeviceReset())
			{
				throw new($"Failed to get viewport, SDL error: {SDL_GetError()}");
			}
		}
	}

	public void GetRenderClipRect(out SDL_Rect rect)
	{
		while (!SDL_GetRenderClipRect(sdlRenderer, out rect))
		{
			if (!CheckDeviceReset())
			{
				throw new($"Failed to get clip rect, SDL error: {SDL_GetError()}");
			}
		}
	}

	public void SetRenderViewport(ref SDL_Rect rect)
	{
		while (!SDL_SetRenderViewport(sdlRenderer, ref rect))
		{
			if (!CheckDeviceReset())
			{
				throw new($"Failed to set viewport, SDL error: {SDL_GetError()}");
			}
		}
	}

	public void SetRenderClipRect(ref SDL_Rect rect)
	{
		while (!SDL_SetRenderClipRect(sdlRenderer, ref rect))
		{
			if (!CheckDeviceReset())
			{
				throw new($"Failed to set clip rect, SDL error: {SDL_GetError()}");
			}
		}
	}

	public void RenderGeometryRaw(nint textureId, nint xy, int xy_stride, nint color, int color_stride, nint uv, int uv_stride, int num_vertices, nint indices, int num_indices, int size_indices)
	{
		var sdlTexture = textureId == 0 ? null : _textureIdMap[textureId];
		while (!SDL_RenderGeometryRaw(sdlRenderer, sdlTexture?.GetNativeTexture() ?? 0, xy, xy_stride, color, color_stride, uv, uv_stride, num_vertices, indices, num_indices, size_indices))
		{
			if (!CheckDeviceReset())
			{
				throw new($"Failed to render raw geometry, SDL error: {SDL_GetError()}");
			}
		}
	}

	public void SetRenderVSync(bool vsync)
	{
		if (_vsyncEnabled != vsync)
		{
			_vsyncEnabled = vsync;
			while (!SDL_SetRenderVSync(sdlRenderer, vsync ? 1 : 0))
			{
				if (!CheckDeviceReset())
				{
					throw new($"Failed to {(vsync ? "enable" : "disable")} VSync, SDL error: {SDL_GetError()}");
				}
			}

			// D3D9 changing vsync will trigger an SDL_EVENT_RENDER_TARGETS_RESET event
			_ = CheckDeviceReset();
		}
	}

	public void RenderPresent()
	{
		SDL_RenderPresent(sdlRenderer);
		// present is typically where SDL_EVENT_RENDER_DEVICE_RESET events occur, so make sure to check it here
		_ = CheckDeviceReset();
	}

	public void Dispose()
	{
		foreach (var sdlTexture in _textureIdMap.Values)
		{
			sdlTexture.Dispose();
		}

		_textureIdMap.Clear();
		SDL_DestroyRenderer(sdlRenderer);
	}
}
