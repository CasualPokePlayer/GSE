// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
#if !GSR_ANDROID
using System.Security.Cryptography;
#endif

#if GSR_ANDROID
using GSR.Android;
#endif

namespace GSR;

/// <summary>
/// Workaround System.Security.Cryptography being unavailable on Android
/// </summary>
internal static class GSRHash
{
	public static byte[] HashDataSHA256(ReadOnlySpan<byte> source)
	{
#if !GSR_ANDROID
		return SHA256.HashData(source);
#else
		return AndroidCryptography.HashDataSHA256(source);
#endif
	}
}
