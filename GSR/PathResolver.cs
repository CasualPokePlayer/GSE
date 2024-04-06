// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.IO;

namespace GSR;

#if GSR_PUBLISH
using static SDL2.SDL;
#endif

internal static class PathResolver
{
	private static readonly string _prefPath = GetPrefPath();

	private static string GetPrefPath()
	{
#if !GSR_PUBLISH
		// for local builds, assume we're always portable
		return AppContext.BaseDirectory;
#elif GSR_OSX || GSR_ANDROID
		// for some platforms, we cannot do a portable build (as the application directory cannot be writable)
		return SDL_GetPrefPath("", "GSR");
#else
		// use a portable.txt file to indicate the user wants a portable build (ala Dolphin)
		// if it exists we'll use the application directory as the pref path
		var portable = File.Exists(Path.Combine(AppContext.BaseDirectory, "portable.txt"));
		return portable ? AppContext.BaseDirectory : SDL_GetPrefPath("", "GSR");
#endif
	}

	public enum PathType
	{
		RomPath,
		PrefPath,
		Custom,
	}

	public static string GetPath(PathType pathType, string folderName, string romPath, string customPath)
	{
#if GSR_ANDROID
		var ret = _prefPath;
#else
		var ret = pathType switch
		{
			PathType.RomPath => romPath,
			PathType.PrefPath => _prefPath,
			PathType.Custom => customPath,
			_ => throw new InvalidOperationException()
		};
#endif

		// if we're pref path based, we'll typically want to create a folder to store our files
		if (folderName != null && pathType == PathType.PrefPath)
		{
			ret = Path.Combine(ret, folderName);
			Directory.CreateDirectory(ret);
		}

		return ret;
	}

	public static string GetConfigPath()
	{
		return Path.Combine(_prefPath, "gsr_config.json");
	}

	public static string GetCrashLogPath()
	{
		return Path.Combine(_prefPath, "gsr_crash.txt");
	}
}
