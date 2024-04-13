// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using static SDL2.SDL;

using GSR.Audio;
using GSR.Emu;
using GSR.Gui;
using GSR.Input;

namespace GSR;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Config))]
internal partial class ConfigSerializerContext : JsonSerializerContext;

internal sealed class Config
{
	public GBPlatform GbPlatform { get; set; } = GBPlatform.GBP;
	public int FastForwardSpeed { get; set; } = 4;
	public bool ApplyColorCorrection { get; set; } = true;
	public bool DisableGbaRtc { get; set; } = true;
	public bool HideSgbBorder { get; set; }
	public bool DarkMode { get; set; } = true;
	public bool DisableWin11RoundCorners { get; set; }

	public bool HideStatusBar { get; set; }
	public bool HideStatePreviews { get; set; }
	public int StatePreviewOpacity { get; set; } = 75;
	public int StatePreviewScale { get; set; } = 30;

	public string GbBiosPath { get; set; }
	public string GbcBiosPath { get; set; }
	public string Sgb2BiosPath { get; set; }
	public string GbaBiosPath { get; set; }

	public PathResolver.PathType SavePathLocation { get; set; } = PathResolver.PathType.RomPath;
	public string SavePathCustom { get; set; }
	public PathResolver.PathType StatePathLocation { get; set; } = PathResolver.PathType.RomPath;
	public string StatePathCustom { get; set; }

	public List<string> RecentRoms { get; set; } = [];

	public int SaveStateSet { get; set; }
	public int SaveStateSlot { get; set; }

	public EmuControllerBindings EmuControllerBindings { get; set; } = new();
	public HotkeyBindings HotkeyBindings { get; set; } = new();
	public bool AllowBackgroundInput { get; set; }
	public bool BackgroundInputForJoysticksOnly { get; set; }

	public bool KeepAspectRatio { get; set; } = true;
	public ScalingFilter OutputFilter { get; set; } = ScalingFilter.SharpBilinear;
	public string RenderDriver { get; set; } = ImGuiWindow.DEFAULT_RENDER_DRIVER;
	public int WindowScale { get; set; } = 3;
	public bool AllowManualResizing { get; set; }

	public string AudioDeviceName { get; set; } = AudioManager.DEFAULT_AUDIO_DEVICE;
	public int LatencyMs { get; set; } = AudioManager.MINIMUM_LATENCY_MS;
	public int Volume { get; set; } = 100;

	[JsonConstructor]
	public Config()
	{
	}

	public void DeserializeInputBindings(InputManager inputManager, ImGuiWindow mainWindow)
	{
		try
		{
			EmuControllerBindings.DeserializeInputBindings(inputManager);
			HotkeyBindings.DeserializeInputBindings(inputManager);
		}
		catch
		{
			_ = SDL_ShowSimpleMessageBox(
				flags: SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING,
				title: "Config load failure",
				message: "Input bindings failed to be deserizalized, default input bindings will be used instead.",
				window: mainWindow.SdlWindow
			);

			EmuControllerBindings = new();
			EmuControllerBindings.DeserializeInputBindings(inputManager);
			HotkeyBindings = new();
			HotkeyBindings.DeserializeInputBindings(inputManager);
		}
	}

	public void SaveConfig(string configPath)
	{
		File.Delete(configPath);
		using var configFile = File.OpenWrite(configPath);
		JsonSerializer.Serialize(configFile, this, ConfigSerializerContext.Default.Config);
	}

	private void SanitizeConfig()
	{
		if (!Enum.IsDefined(GbPlatform))
		{
			GbPlatform = GBPlatform.GBP;
		}

		FastForwardSpeed = Math.Clamp(FastForwardSpeed, 2, 64);
		StatePreviewOpacity = Math.Clamp(StatePreviewOpacity, 25, 100);
		StatePreviewScale = Math.Clamp(StatePreviewScale, 10, 50);

		if (SavePathLocation != PathResolver.PathType.Custom || !Directory.Exists(SavePathCustom))
		{
			SavePathCustom = null;
		}

		if (!Enum.IsDefined(SavePathLocation) ||
		    (SavePathLocation == PathResolver.PathType.Custom && SavePathCustom == null))
		{
			SavePathLocation = PathResolver.PathType.RomPath;
		}

		if (StatePathLocation != PathResolver.PathType.Custom || !Directory.Exists(StatePathCustom))
		{
			StatePathCustom = null;
		}

		if (!Enum.IsDefined(StatePathLocation) ||
		    (StatePathLocation == PathResolver.PathType.Custom && StatePathCustom == null))
		{
			StatePathLocation = PathResolver.PathType.RomPath;
		}

		RecentRoms ??= [];
		SaveStateSet = Math.Clamp(SaveStateSet, 0, 9);
		SaveStateSlot = Math.Clamp(SaveStateSlot, 0, 9);

		// don't need to sanitize input bindings, since DeserializeInputBindings will revert to default bindings if something is wrong

		if (!Enum.IsDefined(OutputFilter))
		{
			OutputFilter = ScalingFilter.SharpBilinear;
		}

		RenderDriver ??= ImGuiWindow.DEFAULT_RENDER_DRIVER;
		WindowScale = Math.Clamp(WindowScale, 1, 15);
		AudioDeviceName ??= AudioManager.DEFAULT_AUDIO_DEVICE;
		LatencyMs = Math.Clamp(LatencyMs, AudioManager.MINIMUM_LATENCY_MS, AudioManager.MAXIMUM_LATENCY_MS);
		Volume = Math.Clamp(Volume, 0, 100);
	}

	public static Config LoadConfig(string configPath)
	{
		if (!File.Exists(configPath))
		{
			return new();
		}

		try
		{
			using var configFile = File.OpenRead(configPath);
			var ret = JsonSerializer.Deserialize(configFile, ConfigSerializerContext.Default.Config);
			ret.SanitizeConfig();
			return ret;
		}
		catch
		{
			_ = SDL_ShowSimpleMessageBox(
				flags: SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING,
				title: "Config load failure",
				message: "Config file failed to load, the default config will be used instead.",
				window: 0
			);

			return new();
		}
	}
}
