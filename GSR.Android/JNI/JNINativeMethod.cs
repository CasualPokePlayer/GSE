// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

// JNINativeMethod defined by https://docs.oracle.com/javase/6/docs/technotes/guides/jni/spec/functions.html

using System.Runtime.InteropServices;

// ReSharper disable UnusedType.Global

namespace GSR.Android.JNI;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct JNINativeMethod
{
	public byte* Name;
	public byte* Signature;
	public void* FnPtr;
}
