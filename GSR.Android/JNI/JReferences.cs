// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

// reference types defined by https://docs.oracle.com/javase/8/docs/technotes/guides/jni/spec/types.html

using System.Runtime.InteropServices;

namespace GSR.Android.JNI;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JObject(nint value)
{
	public readonly nint Value = value;
	public bool IsNull => Value == 0;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JClass(JObject value)
{
	public readonly JObject Value = value;
	public static implicit operator JObject(JClass value) => value.Value;
	public static explicit operator JClass(JObject value) => new(value);
	public bool IsNull => Value.IsNull;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JString(JObject value)
{
	public readonly JObject Value = value;
	public static implicit operator JObject(JString value) => value.Value;
	public static explicit operator JString(JObject value) => new(value);
	public bool IsNull => Value.IsNull;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JArray(JObject value)
{
	public readonly JObject Value = value;
	public static implicit operator JObject(JArray value) => value.Value;
	public static explicit operator JArray(JObject value) => new(value);
	public bool IsNull => Value.IsNull;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JObjectArray(JArray value)
{
	public readonly JArray Value = value;
	public static implicit operator JArray(JObjectArray value) => value.Value;
	public static explicit operator JObjectArray(JArray value) => new(value);
	public static implicit operator JObject(JObjectArray value) => value.Value;
	public static explicit operator JObjectArray(JObject value) => new(new(value));
	public bool IsNull => Value.IsNull;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JBooleanArray(JArray value)
{
	public readonly JArray Value = value;
	public static implicit operator JArray(JBooleanArray value) => value.Value;
	public static explicit operator JBooleanArray(JArray value) => new(value);
	public static implicit operator JObject(JBooleanArray value) => value.Value;
	public static explicit operator JBooleanArray(JObject value) => new(new(value));
	public bool IsNull => Value.IsNull;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JByteArray(JArray value)
{
	public readonly JArray Value = value;
	public static implicit operator JArray(JByteArray value) => value.Value;
	public static explicit operator JByteArray(JArray value) => new(value);
	public static implicit operator JObject(JByteArray value) => value.Value;
	public static explicit operator JByteArray(JObject value) => new(new(value));
	public bool IsNull => Value.IsNull;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JCharArray(JArray value)
{
	public readonly JArray Value = value;
	public static implicit operator JArray(JCharArray value) => value.Value;
	public static explicit operator JCharArray(JArray value) => new(value);
	public static implicit operator JObject(JCharArray value) => value.Value;
	public static explicit operator JCharArray(JObject value) => new(new(value));
	public bool IsNull => Value.IsNull;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JShortArray(JArray value)
{
	public readonly JArray Value = value;
	public static implicit operator JArray(JShortArray value) => value.Value;
	public static explicit operator JShortArray(JArray value) => new(value);
	public static implicit operator JObject(JShortArray value) => value.Value;
	public static explicit operator JShortArray(JObject value) => new(new(value));
	public bool IsNull => Value.IsNull;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JIntArray(JArray value)
{
	public readonly JArray Value = value;
	public static implicit operator JArray(JIntArray value) => value.Value;
	public static explicit operator JIntArray(JArray value) => new(value);
	public static implicit operator JObject(JIntArray value) => value.Value;
	public static explicit operator JIntArray(JObject value) => new(new(value));
	public bool IsNull => Value.IsNull;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JLongArray(JArray value)
{
	public readonly JArray Value = value;
	public static implicit operator JArray(JLongArray value) => value.Value;
	public static explicit operator JLongArray(JArray value) => new(value);
	public static implicit operator JObject(JLongArray value) => value.Value;
	public static explicit operator JLongArray(JObject value) => new(new(value));
	public bool IsNull => Value.IsNull;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JFloatArray(JArray value)
{
	public readonly JArray Value = value;
	public static implicit operator JArray(JFloatArray value) => value.Value;
	public static explicit operator JFloatArray(JArray value) => new(value);
	public static implicit operator JObject(JFloatArray value) => value.Value;
	public static explicit operator JFloatArray(JObject value) => new(new(value));
	public bool IsNull => Value.IsNull;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JDoubleArray(JArray value)
{
	public readonly JArray Value = value;
	public static implicit operator JArray(JDoubleArray value) => value.Value;
	public static explicit operator JDoubleArray(JArray value) => new(value);
	public static implicit operator JObject(JDoubleArray value) => value.Value;
	public static explicit operator JDoubleArray(JObject value) => new(new(value));
	public bool IsNull => Value.IsNull;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JThrowable(JObject value)
{
	public readonly JObject Value = value;
	public static implicit operator JObject(JThrowable value) => value.Value;
	public static explicit operator JThrowable(JObject value) => new(value);
	// ReSharper disable once UnusedMember.Global
	public bool IsNull => Value.IsNull;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JWeak(JObject value)
{
	public readonly JObject Value = value;
	public static implicit operator JObject(JWeak value) => value.Value;
	public static explicit operator JWeak(JObject value) => new(value);
	public bool IsNull => Value.IsNull;
}
