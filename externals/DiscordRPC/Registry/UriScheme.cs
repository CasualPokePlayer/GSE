using System;
using System.Runtime.InteropServices;

using DiscordRPC.Logging;

namespace DiscordRPC.Registry;

internal class UriSchemeRegister(
	ILogger logger,
	string applicationId,
	string steamAppId = null,
	string executable = null)
{
	/// <summary>
	/// The ID of the Discord App to register
	/// </summary>
	public string ApplicationID { get; } = applicationId.Trim();

	/// <summary>
	/// Optional Steam App ID to register. If given a value, then the game will launch through steam instead of Discord.
	/// </summary>
	public string SteamAppID { get; } = steamAppId?.Trim();

	/// <summary>
	/// Is this register using steam?
	/// </summary>
	public bool UsingSteamApp => !string.IsNullOrEmpty(SteamAppID) && SteamAppID != "";

	/// <summary>
	/// The full executable path of the application.
	/// </summary>
	public string ExecutablePath { get; } = executable ?? GetApplicationLocation();

	/// <summary>
	/// Registers the URI scheme, using the correct creator for the correct platform
	/// </summary>
	public bool RegisterUriScheme()
	{
		// Get the creator
		IUriSchemeCreator creator;
		// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
		if (OperatingSystem.IsWindows())
		{
			logger.Trace("Creating Windows Scheme Creator");
			creator = new WindowsUriSchemeCreator(logger);
		}
		else if (Environment.OSVersion.Platform == PlatformID.Unix)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				logger.Trace("Creating MacOSX Scheme Creator");
				creator = new MacUriSchemeCreator(logger);
			}
			else
			{
				logger.Trace("Creating Unix Scheme Creator");
				creator = new UnixUriSchemeCreator(logger);
			}
		}
		else
		{
			logger.Error("Unkown Platform: {0}", Environment.OSVersion.Platform);
			throw new PlatformNotSupportedException("Platform does not support registration.");
		}

		// Regiser the app
		if (creator.RegisterUriScheme(this))
		{
			logger.Info("URI scheme registered.");
			return true;
		}

		return false;
	}

	/// <summary>
	/// Gets the FileName for the currently executing application
	/// </summary>
	/// <returns></returns>
	public static string GetApplicationLocation()
	{
		return Environment.ProcessPath;
	}
}
