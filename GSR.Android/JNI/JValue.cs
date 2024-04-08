// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

// value type defined by https://docs.oracle.com/javase/8/docs/technotes/guides/jni/spec/types.html

using System.Runtime.InteropServices;

// ReSharper disable UnusedType.Global

namespace GSR.Android.JNI;

[StructLayout(LayoutKind.Explicit)]
internal struct JValue
{
	[FieldOffset(0)]
	public JBoolean z;
	[FieldOffset(0)]
	public JByte b;
	[FieldOffset(0)]
	public JChar c;
	[FieldOffset(0)]
	public JShort s;
	[FieldOffset(0)]
	public JInt i;
	[FieldOffset(0)]
	public JLong j;
	[FieldOffset(0)]
	public JFloat f;
	[FieldOffset(0)]
	public JDouble d;
	[FieldOffset(0)]
	public JObject l;
}
