// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;

namespace GSE.Android.JNI;

/// <summary>
/// JNIEnv is just a typedef to JNINativeInterface*
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct JNIEnv
{
	public JNINativeInterface* Vtbl;
}
