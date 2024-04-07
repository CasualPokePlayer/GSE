// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

// ID types defined by https://docs.oracle.com/javase/8/docs/technotes/guides/jni/spec/types.html

using System.Runtime.InteropServices;

namespace GSR.Android.JNI;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JFieldID(nint value)
{
	public readonly nint Value = value;
	public bool IsNull => Value == 0;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JMethodID(nint value)
{
	public readonly nint Value = value;
	public bool IsNull => Value == 0;
}
