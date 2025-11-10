// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

// primitive types defined by https://docs.oracle.com/javase/8/docs/technotes/guides/jni/spec/types.html

using System.Runtime.InteropServices;

// ReSharper disable DefaultStructEqualityIsUsed.Global

namespace GSE.Android.JNI;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JBoolean(byte value)
{
	public readonly byte Value = value;
	public static implicit operator bool(JBoolean value) => value.Value != 0;
	public static implicit operator JBoolean(bool value) => new((byte)(value ? 1 : 0));
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JByte(sbyte value)
{
	public readonly sbyte Value = value;
	public static implicit operator sbyte(JByte value) => value.Value;
	public static implicit operator JByte(sbyte value) => new(value);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JChar(ushort value)
{
	public readonly ushort Value = value;
	public static implicit operator char(JChar value) => (char)value.Value;
	public static implicit operator JChar(char value) => new(value);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JShort(short value)
{
	public readonly short Value = value;
	public static implicit operator short(JShort value) => value.Value;
	public static implicit operator JShort(short value) => new(value);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JInt(int value)
{
	public readonly int Value = value;
	public static implicit operator int(JInt value) => value.Value;
	public static implicit operator JInt(int value) => new(value);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JLong(long value)
{
	public readonly long Value = value;
	public static implicit operator long(JLong value) => value.Value;
	public static implicit operator JLong(long value) => new(value);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JFloat(float value)
{
	public readonly float Value = value;
	public static implicit operator float(JFloat value) => value.Value;
	public static implicit operator JFloat(float value) => new(value);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JDouble(double value)
{
	public readonly double Value = value;
	public static implicit operator double(JDouble value) => value.Value;
	public static implicit operator JDouble(double value) => new(value);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct JSize(int Value)
{
	public readonly int Value = Value;
	public static implicit operator int(JSize value) => value.Value;
	public static implicit operator JSize(int value) => new(value);
}
