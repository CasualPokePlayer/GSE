// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
#if !GSE_ANDROID
using System.Security.Cryptography;
#endif

#if GSE_ANDROID
using GSE.Android;
#endif

namespace GSE;

/// <summary>
/// Workaround System.Security.Cryptography being unavailable on Android
/// </summary>
internal static class GSEHash
{
	public static byte[] HashDataSHA256(ReadOnlySpan<byte> source)
	{
#if !GSE_ANDROID
		return SHA256.HashData(source);
#else
		return AndroidCryptography.HashDataSHA256(source);
#endif
	}
}
