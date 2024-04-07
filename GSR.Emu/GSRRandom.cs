// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#if !GSR_ANDROID
using System.Security.Cryptography;
#endif

#if GSR_ANDROID
using GSR.Android;
#endif

namespace GSR.Emu;

/// <summary>
/// Workaround System.Security.Cryptography being unavailable on Android
/// </summary>
internal static class GSRRandom
{
	public static int GetInt32(int toExclusive)
	{
#if !GSR_ANDROID
		return RandomNumberGenerator.GetInt32(toExclusive);
#else
		return AndroidCryptography.GetRandomInt32(toExclusive);
#endif
	}
}
