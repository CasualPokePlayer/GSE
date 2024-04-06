// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#if GSR_ANDROID

using System;
using System.Runtime.InteropServices;

namespace GSR.Android;

/// <summary>
/// System.Security.Cryptography seems to be broken on Android
/// We work around this by directly using the Java hashing methods with JNI
/// (TODO: Why is this needed? .NET implementation seems to also just use JNI to call the same Java functions...)
/// </summary>
internal static class AndroidHash
{
	private static JNIEnvPtr _env;

	private static JClass _gsrActivityClassId;

	private static JMethodID _hashDataSha256MethodId;

	public static void InitializeJNI()
	{
		_env = JNIEnvPtr.GetEnv();

		_gsrActivityClassId = _env.FindClass("org/psr/gsr/GSRActivity"u8);
		if (_gsrActivityClassId == 0)
		{
			throw new("Failed to find GSR class ID");
		}

		_hashDataSha256MethodId = _env.GetStaticMethodID(_gsrActivityClassId, "HashDataSHA256"u8, "(Ljava/nio/ByteBuffer;)[B"u8);
		if (_hashDataSha256MethodId == 0)
		{
			throw new("Failed to find HashDataSHA256 method ID");
		}
	}

	public static unsafe byte[] HashDataSHA256(ReadOnlySpan<byte> data)
	{
		JByteArray javaSha256;
		fixed (byte* dataPtr = data)
		{
			var bb = _env.NewDirectByteBuffer(dataPtr, data.Length);
			if (bb == 0)
			{
				throw new("Failed to allocate Java byte buffer");
			}

			Span<JValue> args = stackalloc JValue[1];
			args[0].l = bb;
			javaSha256 = _env.CallStaticObjectMethodA(_gsrActivityClassId, _hashDataSha256MethodId, args);
			_env.DeleteLocalRef(bb);
		}

		if (javaSha256 == 0)
		{
			throw new("Failed to hash byte buffer");
		}

		var sha256 = new byte[32];
		_env.GetByteArrayRegion(javaSha256, 0, MemoryMarshal.Cast<byte, JByte>(sha256.AsSpan()));
		_env.DeleteLocalRef(javaSha256);
		return sha256;
	}
}

#endif
