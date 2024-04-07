// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#if GSR_ANDROID

using System.Runtime.InteropServices;

using GSR.Android;

namespace GSR;

/// <summary>
/// Exports must be placed in the GSR project, as it is the published assembly
/// Note too entrypoints have to be defined under
/// </summary>
internal static class AndroidExports
{
	/// <summary>
	/// Entrypoint used in the Java side for the main thread
	/// Uses standard C main() args
	/// Although we don't handle those right now, as we don't have a CLI
	/// </summary>
	/// <param name="argc">number of arguments</param>
	/// <param name="argv">array of UTF8 strings, filled with arguments</param>
	/// <returns>exit code</returns>
	[UnmanagedCallersOnly(EntryPoint = "GSRMain")]
	public static int GSRMain(int argc, nint argv)
	{
		_ = argc;
		_ = argv;
		return Entrypoint.Main();
	}

	/// <summary>
	/// Called by Java side on System.loadLibrary, indicting initialization of the JNI
	/// JNI entrypoints must be registered at this point, and other JNI initialization should occur here too
	/// </summary>
	/// <param name="vm">pointer to JavaVM</param>
	/// <param name="reserved">reserved</param>
	/// <returns>version number required</returns>
	[UnmanagedCallersOnly(EntryPoint = "JNI_OnLoad")]
	public static int JNIOnLoad(nint vm, nint reserved)
	{
		_ = reserved;
		return AndroidJNI.Initialize(vm);
	}
}

#endif
