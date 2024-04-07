// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;

namespace GSR.Android.JNI;

/// <summary>
/// JavaVM is just a typedef to JNIInvokeInterface*
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct JavaVM
{
	public JNIInvokeInterface* Vtbl;
}
