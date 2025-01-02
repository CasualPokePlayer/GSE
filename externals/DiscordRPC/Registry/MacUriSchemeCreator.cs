using System.IO;

using DiscordRPC.Logging;

namespace DiscordRPC.Registry;

internal class MacUriSchemeCreator(ILogger logger) : IUriSchemeCreator
{
	public bool RegisterUriScheme(UriSchemeRegister register)
	{
		//var home = Environment.GetEnvironmentVariable("HOME");
		//if (string.IsNullOrEmpty(home)) return; //TODO: Log Error

		var exe = register.ExecutablePath;
		if (string.IsNullOrEmpty(exe))
		{
			logger.Error("Failed to register because the application could not be located.");
			return false;
		}
			
		logger.Trace("Registering Steam Command");

		// Prepare the command
		var command = exe;
		if (register.UsingSteamApp) command = $"steam://rungameid/{register.SteamAppID}";
		else logger.Warning("This library does not fully support MacOS URI Scheme Registration.");

		//get the folder ready
		const string filepath = "~/Library/Application Support/discord/games";
		var directory = Directory.CreateDirectory(filepath);
		if (!directory.Exists)
		{
			logger.Error("Failed to register because {0} does not exist", filepath);
			return false;
		}

		//Write the contents to file
		var applicationSchemeFilePath = $"{filepath}/{register.ApplicationID}.json";
		File.WriteAllText(applicationSchemeFilePath, "{ \"command\": \""+ command + "\" }");
		logger.Trace("Registered {0}, {1}", applicationSchemeFilePath, command);
		return true;
	}
}
