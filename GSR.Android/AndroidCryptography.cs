// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Runtime.InteropServices;

using GSR.Android.JNI;

namespace GSR.Android;

/// <summary>
/// System.Security.Cryptography requires OpenSSL be bundled in on Android
/// This is annoying, and we don't use many cryptography methods in the first place
/// We can work around this by using the Java cryptography methods with JNI
/// </summary>
public static class AndroidCryptography
{
	private static JClass _gsrActivityClassId;
	private static JMethodID _hashDataSha256MethodId;
	private static JMethodID _getRandomInt32MethodId;

	internal static void InitializeJNI(JNIEnvPtr env)
	{
		_gsrActivityClassId = env.FindClass("org/psr/gsr/GSRActivity"u8);
		_hashDataSha256MethodId = env.GetStaticMethodID(_gsrActivityClassId, "HashDataSHA256"u8, "(Ljava/nio/ByteBuffer;)[B"u8);
		_getRandomInt32MethodId = env.GetStaticMethodID(_gsrActivityClassId, "GetRandomInt32"u8, "(I)I"u8);
	}

	public static unsafe byte[] HashDataSHA256(ReadOnlySpan<byte> data)
	{
		var env = JNIEnvPtr.GetEnv();
		fixed (byte* dataPtr = data)
		{
			using var bb = new LocalRefWrapper<JObject>(env, env.NewDirectByteBuffer(dataPtr, data.Length));
			Span<JValue> args = stackalloc JValue[1];
			args[0].l = bb.LocalRef;
			using var javaSha256 = new LocalRefWrapper<JByteArray>(env,
				(JByteArray)env.CallStaticObjectMethodA(_gsrActivityClassId, _hashDataSha256MethodId, args));
			var sha256 = new byte[32];
			env.GetByteArrayRegion(javaSha256.LocalRef, 0, MemoryMarshal.Cast<byte, JByte>(sha256.AsSpan()));
			return sha256;
		}
	}

	public static int GetRandomInt32(int toExclusive)
	{
		var env = JNIEnvPtr.GetEnv();
		Span<JValue> args = stackalloc JValue[1];
		args[0].i = toExclusive;
		return env.CallStaticIntMethodA(_gsrActivityClassId, _getRandomInt32MethodId, args);
	}
}
