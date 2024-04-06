// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#if GSR_ANDROID

using System.Runtime.InteropServices;

using GSR.Input;

namespace GSR.Android;

/// <summary>
/// GSR.Input can't contain any entrypoints, as it's not the published assembly
/// Therefore we have to implement input entrypoints here
/// </summary>
internal static class AndroidInput
{
	public static volatile InputManager InputManager;

	[UnmanagedCallersOnly(EntryPoint = "Java_org_psr_gsr_GSRActivity_DispatchAndroidKeyEvent")]
	public static void DispatchAndroidKeyEvent(JNIEnvPtr env, JClass cls, JInt keycode, JBoolean pressed)
	{
		_ = env;
		_ = cls;
		var inputManager = InputManager;
		inputManager?.DispatchAndroidKeyEvent(keycode, pressed != JNI.FALSE);
	}
}

#endif
