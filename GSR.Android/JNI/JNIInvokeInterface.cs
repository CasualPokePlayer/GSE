// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;

namespace GSR.Android.JNI;

/// <summary>
/// JNIInvokeInterface as described by https://docs.oracle.com/javase/8/docs/technotes/guides/jni/spec/invocation.html
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct JNIInvokeInterface
{
	public void* _reserved0;
	public void* _reserved1;
	public void* _reserved2;

	public delegate* unmanaged<JavaVM*, JInt> DestroyJavaVM;
	public delegate* unmanaged<JavaVM*, void**, void*, JInt> AttachCurrentThread;
	public delegate* unmanaged<JavaVM*, JInt> DetachCurrentThread;

	public delegate* unmanaged<JavaVM*, void**, JInt, JInt> GetEnv;

	public delegate* unmanaged<JavaVM*, void**, void*, JInt> AttachCurrentThreadAsDaemon;
}
