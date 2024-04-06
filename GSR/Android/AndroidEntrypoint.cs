// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#if GSR_ANDROID

using System;
using System.Runtime.InteropServices;

using static SDL2.SDL;

namespace GSR.Android;

/// <summary>
/// The Android version is implemented as a shared library under a Java shim
/// This shim looks for a "GSRMain" function, then calls it with standard C main arguments
/// (this isn't called under JNI, but rather from SDL2 internally, hence the lack of JNI naming)
/// </summary>
internal static class AndroidEntrypoint
{
	[UnmanagedCallersOnly(EntryPoint = "GSRMain")]
	public static int GSRMain(int argc, nint argv)
	{
		try
		{
			AndroidFile.InitializeJNI();
			AndroidHash.InitializeJNI();
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
			_ = SDL_ShowSimpleMessageBox(
				flags: SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
				title: "Fatal Error",
				message: "JNI initialization has failed, this is fatal",
				window: 0
			);

			return -1;
		}

		// we don't have CLI args yet
		// if we ever do, we'll need to create a string[] from these
		_ = argc;
		_ = argv;
		return Entrypoint.Main();
	}
}

#endif
