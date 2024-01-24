using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using GSR.Emu;
using GSR.Input;

using static SDL2.SDL;

namespace GSR;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Config))]
internal partial class ConfigSerializerContext : JsonSerializerContext;

internal sealed class Config
{
	public EmuControllerBindings EmuControllerBindings = new();

	[JsonConstructor]
	public Config()
	{
	}

	private Config(InputManager inputManager)
	{
		EmuControllerBindings.SetDefaultBindings(inputManager);
	}

	private void DeserializeInputBindings(InputManager inputManager)
	{
		EmuControllerBindings.DeserializeInputBindings(inputManager);
	}

	public void SaveConfig(string configPath)
	{
		using var configFile = File.OpenWrite(configPath);
		JsonSerializer.Serialize(configFile, this, ConfigSerializerContext.Default.Config);
	}

	public static Config LoadConfig(InputManager inputManager, string configPath)
	{
		if (!File.Exists(configPath))
		{
			return new(inputManager);
		}

		try
		{
			using var configFile = File.OpenRead(configPath);
			var ret = JsonSerializer.Deserialize(configFile, ConfigSerializerContext.Default.Config);
			ret.DeserializeInputBindings(inputManager);
			return ret;
		}
		catch
		{
			_ = SDL_ShowSimpleMessageBox(
				flags: SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING,
				title: "Config load failure",
				message: "Config file failed to load, the default config will be used instead.",
				window: IntPtr.Zero
			);

			return new(inputManager);
		}
	}
}
