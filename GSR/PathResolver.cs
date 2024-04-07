// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#if !GSR_PUBLISH || !GSR_ANDROID
using System;
#endif
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
#elif GSR_ANDROID
		// we prefer the "external" storage path (SDL_GetPrefPath uses the "internal" storage path)
		return SDL_AndroidGetExternalStoragePath();
#elif GSR_OSX
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
		// only the pref path is freely writable for us on Android
		_ = pathType;
		_ = romPath;
		_ = customPath;

		if (folderName == null)
		{
			return _prefPath;
		}

		var ret = Path.Combine(_prefPath, folderName);
		Directory.CreateDirectory(ret);
		return ret;
#else
		var ret = pathType switch
		{
			PathType.RomPath => romPath,
			PathType.PrefPath => _prefPath,
			PathType.Custom => customPath,
			_ => throw new InvalidOperationException()
		};

		// if we're pref path based, we'll typically want to create a folder to store our files
		if (folderName != null && pathType == PathType.PrefPath)
		{
			ret = Path.Combine(ret, folderName);
			Directory.CreateDirectory(ret);
		}

		return ret;
#endif
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
