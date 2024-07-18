// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System.Runtime.CompilerServices;

namespace GSE.Android.JNI;

internal ref struct LocalRefWrapper<T>(JNIEnvPtr Env, T Obj)
	where T : unmanaged
{
	public readonly T LocalRef => Obj;

	public void Dispose()
	{
		// kind of a hack due to constraint limitations
		Env.DeleteLocalRef(Unsafe.As<T, JObject>(ref Obj));
	}
}
