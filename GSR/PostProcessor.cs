// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;

using static SDL2.SDL;

using GSR.Emu;

namespace GSR;

public enum ScalingFilter
{
	NearestNeighbor,
	Bilinear,
	SharpBilinear
}

/// <summary>
/// Fairly simple post processor, capable of scaling with nearest neighbor, bilinear, and sharp bilinear
/// Also capable of maintaining the correct aspect ratio with letterboxing
/// This can't do anything particularly fancy, due to the restraints of SDL_Renderer
/// </summary>
internal sealed class PostProcessor(Config config, EmuManager emuManager, SDLRenderer sdlRenderer) : IDisposable
{
	private readonly SDLTexture _emuTexture = new(sdlRenderer, SDL_PIXELFORMAT_ARGB8888,
		SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, SDL_ScaleMode.SDL_ScaleModeNearest, SDL_BlendMode.SDL_BLENDMODE_NONE);
	private readonly SDLTexture _nnScaledTexture = new(sdlRenderer, SDL_PIXELFORMAT_ARGB8888,
		SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, SDL_ScaleMode.SDL_ScaleModeNearest, SDL_BlendMode.SDL_BLENDMODE_NONE);
	private readonly SDLTexture _blScaledTexture = new(sdlRenderer, SDL_PIXELFORMAT_ARGB8888,
		SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, SDL_ScaleMode.SDL_ScaleModeLinear, SDL_BlendMode.SDL_BLENDMODE_NONE);

	private (bool KeepAspectRatio, ScalingFilter VideoFilter) _lastFrameConfig;

	public void ResetEmuTexture(int width, int height)
	{
		_emuTexture.ResetTexture(width, height);
	}

	public void RenderEmuTexture(EmuVideoBuffer emuVideoBuffer)
	{
		_emuTexture.SetEmuVideoBuffer(emuVideoBuffer);
	}

	/// <summary>
	/// Helper ref struct for setting render target in an RAII style
	/// </summary>
	private readonly ref struct SDLSetRenderTargetWrapper
	{
		private readonly SDLRenderer _sdlRenderer;

		public SDLSetRenderTargetWrapper(SDLRenderer sdlRenderer, SDLTexture sdlTexture)
		{
			_sdlRenderer = sdlRenderer;
			_sdlRenderer.SetRenderTarget(sdlTexture);
		}

		public void Dispose()
		{
			_sdlRenderer.SetRenderTarget(null);
		}
	}

	private void RenderTexture(SDLTexture src, SDLTexture dst, ref SDL_Rect srcRect, ref SDL_Rect dstRect, bool clear)
	{
		using (new SDLSetRenderTargetWrapper(sdlRenderer, dst))
		{
			if (clear)
			{
				// TODO: make this configurable?
				sdlRenderer.SetRenderDrawColor(0x00, 0x00, 0x00, 0xFF);
				sdlRenderer.RenderClear();
			}

			sdlRenderer.RenderCopy(src, ref srcRect, ref dstRect);
		}
	}

	private void CalculateScaledRect(in SDL_Rect srcRect, ref SDL_Rect dstRect, bool integerScaleOnly)
	{
		var srcWidth = srcRect.w;
		var srcHeight = srcRect.h;
		var dstWidth = dstRect.w;
		var dstHeight = dstRect.h;

		if (integerScaleOnly)
		{
			var scaleW = Math.Max(dstWidth / srcWidth, 1);
			var scaleH = Math.Max(dstHeight / srcHeight, 1);
			if (config.KeepAspectRatio)
			{
				var scale = Math.Min(scaleW, scaleH);
				scaleW = scale;
				scaleH = scale;
			}

			dstWidth = srcWidth * scaleW;
			dstHeight = srcHeight * scaleH;

			// center the dest rect
			dstRect.x += (dstRect.w - dstWidth) / 2;
			dstRect.y += (dstRect.h - dstHeight) / 2;
		}
		else if (config.KeepAspectRatio)
		{
			var scaleW = dstWidth / (double)srcWidth;
			var scaleH = dstHeight / (double)srcHeight;
			var scale = Math.Min(scaleW, scaleH);

			dstWidth = Math.Min((int)Math.Round(srcWidth * scale), dstWidth);
			dstHeight = Math.Min((int)Math.Round(srcHeight * scale), dstHeight);

			// center the dest rect
			dstRect.x += (dstRect.w - dstWidth) / 2;
			dstRect.y += (dstRect.h - dstHeight) / 2;
		}

		dstRect.w = dstWidth;
		dstRect.h = dstHeight;
	}

	public SDLTexture DoPostProcessing(int finalWidth, int finalHeight)
	{
		// check if the current config changed since last frame, we'll do additional clears in such a case
		// don't need to check the window scaling here, finalWidth/finalHeight changing will cover that
		var curConfig = (config.KeepAspectRatio, config.OutputFilter);
		var configChanged = curConfig != _lastFrameConfig;
		_lastFrameConfig = curConfig;

		// first copy we do the entire emu texture for the source...
		var srcTex = _emuTexture;
		var srcRect = new SDL_Rect { x = 0, y = 0, w = srcTex.Width, h = srcTex.Height };

		// cut out the SGB border if the user wants it hidden
		if (config.HideSgbBorder && emuManager.CurrentGbPlatform == GBPlatform.SGB2)
		{
			srcRect.x = (256 - 160) / 2;
			srcRect.y = (224 - 144) / 2;
			srcRect.w = 160;
			srcRect.h = 144;
		}

		var dstTex = config.OutputFilter == ScalingFilter.Bilinear ? _blScaledTexture : _nnScaledTexture;
		var dstRect = new SDL_Rect { x = 0, y = 0, w = finalWidth, h = finalHeight };
		CalculateScaledRect(in srcRect, ref dstRect, config.OutputFilter == ScalingFilter.SharpBilinear);

		// if we end up re-creating the dst texture, we should do a clear before doing anything with it
		var needsClear = dstTex.Width != finalWidth || dstTex.Height != finalHeight;
		dstTex.SetVideoDimensions(finalWidth, finalHeight);

		RenderTexture(srcTex, dstTex, ref srcRect, ref dstRect, needsClear || configChanged);

		if (config.OutputFilter == ScalingFilter.SharpBilinear)
		{
			srcTex = dstTex;
			srcRect = dstRect;
			dstRect = new() { x = 0, y = 0, w = finalWidth, h = finalHeight };
			CalculateScaledRect(in srcRect, ref dstRect, false);

			// only do a second copy if the rects differ
			if (srcRect.x != dstRect.x ||
			    srcRect.y != dstRect.y ||
			    srcRect.w != dstRect.w ||
			    srcRect.h != dstRect.h)
			{
				dstTex = _blScaledTexture;
				needsClear = dstTex.Width != finalWidth || dstTex.Height != finalHeight;
				dstTex.SetVideoDimensions(finalWidth, finalHeight);
				RenderTexture(srcTex, dstTex, ref srcRect, ref dstRect, needsClear || configChanged);
			}
		}

		return dstTex;
	}

	public void Dispose()
	{
		_emuTexture.Dispose();
		_nnScaledTexture.Dispose();
		_blScaledTexture.Dispose();
	}
}
