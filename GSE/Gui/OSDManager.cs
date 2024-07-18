// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.IO.Hashing;
using System.Collections.Concurrent;
using System.Numerics;

using ImGuiNET;

using static SDL2.SDL;

using GSE.Emu;

namespace GSE.Gui;

/// <summary>
/// Manages the OSD. Can be done on a status bar or a transparent overlay
/// </summary>
internal sealed class OSDManager(Config config, EmuManager emuManager, SDLRenderer sdlRenderer)
{
	private readonly ConcurrentQueue<string> _osdMessages = new();
	private string _currentOsdMessage;
	private int _currentOsdMessageCountdown;
	private string _currentRomHash;
	private bool _isPsrRom;

	private readonly SDLTexture _statePreview = new(sdlRenderer, SDL_PIXELFORMAT_ARGB8888,
		SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, SDL_ScaleMode.SDL_ScaleModeNearest, SDL_BlendMode.SDL_BLENDMODE_BLEND);
	private int _statePreviewCountdown;
	private int _statePreviewSlot;
	public bool StatePreviewActive => _statePreviewCountdown > 0;

	private string RomInfoPrefix()
	{
		return $"{(_isPsrRom ? "<PSR> | " : string.Empty)}{emuManager.CurrentGbPlatform} | {_currentRomHash}";
	}

	public void OnRomLoaded(string romName, ReadOnlySpan<byte> romData)
	{
		_currentRomHash = $"{Crc32.HashToUInt32(romData):X8}";
		var sha256 = Convert.ToHexString(GSEHash.HashDataSHA256(romData));
		_isPsrRom = PSRData.GoodRoms.Contains(sha256);
		_osdMessages.Enqueue($"{(_isPsrRom ? "<PSR> | " : string.Empty)}{_currentRomHash} | Loaded {romName}");
	}

	public void OnRomUnloaded()
	{
		_currentRomHash = null;
		_isPsrRom = false;
		_osdMessages.Enqueue("Unloaded ROM");
	}

	public void OnHardReset()
	{
		_osdMessages.Enqueue($"v{GSEVersion.FullSemVer} | {RomInfoPrefix()} | Reset");
	}

	public void QueueMessage(string message)
	{
		_osdMessages.Enqueue(message);
	}

	private string NextOSDMessage()
	{
		if (_osdMessages.TryDequeue(out var newMessage))
		{
			_currentOsdMessage = newMessage;
			_currentOsdMessageCountdown = 180; // around 3 seconds
			return _currentOsdMessage;
		}

		if (_currentOsdMessage != null)
		{
			_currentOsdMessageCountdown--;
			if (_currentOsdMessageCountdown == 0)
			{
				_currentOsdMessage = null;
			}
		}

		return _currentOsdMessage;
	}

	public void RunStatusBar()
	{
		var vp = ImGui.GetMainViewport();
		var frameHeight = ImGui.GetFrameHeight();
		if (ImGuiInternal.BeginViewportSidebar("OSD Status Bar", vp, ImGuiDir.Down, frameHeight, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.MenuBar))
		{
			if (ImGui.BeginMenuBar())
			{
				var nextOsdMessage = NextOSDMessage();
				if (nextOsdMessage != null)
				{
					ImGui.TextUnformatted(nextOsdMessage);
				}
				else
				{
					// normal status if no OSD message was displayed
					if (emuManager.RomIsLoaded)
					{
						ImGui.TextUnformatted($"v{GSEVersion.FullSemVer} | {RomInfoPrefix()}");
						var cycleCountStr = $"{emuManager.GetCycleCount()}";
						ImGui.SameLine(ImGui.GetWindowWidth() - ImGui.CalcTextSize(cycleCountStr).X - ImGui.GetTextLineHeight());
						ImGui.TextUnformatted(cycleCountStr);
					}
				}

				ImGui.EndMenuBar();
			}
		}

		ImGui.End();
	}

	public void RunOverlay()
	{
		var nextOsdMessage = NextOSDMessage();
		if (nextOsdMessage != null)
		{
			var vp = ImGui.GetMainViewport();
			ImGui.SetNextWindowPos(new(vp.Pos.X + ImGui.GetStyle().FramePadding.X, vp.Pos.Y + vp.Size.Y - ImGui.GetFrameHeight() * 2));
			if (ImGui.Begin("OSD Overlay", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDecoration))
			{
				ImGui.TextUnformatted(nextOsdMessage);
			}

			ImGui.End();
		}
	}

	public void SetStatePreview(EmuVideoBuffer emuVideoBuffer, int slot)
	{
		_statePreview.SetEmuVideoBuffer(emuVideoBuffer);
		_statePreviewCountdown = 180; // roughly 3 seconds
		_statePreviewSlot = slot;
	}

	public void ClearStatePreview()
	{
		_statePreviewCountdown = 0;
	}

	public void RunStatePreviewOverlay()
	{
		if (StatePreviewActive)
		{
			var vp = ImGui.GetMainViewport();
			var style = ImGui.GetStyle();

			// calculate a rect of the "OSD overlay area"
			var leftSide = vp.Pos.X + style.FramePadding.X;
			var rightSide = vp.Pos.X + vp.Size.X - style.FramePadding.X;
			var topSide = vp.Pos.Y + style.FramePadding.Y + ImGui.GetFrameHeight();
			var bottomSide = vp.Pos.Y + vp.Size.Y - style.FramePadding.Y;

			// we want the preview width to be decently wide
			// but we also want the height to cover a percentage of the screen
			var previewHeight = (float)Math.Round((bottomSide - topSide) * config.StatePreviewScale / 100.0f);
			var previewWidth = (float)Math.Round(previewHeight * _statePreview.Width / _statePreview.Height);

			// the X pos should shift left according to the state slot
			// but we'd want to "overlap" the slot areas, otherwise the previews would be too small
			var rightMostPreview = rightSide - previewWidth;
			var previewXPos = (float)Math.Round(leftSide + (rightMostPreview - leftSide) / 9 * _statePreviewSlot);

			ImGui.SetNextWindowPos(new(previewXPos, vp.Pos.Y + style.FramePadding.Y + ImGui.GetFrameHeight()));
			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
			if (ImGui.Begin("State Preview", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground))
			{
				var opacity = config.StatePreviewOpacity / 100.0f;
				ImGui.Image(_statePreview.TextureId, new(previewWidth, previewHeight), new(0, 0), new(1, 1), new(1, 1, 1, opacity));
			}
			ImGui.PopStyleVar(3);

			ImGui.End();
			_statePreviewCountdown--;
		}
	}
}
