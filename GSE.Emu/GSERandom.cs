// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#if !GSE_ANDROID
using System.Security.Cryptography;
#endif

#if GSE_ANDROID
using GSE.Android;
#endif

namespace GSE.Emu;

/// <summary>
/// Workaround System.Security.Cryptography being unavailable on Android
/// </summary>
internal static class GSERandom
{
	public static int GetInt32(int toExclusive)
	{
#if !GSE_ANDROID
		return RandomNumberGenerator.GetInt32(toExclusive);
#else
		return AndroidCryptography.GetRandomInt32(toExclusive);
#endif
	}
}
