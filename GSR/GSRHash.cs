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
/// Workaround hashing methods being broken on Android for some reason
/// </summary>
internal static class GSRHash
{
	public static byte[] HashDataSHA256(ReadOnlySpan<byte> source)
	{
#if !GSR_ANDROID
		return SHA256.HashData(source);
#else
		return AndroidHash.HashDataSHA256(source);
#endif
	}
}
