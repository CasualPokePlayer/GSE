// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.IO.Hashing;
using System.Collections.Concurrent;
using System.Security.Cryptography;

using ImGuiNET;

using GSR.Emu;

namespace GSR.Gui;

/// <summary>
/// Manages the OSD. Can be done on a status bar or a transparent overlay
/// </summary>
internal sealed class OSDManager(EmuManager emuManager)
{
	private readonly ConcurrentQueue<string> _osdMessages = new();
	private string _currentOsdMessage;
	private int _currentOsdMessageCountdown;
	private string _currentRomHash;
	private bool _isPsrRom;

	// TODO: put emu revision in here
	private string RomInfoPrefix()
	{
		return $"{(_isPsrRom ? "<PSR> | " : string.Empty)}{emuManager.CurrentGbPlatform} | {_currentRomHash}";
	}

	public void OnRomLoaded(string romName, ReadOnlySpan<byte> romData)
	{
		_currentRomHash = $"{Crc32.HashToUInt32(romData):X8}";
		var sha256 = Convert.ToHexString(SHA256.HashData(romData));
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
		_osdMessages.Enqueue($"v{GSRVersion.FullSemVer} | {RomInfoPrefix()} | Reset");
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
						ImGui.TextUnformatted($"v{GSRVersion.FullSemVer} | {RomInfoPrefix()}");
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
			ImGui.SetNextWindowPos(new(vp.Pos.X + ImGui.GetStyle().FramePadding.X, vp.Size.Y - ImGui.GetFrameHeight() * 2));
			if (ImGui.Begin("OSD Overlay", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDecoration))
			{
				ImGui.TextUnformatted(nextOsdMessage);
			}

			ImGui.End();
		}
	}
}
