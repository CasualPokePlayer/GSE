// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Runtime.CompilerServices;

namespace GSR.Android.JNI;

internal class JNIException : Exception
{
	private JNIException(string message)
		: base(message)
	{
	}

	public static unsafe JNIException GetExceptionFromJava(JNIEnvPtr envPtr)
	{
		// JNIEnvPtr functions may call GetExceptionFromJava (or a function that does so)
		// to avoid possible recursion, we'll directly use JNIEnv*
		// JNIEnvPtr as a value is the same as JNIEnv*, so we can just cast it
		var env = (JNIEnv*)Unsafe.As<JNIEnvPtr, nint>(ref envPtr);
		if (!env->Vtbl->ExceptionCheck(env))
		{
			return null;
		}

		var jException = env->Vtbl->ExceptionOccurred(env);
		env->Vtbl->ExceptionClear(env);

		var jExceptionClass = env->Vtbl->GetObjectClass(env, jException);

		try
		{
			JMethodID getMessageMethodId;
			fixed (byte* name = "getMessage"u8, sig = "()Ljava/lang/String;"u8)
			{
				getMessageMethodId = env->Vtbl->GetMethodID(env, jExceptionClass, name, sig);
			}

			if (getMessageMethodId.IsNull)
			{
				env->Vtbl->ExceptionClear(env);
				throw new("Unknown Java exception occurred (GetMethodID for Java exception getMessage failed?)");
			}

			var jmessage = (JString)env->Vtbl->CallObjectMethodA(env, jException, getMessageMethodId, null);
			if (env->Vtbl->ExceptionCheck(env) || jmessage.IsNull)
			{
				env->Vtbl->ExceptionClear(env);
				throw new("Unknown Java exception occurred (CallObjectMethodA for Java exception getMessage failed?)");
			}

			try
			{
				var jmessageLen = env->Vtbl->GetStringLength(env, jmessage);
				var message = jmessageLen > 1024 ? new char[jmessageLen] : stackalloc char[jmessageLen];
				fixed (char* messagePtr = message)
				{
					env->Vtbl->GetStringRegion(env, jmessage, 0, jmessageLen, (JChar*)messagePtr);
				}

				if (env->Vtbl->ExceptionCheck(env))
				{
					env->Vtbl->ExceptionClear(env);
					throw new("Unknown Java exception occurred (GetStringRegion for Java exception string failed?)");
				}

				return new(new(message));
			}
			finally
			{
				env->Vtbl->DeleteLocalRef(env, jmessage);
			}
		}
		finally
		{
			env->Vtbl->DeleteLocalRef(env, jExceptionClass);
			env->Vtbl->DeleteLocalRef(env, jException);
		}
	}

	public static void ThrowForError(JNIEnvPtr env, string envFunc)
	{
		throw GetExceptionFromJava(env) ?? new($"{envFunc} failed, but no Java exception was present");
	}

	public static void ThrowIfExceptionPending(JNIEnvPtr env)
	{
		var jniEx = GetExceptionFromJava(env);
		if (jniEx != null)
		{
			throw jniEx;
		}
	}

	private const int JNI_EDETACHED = -2;
	private const int JNI_EVERSION = -3;

	public static string GetErrorCodeString(JInt errorCode)
	{
		return (int)errorCode switch
		{
			JNI_EDETACHED => "JNI_EDETACHED",
			JNI_EVERSION => "JNI_EVERSION",
			_ => $"Unknown ({errorCode})"
		};
	}

	public static void ThrowForErrorCode(JInt errorCode, string envFunc)
	{
		throw new JNIException($"{GetErrorCodeString(errorCode)} error code occurred for {envFunc}");
	}
}
