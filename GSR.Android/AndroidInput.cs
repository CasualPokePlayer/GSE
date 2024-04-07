// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;

using GSR.Android.JNI;
using GSR.Input;

namespace GSR.Android;

/// <summary>
/// GSR.Input can't contain any entrypoints, as it's not the published assembly
/// Therefore we have to implement input entrypoints here
/// </summary>
public static class AndroidInput
{
	// ReSharper disable once UnassignedField.Global
#pragma warning disable CA2211 // GSR assembly needs to be able to set this
	public static volatile InputManager InputManager;
#pragma warning restore CA2211

	[UnmanagedCallersOnly]
	internal static void DispatchAndroidKeyEvent(JNIEnvPtr env, JClass cls, JInt keycode, JBoolean pressed)
	{
		_ = env;
		_ = cls;
		var inputManager = InputManager;
		inputManager?.DispatchAndroidKeyEvent(keycode, pressed);
	}
}
