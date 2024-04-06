// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

//#if GSR_ANDROID

// types defined by https://docs.oracle.com/javase/6/docs/technotes/guides/jni/spec/types.html

global using JBoolean = byte;
global using JByte = sbyte;
global using JChar = ushort;
global using JShort = short;
global using JInt = int;
global using JLong = long;
global using JFloat = float;
global using JDouble = double;

global using JSize = int;

global using JObject = nint;
global using JClass = nint;
global using JString = nint;
global using JArray = nint;
global using JObjectArray = nint;
global using JBooleanArray = nint;
global using JByteArray = nint;
global using JCharArray = nint;
global using JShortArray = nint;
global using JIntArray = nint;
global using JLongArray = nint;
global using JFloatArray = nint;
global using JDoubleArray = nint;
global using JThrowable = nint;
global using JWeak = nint;

global using JFieldID = nint;

global using JMethodID = nint;

using System;
using System.Runtime.InteropServices;

using static SDL2.SDL;

// ReSharper disable UnusedMember.Global

namespace GSR.Android;

[StructLayout(LayoutKind.Explicit)]
public struct JValue
{
	[FieldOffset(0)]
	public JBoolean z;
	[FieldOffset(0)]
	public JByte b;
	[FieldOffset(0)]
	public JChar c;
	[FieldOffset(0)]
	public JShort s;
	[FieldOffset(0)]
	public JInt i;
	[FieldOffset(0)]
	public JLong j;
	[FieldOffset(0)]
	public JFloat f;
	[FieldOffset(0)]
	public JDouble d;
	[FieldOffset(0)]
	public JObject l;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct JNINativeMethod
{
	public byte* Name;
	public byte* Signature;
	public void* FnPtr;
}

/// <summary>
/// JNINativeInterface as described by https://docs.oracle.com/javase/6/docs/technotes/guides/jni/spec/jniTOC.html
/// Note that various functions have V and A postfix variants
/// For these functions, the base function and the V variant cannot be used, only the A variant can be used
/// (base function uses variadic args, V variant uses va_list, neither are marshallable in C#)
/// Also note that we use SDL's JNI env, which will be using JNI v1.4 (therefore we don't support functions past that)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct JNINativeInterface
{
	public void* _reserved0;
	public void* _reserved1;
	public void* _reserved2;
	public void* _reserved3;

	public delegate* unmanaged<JNIEnv*, int> GetVersion;

	public delegate* unmanaged<JNIEnv*, byte*, JObject, JByte*, JSize, JClass> DefineClass;
	public delegate* unmanaged<JNIEnv*, byte*, JClass> FindClass;

	public delegate* unmanaged<JNIEnv*, JObject, JMethodID> FromReflectedMethod;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID> FromReflectedField;
	public delegate* unmanaged<JNIEnv*, JClass, JMethodID, JBoolean, JObject> ToReflectedMethod;

	public delegate* unmanaged<JNIEnv*, JClass, JClass> GetSuperclass;
	public delegate* unmanaged<JNIEnv*, JClass, JClass, JBoolean> IsAssignableFrom;

	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JBoolean, JObject> ToReflectedField;

	public delegate* unmanaged<JNIEnv*, JThrowable, JInt> Throw;
	public delegate* unmanaged<JNIEnv*, JClass, byte*, JInt> ThrowNew;
	public delegate* unmanaged<JNIEnv*, JThrowable> ExceptionOccurred;
	public delegate* unmanaged<JNIEnv*, void> ExceptionDescribe;
	public delegate* unmanaged<JNIEnv*, void> ExceptionClear;
	public delegate* unmanaged<JNIEnv*, byte*, void> FatalError;

	public delegate* unmanaged<JNIEnv*, JInt, JInt> PushLocalFrame;
	public delegate* unmanaged<JNIEnv*, JObject, JObject> PopLocalFrame;

	public delegate* unmanaged<JNIEnv*, JObject, JObject> NewGlobalRef;
	public delegate* unmanaged<JNIEnv*, JObject, void> DeleteGlobalRef;
	public delegate* unmanaged<JNIEnv*, JObject, void> DeleteLocalRef;
	public delegate* unmanaged<JNIEnv*, JObject, JObject, JBoolean> IsSameObject;
	public delegate* unmanaged<JNIEnv*, JObject, JObject> NewLocalRef;
	public delegate* unmanaged<JNIEnv*, JInt, JInt> EnsureLocalCapacity;

	public delegate* unmanaged<JNIEnv*, JClass, JObject> AllocObject;
	public void* NewObject;
	public void* NewObjectV;
	public delegate* unmanaged<JNIEnv*, JClass, JMethodID, JValue*, JObject> NewObjectA;

	public delegate* unmanaged<JNIEnv*, JObject, JClass> GetObjectClass;
	public delegate* unmanaged<JNIEnv*, JObject, JClass, JBoolean> IsInstanceOf;

	public delegate* unmanaged<JNIEnv*, JClass, byte*, byte*, JMethodID> GetMethodID;

	public void* CallObjectMethod;
	public void* CallObjectMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JMethodID, JValue*, JObject> CallObjectMethodA;
	public void* CallBooleanMethod;
	public void* CallBooleanMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JMethodID, JValue*, JBoolean> CallBooleanMethodA;
	public void* CallByteMethod;
	public void* CallByteMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JMethodID, JValue*, JByte> CallByteMethodA;
	public void* CallCharMethod;
	public void* CallCharMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JMethodID, JValue*, JChar> CallCharMethodA;
	public void* CallShortMethod;
	public void* CallShortMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JMethodID, JValue*, JShort> CallShortMethodA;
	public void* CallIntMethod;
	public void* CallIntMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JMethodID, JValue*, JInt> CallIntMethodA;
	public void* CallLongMethod;
	public void* CallLongMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JMethodID, JValue*, JLong> CallLongMethodA;
	public void* CallFloatMethod;
	public void* CallFloatMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JMethodID, JValue*, JFloat> CallFloatMethodA;
	public void* CallDoubleMethod;
	public void* CallDoubleMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JMethodID, JValue*, JDouble> CallDoubleMethodA;
	public void* CallVoidMethod;
	public void* CallVoidMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JMethodID, JValue*, void> CallVoidMethodA;

	public void* CallNonvirtualObjectMethod;
	public void* CallNonvirtualObjectMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JClass, JMethodID, JValue*, JObject> CallNonvirtualObjectMethodA;
	public void* CallNonvirtualBooleanMethod;
	public void* CallNonvirtualBooleanMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JClass, JMethodID, JValue*, JBoolean> CallNonvirtualBooleanMethodA;
	public void* CallNonvirtualByteMethod;
	public void* CallNonvirtualByteMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JClass, JMethodID, JValue*, JByte> CallNonvirtualByteMethodA;
	public void* CallNonvirtualCharMethod;
	public void* CallNonvirtualCharMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JClass, JMethodID, JValue*, JChar> CallNonvirtualCharMethodA;
	public void* CallNonvirtualShortMethod;
	public void* CallNonvirtualShortMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JClass, JMethodID, JValue*, JShort> CallNonvirtualShortMethodA;
	public void* CallNonvirtualIntMethod;
	public void* CallNonvirtualIntMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JClass, JMethodID, JValue*, JInt> CallNonvirtualIntMethodA;
	public void* CallNonvirtualLongMethod;
	public void* CallNonvirtualLongMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JClass, JMethodID, JValue*, JLong> CallNonvirtualLongMethodA;
	public void* CallNonvirtualFloatMethod;
	public void* CallNonvirtualFloatMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JClass, JMethodID, JValue*, JFloat> CallNonvirtualFloatMethodA;
	public void* CallNonvirtualDoubleMethod;
	public void* CallNonvirtualDoubleMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JClass, JMethodID, JValue*, JDouble> CallNonvirtualDoubleMethodA;
	public void* CallNonvirtualVoidMethod;
	public void* CallNonvirtualVoidMethodV;
	public delegate* unmanaged<JNIEnv*, JObject, JClass, JMethodID, JValue*, void> CallNonvirtualVoidMethodA;

	public delegate* unmanaged<JNIEnv*, JClass, byte*, byte*, JFieldID> GetFieldID;

	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JObject> GetObjectField;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JBoolean> GetBooleanField;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JByte> GetByteField;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JChar> GetCharField;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JShort> GetShortField;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JInt> GetIntField;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JLong> GetLongField;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JFloat> GetFloatField;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JDouble> GetDoubleField;

	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JObject, void> SetObjectField;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JBoolean, void> SetBooleanField;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JByte, void> SetByteField;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JChar, void> SetCharField;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JShort, void> SetShortField;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JInt, void> SetIntField;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JLong, void> SetLongField;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JFloat, void> SetFloatField;
	public delegate* unmanaged<JNIEnv*, JObject, JFieldID, JDouble, void> SetDoubleField;

	public delegate* unmanaged<JNIEnv*, JClass, byte*, byte*, JMethodID> GetStaticMethodID;

	public void* CallStaticObjectMethod;
	public void* CallStaticObjectMethodV;
	public delegate* unmanaged<JNIEnv*, JClass, JMethodID, JValue*, JObject> CallStaticObjectMethodA;
	public void* CallStaticBooleanMethod;
	public void* CallStaticBooleanMethodV;
	public delegate* unmanaged<JNIEnv*, JClass, JMethodID, JValue*, JBoolean> CallStaticBooleanMethodA;
	public void* CallStaticByteMethod;
	public void* CallStaticByteMethodV;
	public delegate* unmanaged<JNIEnv*, JClass, JMethodID, JValue*, JByte> CallStaticByteMethodA;
	public void* CallStaticCharMethod;
	public void* CallStaticCharMethodV;
	public delegate* unmanaged<JNIEnv*, JClass, JMethodID, JValue*, JChar> CallStaticCharMethodA;
	public void* CallStaticShortMethod;
	public void* CallStaticShortMethodV;
	public delegate* unmanaged<JNIEnv*, JClass, JMethodID, JValue*, JShort> CallStaticShortMethodA;
	public void* CallStaticIntMethod;
	public void* CallStaticIntMethodV;
	public delegate* unmanaged<JNIEnv*, JClass, JMethodID, JValue*, JInt> CallStaticIntMethodA;
	public void* CallStaticLongMethod;
	public void* CallStaticLongMethodV;
	public delegate* unmanaged<JNIEnv*, JClass, JMethodID, JValue*, JLong> CallStaticLongMethodA;
	public void* CallStaticFloatMethod;
	public void* CallStaticFloatMethodV;
	public delegate* unmanaged<JNIEnv*, JClass, JMethodID, JValue*, JFloat> CallStaticFloatMethodA;
	public void* CallStaticDoubleMethod;
	public void* CallStaticDoubleMethodV;
	public delegate* unmanaged<JNIEnv*, JClass, JMethodID, JValue*, JDouble> CallStaticDoubleMethodA;
	public void* CallStaticVoidMethod;
	public void* CallStaticVoidMethodV;
	public delegate* unmanaged<JNIEnv*, JClass, JMethodID, JValue*, void> CallStaticVoidMethodA;

	public delegate* unmanaged<JNIEnv*, JClass, byte*, byte*, JFieldID> GetStaticFieldID;

	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JObject> GetStaticObjectField;
	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JBoolean> GetStaticBooleanField;
	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JByte> GetStaticByteField;
	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JChar> GetStaticCharField;
	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JShort> GetStaticShortField;
	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JInt> GetStaticIntField;
	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JLong> GetStaticLongField;
	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JFloat> GetStaticFloatField;
	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JDouble> GetStaticDoubleField;

	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JObject, void> SetStaticObjectField;
	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JBoolean, void> SetStaticBooleanField;
	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JByte, void> SetStaticByteField;
	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JChar, void> SetStaticCharField;
	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JShort, void> SetStaticShortField;
	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JInt, void> SetStaticIntField;
	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JLong, void> SetStaticLongField;
	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JFloat, void> SetStaticFloatField;
	public delegate* unmanaged<JNIEnv*, JClass, JFieldID, JDouble, void> SetStaticDoubleField;

	public delegate* unmanaged<JNIEnv*, JChar*, JSize, JString> NewString;

	public delegate* unmanaged<JNIEnv*, JString, JSize> GetStringLength;
	public delegate* unmanaged<JNIEnv*, JString, JBoolean*, JChar*> GetStringChars;
	public delegate* unmanaged<JNIEnv*, JString, JChar*, void> ReleaseStringChars;

	public delegate* unmanaged<JNIEnv*, byte*, JString> NewStringUTF;
	public delegate* unmanaged<JNIEnv*, JString, JSize> GetStringUTFLength;
	public delegate* unmanaged<JNIEnv*, JString, JBoolean*, byte*> GetStringUTFChars;
	public delegate* unmanaged<JNIEnv*, JString, byte*, void> ReleaseStringUTFChars;

	public delegate* unmanaged<JNIEnv*, JArray, JSize> GetArrayLength;

	public delegate* unmanaged<JNIEnv*, JSize, JClass, JObject, JObjectArray> NewObjectArray;
	public delegate* unmanaged<JNIEnv*, JObjectArray, JSize, JObject> GetObjectArrayElement;
	public delegate* unmanaged<JNIEnv*, JObjectArray, JSize, JObject, void> SetObjectArrayElement;

	public delegate* unmanaged<JNIEnv*, JSize, JBooleanArray> NewBooleanArray;
	public delegate* unmanaged<JNIEnv*, JSize, JByteArray> NewByteArray;
	public delegate* unmanaged<JNIEnv*, JSize, JCharArray> NewCharArray;
	public delegate* unmanaged<JNIEnv*, JSize, JShortArray> NewShortArray;
	public delegate* unmanaged<JNIEnv*, JSize, JIntArray> NewIntArray;
	public delegate* unmanaged<JNIEnv*, JSize, JLongArray> NewLongArray;
	public delegate* unmanaged<JNIEnv*, JSize, JFloatArray> NewFloatArray;
	public delegate* unmanaged<JNIEnv*, JSize, JDoubleArray> NewDoubleArray;

	public delegate* unmanaged<JNIEnv*, JBooleanArray, JBoolean*, JBoolean*> GetBooleanArrayElements;
	public delegate* unmanaged<JNIEnv*, JByteArray, JBoolean*, JByte*> GetByteArrayElements;
	public delegate* unmanaged<JNIEnv*, JCharArray, JBoolean*, JChar*> GetCharArrayElements;
	public delegate* unmanaged<JNIEnv*, JShortArray, JBoolean*, JShort*> GetShortArrayElements;
	public delegate* unmanaged<JNIEnv*, JIntArray, JBoolean*, JInt*> GetIntArrayElements;
	public delegate* unmanaged<JNIEnv*, JLongArray, JBoolean*, JLong*> GetLongArrayElements;
	public delegate* unmanaged<JNIEnv*, JFloatArray, JBoolean*, JFloat*> GetFloatArrayElements;
	public delegate* unmanaged<JNIEnv*, JDoubleArray, JBoolean*, JDouble*> GetDoubleArrayElements;

	public delegate* unmanaged<JNIEnv*, JBooleanArray, JBoolean*, JInt, void> ReleaseBooleanArrayElements;
	public delegate* unmanaged<JNIEnv*, JByteArray, JByte*, JInt, void> ReleaseByteArrayElements;
	public delegate* unmanaged<JNIEnv*, JCharArray, JChar*, JInt, void> ReleaseCharArrayElements;
	public delegate* unmanaged<JNIEnv*, JShortArray, JShort*, JInt, void> ReleaseShortArrayElements;
	public delegate* unmanaged<JNIEnv*, JIntArray, JInt*, JInt, void> ReleaseIntArrayElements;
	public delegate* unmanaged<JNIEnv*, JLongArray, JLong*, JInt, void> ReleaseLongArrayElements;
	public delegate* unmanaged<JNIEnv*, JFloatArray, JFloat*, JInt, void> ReleaseFloatArrayElements;
	public delegate* unmanaged<JNIEnv*, JDoubleArray, JDouble*, JInt, void> ReleaseDoubleArrayElements;

	public delegate* unmanaged<JNIEnv*, JBooleanArray, JSize, JSize, JBoolean*, void> GetBooleanArrayRegion;
	public delegate* unmanaged<JNIEnv*, JByteArray, JSize, JSize, JByte*, void> GetByteArrayRegion;
	public delegate* unmanaged<JNIEnv*, JCharArray, JSize, JSize, JChar*, void> GetCharArrayRegion;
	public delegate* unmanaged<JNIEnv*, JShortArray, JSize, JSize, JShort*, void> GetShortArrayRegion;
	public delegate* unmanaged<JNIEnv*, JIntArray, JSize, JSize, JInt*, void> GetIntArrayRegion;
	public delegate* unmanaged<JNIEnv*, JLongArray, JSize, JSize, JLong*, void> GetLongArrayRegion;
	public delegate* unmanaged<JNIEnv*, JFloatArray, JSize, JSize, JFloat*, void> GetFloatArrayRegion;
	public delegate* unmanaged<JNIEnv*, JDoubleArray, JSize, JSize, JDouble*, void> GetDoubleArrayRegion;

	public delegate* unmanaged<JNIEnv*, JBooleanArray, JSize, JSize, JBoolean*, void> SetBooleanArrayRegion;
	public delegate* unmanaged<JNIEnv*, JByteArray, JSize, JSize, JByte*, void> SetByteArrayRegion;
	public delegate* unmanaged<JNIEnv*, JCharArray, JSize, JSize, JChar*, void> SetCharArrayRegion;
	public delegate* unmanaged<JNIEnv*, JShortArray, JSize, JSize, JShort*, void> SetShortArrayRegion;
	public delegate* unmanaged<JNIEnv*, JIntArray, JSize, JSize, JInt*, void> SetIntArrayRegion;
	public delegate* unmanaged<JNIEnv*, JLongArray, JSize, JSize, JLong*, void> SetLongArrayRegion;
	public delegate* unmanaged<JNIEnv*, JFloatArray, JSize, JSize, JFloat*, void> SetFloatArrayRegion;
	public delegate* unmanaged<JNIEnv*, JDoubleArray, JSize, JSize, JDouble*, void> SetDoubleArrayRegion;

	public delegate* unmanaged<JNIEnv*, JClass, JNINativeMethod*, JInt, JInt> RegisterNatives;
	public delegate* unmanaged<JNIEnv*, JClass, JInt> UnregisterNatives;

	public delegate* unmanaged<JNIEnv*, JObject, JInt> MonitorEnter;
	public delegate* unmanaged<JNIEnv*, JObject, JInt> MonitorExit;

	public delegate* unmanaged<JNIEnv*, void**, JInt> GetJavaVM;

	public delegate* unmanaged<JNIEnv*, JString, JSize, JSize, JChar*, void> GetStringRegion;
	public delegate* unmanaged<JNIEnv*, JString, JSize, JSize, byte*, void> GetStringUTFRegion;

	public delegate* unmanaged<JNIEnv*, JArray, JBoolean*, void*> GetPrimitiveArrayCritical;
	public delegate* unmanaged<JNIEnv*, JArray, void*, JInt, void> ReleasePrimitiveArrayCritical;

	public delegate* unmanaged<JNIEnv*, JString, JBoolean*, JChar*> GetStringCritical;
	public delegate* unmanaged<JNIEnv*, JString, JChar*, void> ReleaseStringCritical;

	public delegate* unmanaged<JNIEnv*, JObject, JWeak> NewWeakGlobalRef;
	public delegate* unmanaged<JNIEnv*, JWeak, void> DeleteWeakGlobalRef;

	public delegate* unmanaged<JNIEnv*, JBoolean> ExceptionCheck;

	public delegate* unmanaged<JNIEnv*, void*, JLong, JObject> NewDirectByteBuffer;
	public delegate* unmanaged<JNIEnv*, JObject, void*> GetDirectBufferAddress;
	public delegate* unmanaged<JNIEnv*, JObject, JLong> GetDirectBufferCapacity;
}

public static class JNI
{
	public const JBoolean TRUE = 1;
	public const JBoolean FALSE = 0;
}

/// <summary>
/// JNIEnv is just a typedef to JNINativeInterface*
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct JNIEnv
{
	public JNINativeInterface* Vtbl;
}

/// <summary>
/// Wraps JNIEnv*
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct JNIEnvPtr
{
	private readonly JNIEnv* _jniEnv;

	private JNIEnvPtr(nint jniEnv)
	{
		_jniEnv = (JNIEnv*)jniEnv;
	}

	public static JNIEnvPtr GetEnv()
	{
		var jniEnv = SDL_AndroidGetJNIEnv();
		if (jniEnv == 0)
		{
			throw new($"Failed to obtain JNI env, SDL error: {SDL_GetError()}");
		}

		return new(jniEnv);
	}

	public int GetVersion()
	{
		return _jniEnv->Vtbl->GetVersion(_jniEnv);
	}

	public JClass DefineClass(ReadOnlySpan<byte> name, JObject loader, ReadOnlySpan<JByte> buf)
	{
		fixed (byte* namePtr = name)
		fixed (JByte* bufPtr = buf)
		{
			return _jniEnv->Vtbl->DefineClass(_jniEnv, namePtr, loader, bufPtr, buf.Length);
		}
	}

	public JClass FindClass(ReadOnlySpan<byte> name)
	{
		fixed (byte* namePtr = name)
		{
			return _jniEnv->Vtbl->FindClass(_jniEnv, namePtr);
		}
	}

	public JMethodID FromReflectedMethod(JObject method)
	{
		return _jniEnv->Vtbl->FromReflectedMethod(_jniEnv, method);
	}

	public JFieldID FromReflectedField(JObject field)
	{
		return _jniEnv->Vtbl->FromReflectedMethod(_jniEnv, field);
	}

	public JObject ToReflectedMethod(JClass cls, JMethodID methodID, bool isStatic)
	{
		return _jniEnv->Vtbl->ToReflectedMethod(_jniEnv, cls, methodID, isStatic ? JNI.TRUE : JNI.FALSE);
	}

	public JClass GetSuperclass(JClass clazz)
	{
		return _jniEnv->Vtbl->GetSuperclass(_jniEnv, clazz);
	}

	public bool IsAssignableFrom(JClass clazz1, JClass clazz2)
	{
		return _jniEnv->Vtbl->IsAssignableFrom(_jniEnv, clazz1, clazz2) != JNI.FALSE;
	}

	public JObject ToReflectedField(JClass cls, JFieldID fieldID, bool isStatic)
	{
		return _jniEnv->Vtbl->ToReflectedField(_jniEnv, cls, fieldID, isStatic ? JNI.TRUE : JNI.FALSE);
	}

	public JInt Throw(JThrowable obj)
	{
		return _jniEnv->Vtbl->Throw(_jniEnv, obj);
	}

	public JInt ThrowNew(JClass clazz, ReadOnlySpan<byte> message)
	{
		fixed (byte* messagePtr = message)
		{
			return _jniEnv->Vtbl->ThrowNew(_jniEnv, clazz, messagePtr);
		}
	}

	public JThrowable ExceptionOccurred()
	{
		return _jniEnv->Vtbl->ExceptionOccurred(_jniEnv);
	}

	public void ExceptionDescribe()
	{
		_jniEnv->Vtbl->ExceptionDescribe(_jniEnv);
	}

	public void ExceptionClear()
	{
		_jniEnv->Vtbl->ExceptionClear(_jniEnv);
	}

	public void FatalError(ReadOnlySpan<byte> msg)
	{
		fixed (byte* msgPtr = msg)
		{
			_jniEnv->Vtbl->FatalError(_jniEnv, msgPtr);
		}
	}

	public JInt PushLocalFrame(JInt capacity)
	{
		return _jniEnv->Vtbl->PushLocalFrame(_jniEnv, capacity);
	}

	public JObject PopLocalFrame(JObject result)
	{
		return _jniEnv->Vtbl->PopLocalFrame(_jniEnv, result);
	}

	public JObject NewGlobalRef(JObject obj)
	{
		return _jniEnv->Vtbl->NewGlobalRef(_jniEnv, obj);
	}

	public void DeleteGlobalRef(JObject globalRef)
	{
		_jniEnv->Vtbl->DeleteGlobalRef(_jniEnv, globalRef);
	}

	public void DeleteLocalRef(JObject localRef)
	{
		_jniEnv->Vtbl->DeleteLocalRef(_jniEnv, localRef);
	}

	public bool IsSameObject(JObject ref1, JObject ref2)
	{
		return _jniEnv->Vtbl->IsSameObject(_jniEnv, ref1, ref2) != JNI.FALSE;
	}

	public JObject NewLocalRef(JObject ref_)
	{
		return _jniEnv->Vtbl->NewLocalRef(_jniEnv, ref_);
	}

	public JInt EnsureLocalCapacity(JInt capacity)
	{
		return _jniEnv->Vtbl->EnsureLocalCapacity(_jniEnv, capacity);
	}

	public JObject AllocObject(JClass clazz)
	{
		return _jniEnv->Vtbl->AllocObject(_jniEnv, clazz);
	}

	public JObject NewObjectA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->NewObjectA(_jniEnv, clazz, methodID, argsPtr);
		}
	}

	public JClass GetObjectClass(JObject obj)
	{
		return _jniEnv->Vtbl->GetObjectClass(_jniEnv, obj);
	}

	public bool IsInstanceOf(JObject obj, JClass clazz)
	{
		return _jniEnv->Vtbl->IsInstanceOf(_jniEnv, obj, clazz) != JNI.FALSE;
	}

	public JMethodID GetMethodID(JClass clazz, ReadOnlySpan<byte> name, ReadOnlySpan<byte> sig)
	{
		fixed (byte* namePtr = name, sigPtr = sig)
		{
			return _jniEnv->Vtbl->GetMethodID(_jniEnv, clazz, namePtr, sigPtr);
		}
	}

	public JObject CallObjectMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallObjectMethodA(_jniEnv, obj, methodID, argsPtr);
		}
	}

	public JBoolean CallBooleanMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallBooleanMethodA(_jniEnv, obj, methodID, argsPtr);
		}
	}

	public JByte CallByteMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallByteMethodA(_jniEnv, obj, methodID, argsPtr);
		}
	}

	public char CallCharMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return (char)_jniEnv->Vtbl->CallCharMethodA(_jniEnv, obj, methodID, argsPtr);
		}
	}

	public JShort CallShortMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallShortMethodA(_jniEnv, obj, methodID, argsPtr);
		}
	}

	public JInt CallIntMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallIntMethodA(_jniEnv, obj, methodID, argsPtr);
		}
	}

	public JLong CallLongMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallLongMethodA(_jniEnv, obj, methodID, argsPtr);
		}
	}

	public JFloat CallFloatMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallFloatMethodA(_jniEnv, obj, methodID, argsPtr);
		}
	}

	public JDouble CallDoubleMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallDoubleMethodA(_jniEnv, obj, methodID, argsPtr);
		}
	}

	public void CallVoidMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			_jniEnv->Vtbl->CallVoidMethodA(_jniEnv, obj, methodID, argsPtr);
		}
	}

	public JObject CallNonvirtualObjectMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallNonvirtualObjectMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
		}
	}

	public JBoolean CallNonvirtualBooleanMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallNonvirtualBooleanMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
		}
	}

	public JByte CallNonvirtualByteMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallNonvirtualByteMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
		}
	}

	public char CallNonvirtualCharMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return (char)_jniEnv->Vtbl->CallNonvirtualCharMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
		}
	}

	public JShort CallNonvirtualShortMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallNonvirtualShortMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
		}
	}

	public JInt CallNonvirtualIntMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallNonvirtualIntMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
		}
	}

	public JLong CallNonvirtualLongMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallNonvirtualLongMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
		}
	}

	public JFloat CallNonvirtualFloatMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallNonvirtualFloatMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
		}
	}

	public JDouble CallNonvirtualDoubleMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallNonvirtualDoubleMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
		}
	}

	public void CallNonvirtualVoidMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			_jniEnv->Vtbl->CallNonvirtualVoidMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
		}
	}

	public JFieldID GetFieldID(JClass clazz, ReadOnlySpan<byte> name, ReadOnlySpan<byte> sig)
	{
		fixed (byte* namePtr = name, sigPtr = sig)
		{
			return _jniEnv->Vtbl->GetFieldID(_jniEnv, clazz, namePtr, sigPtr);
		}
	}

	public JObject GetObjectField(JObject obj, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetObjectField(_jniEnv, obj, fieldID);
	}

	public JBoolean GetBooleanField(JObject obj, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetBooleanField(_jniEnv, obj, fieldID);
	}

	public JByte GetByteField(JObject obj, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetByteField(_jniEnv, obj, fieldID);
	}

	public char GetCharField(JObject obj, JFieldID fieldID)
	{
		return (char)_jniEnv->Vtbl->GetCharField(_jniEnv, obj, fieldID);
	}

	public JShort GetShortField(JObject obj, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetShortField(_jniEnv, obj, fieldID);
	}

	public JInt GetIntField(JObject obj, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetIntField(_jniEnv, obj, fieldID);
	}

	public JLong GetLongField(JObject obj, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetLongField(_jniEnv, obj, fieldID);
	}

	public JFloat GetFloatField(JObject obj, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetFloatField(_jniEnv, obj, fieldID);
	}

	public JDouble GetDoubleField(JObject obj, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetDoubleField(_jniEnv, obj, fieldID);
	}

	public void SetObjectField(JObject obj, JFieldID fieldID, JObject value)
	{
		_jniEnv->Vtbl->SetObjectField(_jniEnv, obj, fieldID, value);
	}

	public void SetBooleanField(JObject obj, JFieldID fieldID, JBoolean value)
	{
		_jniEnv->Vtbl->SetBooleanField(_jniEnv, obj, fieldID, value);
	}

	public void SetByteField(JObject obj, JFieldID fieldID, JByte value)
	{
		_jniEnv->Vtbl->SetByteField(_jniEnv, obj, fieldID, value);
	}

	public void SetCharField(JObject obj, JFieldID fieldID, char value)
	{
		_jniEnv->Vtbl->SetCharField(_jniEnv, obj, fieldID, value);
	}

	public void SetShortField(JObject obj, JFieldID fieldID, JShort value)
	{
		_jniEnv->Vtbl->SetShortField(_jniEnv, obj, fieldID, value);
	}

	public void SetIntField(JObject obj, JFieldID fieldID, JInt value)
	{
		_jniEnv->Vtbl->SetIntField(_jniEnv, obj, fieldID, value);
	}

	public void SetLongField(JObject obj, JFieldID fieldID, JLong value)
	{
		_jniEnv->Vtbl->SetLongField(_jniEnv, obj, fieldID, value);
	}

	public void SetFloatField(JObject obj, JFieldID fieldID, JFloat value)
	{
		_jniEnv->Vtbl->SetFloatField(_jniEnv, obj, fieldID, value);
	}

	public void SetDoubleField(JObject obj, JFieldID fieldID, JDouble value)
	{
		_jniEnv->Vtbl->SetDoubleField(_jniEnv, obj, fieldID, value);
	}

	public JMethodID GetStaticMethodID(JClass clazz, ReadOnlySpan<byte> name, ReadOnlySpan<byte> sig)
	{
		fixed (byte* namePtr = name, sigPtr = sig)
		{
			return _jniEnv->Vtbl->GetStaticMethodID(_jniEnv, clazz, namePtr, sigPtr);
		}
	}

	public JObject CallStaticObjectMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallStaticObjectMethodA(_jniEnv, clazz, methodID, argsPtr);
		}
	}

	public JBoolean CallStaticBooleanMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallStaticBooleanMethodA(_jniEnv, clazz, methodID, argsPtr);
		}
	}

	public JByte CallStaticByteMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallStaticByteMethodA(_jniEnv, clazz, methodID, argsPtr);
		}
	}

	public char CallStaticCharMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return (char)_jniEnv->Vtbl->CallStaticCharMethodA(_jniEnv, clazz, methodID, argsPtr);
		}
	}

	public JShort CallStaticShortMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallStaticShortMethodA(_jniEnv, clazz, methodID, argsPtr);
		}
	}

	public JInt CallStaticIntMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallStaticIntMethodA(_jniEnv, clazz, methodID, argsPtr);
		}
	}

	public JLong CallStaticLongMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallStaticLongMethodA(_jniEnv, clazz, methodID, argsPtr);
		}
	}

	public JFloat CallStaticFloatMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallStaticFloatMethodA(_jniEnv, clazz, methodID, argsPtr);
		}
	}

	public JDouble CallStaticDoubleMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			return _jniEnv->Vtbl->CallStaticDoubleMethodA(_jniEnv, clazz, methodID, argsPtr);
		}
	}

	public void CallStaticVoidMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			_jniEnv->Vtbl->CallStaticVoidMethodA(_jniEnv, clazz, methodID, argsPtr);
		}
	}

	public JFieldID GetStaticFieldID(JClass clazz, ReadOnlySpan<byte> name, ReadOnlySpan<byte> sig)
	{
		fixed (byte* namePtr = name, sigPtr = sig)
		{
			return _jniEnv->Vtbl->GetStaticFieldID(_jniEnv, clazz, namePtr, sigPtr);
		}
	}

	public JObject GetStaticObjectField(JClass clazz, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetStaticObjectField(_jniEnv, clazz, fieldID);
	}

	public JBoolean GetStaticBooleanField(JClass clazz, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetStaticBooleanField(_jniEnv, clazz, fieldID);
	}

	public JByte GetStaticByteField(JClass clazz, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetStaticByteField(_jniEnv, clazz, fieldID);
	}

	public char GetStaticCharField(JClass clazz, JFieldID fieldID)
	{
		return (char)_jniEnv->Vtbl->GetStaticCharField(_jniEnv, clazz, fieldID);
	}

	public JShort GetStaticShortField(JClass clazz, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetStaticShortField(_jniEnv, clazz, fieldID);
	}

	public JInt GetStaticIntField(JClass clazz, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetStaticIntField(_jniEnv, clazz, fieldID);
	}

	public JLong GetStaticLongField(JClass clazz, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetStaticLongField(_jniEnv, clazz, fieldID);
	}

	public JFloat GetStaticFloatField(JClass clazz, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetStaticFloatField(_jniEnv, clazz, fieldID);
	}

	public JDouble GetStaticDoubleField(JClass clazz, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetStaticDoubleField(_jniEnv, clazz, fieldID);
	}

	public void SetStaticObjectField(JClass clazz, JFieldID fieldID, JObject value)
	{
		_jniEnv->Vtbl->SetStaticObjectField(_jniEnv, clazz, fieldID, value);
	}

	public void SetStaticBooleanField(JClass clazz, JFieldID fieldID, JBoolean value)
	{
		_jniEnv->Vtbl->SetStaticBooleanField(_jniEnv, clazz, fieldID, value);
	}

	public void SetStaticByteField(JClass clazz, JFieldID fieldID, JByte value)
	{
		_jniEnv->Vtbl->SetStaticByteField(_jniEnv, clazz, fieldID, value);
	}

	public void SetStaticCharField(JClass clazz, JFieldID fieldID, char value)
	{
		_jniEnv->Vtbl->SetStaticCharField(_jniEnv, clazz, fieldID, value);
	}

	public void SetStaticShortField(JClass clazz, JFieldID fieldID, JShort value)
	{
		_jniEnv->Vtbl->SetStaticShortField(_jniEnv, clazz, fieldID, value);
	}

	public void SetStaticIntField(JClass clazz, JFieldID fieldID, JInt value)
	{
		_jniEnv->Vtbl->SetStaticIntField(_jniEnv, clazz, fieldID, value);
	}

	public void SetStaticLongField(JClass clazz, JFieldID fieldID, JLong value)
	{
		_jniEnv->Vtbl->SetStaticLongField(_jniEnv, clazz, fieldID, value);
	}

	public void SetStaticFloatField(JClass clazz, JFieldID fieldID, JFloat value)
	{
		_jniEnv->Vtbl->SetStaticFloatField(_jniEnv, clazz, fieldID, value);
	}

	public void SetStaticDoubleField(JClass clazz, JFieldID fieldID, JDouble value)
	{
		_jniEnv->Vtbl->SetStaticDoubleField(_jniEnv, clazz, fieldID, value);
	}

	public JString NewString(ReadOnlySpan<char> unicodeChars)
	{
		fixed (char* unicodeCharsPtr = unicodeChars)
		{
			return _jniEnv->Vtbl->NewString(_jniEnv, (JChar*)unicodeCharsPtr, unicodeChars.Length);
		}
	}

	public JSize GetStringLength(JString string_)
	{
		return _jniEnv->Vtbl->GetStringLength(_jniEnv, string_);
	}

	public char* GetStringChars(JString string_)
	{
		return (char*)_jniEnv->Vtbl->GetStringChars(_jniEnv, string_, null);
	}

	public void ReleaseStringChars(JString string_, char* chars)
	{
		_jniEnv->Vtbl->ReleaseStringChars(_jniEnv, string_, (JChar*)chars);
	}

	public JString NewStringUTF(ReadOnlySpan<byte> bytes)
	{
		fixed (byte* bytesPtr = bytes)
		{
			return _jniEnv->Vtbl->NewStringUTF(_jniEnv, bytesPtr);
		}
	}

	public JSize GetStringUTFLength(JString string_)
	{
		return _jniEnv->Vtbl->GetStringUTFLength(_jniEnv, string_);
	}

	public byte* GetStringUTFChars(JString string_)
	{
		return _jniEnv->Vtbl->GetStringUTFChars(_jniEnv, string_, null);
	}

	public void ReleaseStringUTFChars(JString string_, byte* chars)
	{
		_jniEnv->Vtbl->ReleaseStringUTFChars(_jniEnv, string_, chars);
	}

	public JSize GetArrayLength(JArray array)
	{
		return _jniEnv->Vtbl->GetArrayLength(_jniEnv, array);
	}

	public JObjectArray NewObjectArray(JSize length, JClass elementClass, JObject initialElement)
	{
		return _jniEnv->Vtbl->NewObjectArray(_jniEnv, length, elementClass, initialElement);
	}

	public JObject GetObjectArrayElement(JObjectArray array, JSize index)
	{
		return _jniEnv->Vtbl->GetObjectArrayElement(_jniEnv, array, index);
	}

	public void SetObjectArrayElement(JObjectArray array, JSize index, JObject value)
	{
		_jniEnv->Vtbl->SetObjectArrayElement(_jniEnv, array, index, value);
	}

	public JBooleanArray NewBooleanArray(JSize length)
	{
		return _jniEnv->Vtbl->NewBooleanArray(_jniEnv, length);
	}

	public JByteArray NewByteArray(JSize length)
	{
		return _jniEnv->Vtbl->NewByteArray(_jniEnv, length);
	}

	public JCharArray NewCharArray(JSize length)
	{
		return _jniEnv->Vtbl->NewCharArray(_jniEnv, length);
	}

	public JShortArray NewShortArray(JSize length)
	{
		return _jniEnv->Vtbl->NewShortArray(_jniEnv, length);
	}

	public JIntArray NewIntArray(JSize length)
	{
		return _jniEnv->Vtbl->NewIntArray(_jniEnv, length);
	}

	public JLongArray NewLongArray(JSize length)
	{
		return _jniEnv->Vtbl->NewLongArray(_jniEnv, length);
	}

	public JFloatArray NewFloatArray(JSize length)
	{
		return _jniEnv->Vtbl->NewFloatArray(_jniEnv, length);
	}

	public JDoubleArray NewDoubleArray(JSize length)
	{
		return _jniEnv->Vtbl->NewDoubleArray(_jniEnv, length);
	}

	public bool* GetBooleanArrayElements(JBooleanArray array)
	{
		return (bool*)_jniEnv->Vtbl->GetBooleanArrayElements(_jniEnv, array, null);
	}

	public JByte* GetByteArrayElements(JByteArray array)
	{
		return _jniEnv->Vtbl->GetByteArrayElements(_jniEnv, array, null);
	}

	public char* GetCharArrayElements(JCharArray array)
	{
		return (char*)_jniEnv->Vtbl->GetCharArrayElements(_jniEnv, array, null);
	}

	public JShort* GetShortArrayElements(JShortArray array)
	{
		return _jniEnv->Vtbl->GetShortArrayElements(_jniEnv, array, null);
	}

	public JInt* GetIntArrayElements(JIntArray array)
	{
		return _jniEnv->Vtbl->GetIntArrayElements(_jniEnv, array, null);
	}

	public JLong* GetLongArrayElements(JLongArray array)
	{
		return _jniEnv->Vtbl->GetLongArrayElements(_jniEnv, array, null);
	}

	public JFloat* GetFloatArrayElements(JFloatArray array)
	{
		return _jniEnv->Vtbl->GetFloatArrayElements(_jniEnv, array, null);
	}

	public JDouble* GetDoubleArrayElements(JDoubleArray array)
	{
		return _jniEnv->Vtbl->GetDoubleArrayElements(_jniEnv, array, null);
	}

	public void ReleaseBooleanArrayElements(JBooleanArray array, bool* elems)
	{
		_jniEnv->Vtbl->ReleaseBooleanArrayElements(_jniEnv, array, (JBoolean*)elems, 0);
	}

	public void ReleaseByteArrayElements(JByteArray array, JByte* elems)
	{
		_jniEnv->Vtbl->ReleaseByteArrayElements(_jniEnv, array, elems, 0);
	}

	public void ReleaseCharArrayElements(JCharArray array, char* elems)
	{
		_jniEnv->Vtbl->ReleaseCharArrayElements(_jniEnv, array, (JChar*)elems, 0);
	}

	public void ReleaseShortArrayElements(JShortArray array, JShort* elems)
	{
		_jniEnv->Vtbl->ReleaseShortArrayElements(_jniEnv, array, elems, 0);
	}

	public void ReleaseIntArrayElements(JIntArray array, JInt* elems)
	{
		_jniEnv->Vtbl->ReleaseIntArrayElements(_jniEnv, array, elems, 0);
	}

	public void ReleaseLongArrayElements(JLongArray array, JLong* elems)
	{
		_jniEnv->Vtbl->ReleaseLongArrayElements(_jniEnv, array, elems, 0);
	}

	public void ReleaseFloatArrayElements(JFloatArray array, JFloat* elems)
	{
		_jniEnv->Vtbl->ReleaseFloatArrayElements(_jniEnv, array, elems, 0);
	}

	public void ReleaseDoubleArrayElements(JDoubleArray array, JDouble* elems)
	{
		_jniEnv->Vtbl->ReleaseDoubleArrayElements(_jniEnv, array, elems, 0);
	}

	public void GetBooleanArrayRegion(JBooleanArray array, JSize start, Span<bool> buf)
	{
		fixed (bool* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetBooleanArrayRegion(_jniEnv, array, start, buf.Length, (JBoolean*)bufPtr);
		}
	}

	public void GetByteArrayRegion(JByteArray array, JSize start, Span<JByte> buf)
	{
		fixed (JByte* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetByteArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
		}
	}

	public void GetCharArrayRegion(JCharArray array, JSize start, Span<char> buf)
	{
		fixed (char* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetCharArrayRegion(_jniEnv, array, start, buf.Length, (JChar*)bufPtr);
		}
	}

	public void GetShortArrayRegion(JShortArray array, JSize start, Span<JShort> buf)
	{
		fixed (JShort* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetShortArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
		}
	}

	public void GetIntArrayRegion(JIntArray array, JSize start, Span<JInt> buf)
	{
		fixed (JInt* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetIntArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
		}
	}

	public void GetLongArrayRegion(JLongArray array, JSize start, Span<JLong> buf)
	{
		fixed (JLong* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetLongArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
		}
	}

	public void GetFloatArrayRegion(JFloatArray array, JSize start, Span<JFloat> buf)
	{
		fixed (JFloat* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetFloatArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
		}
	}

	public void GetDoubleArrayRegion(JDoubleArray array, JSize start, Span<JDouble> buf)
	{
		fixed (JDouble* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetDoubleArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
		}
	}

	public void SetBooleanArrayRegion(JBooleanArray array, JSize start, ReadOnlySpan<bool> buf)
	{
		fixed (bool* bufPtr = buf)
		{
			_jniEnv->Vtbl->SetBooleanArrayRegion(_jniEnv, array, start, buf.Length, (JBoolean*)bufPtr);
		}
	}

	public void SetByteArrayRegion(JByteArray array, JSize start, ReadOnlySpan<JByte> buf)
	{
		fixed (JByte* bufPtr = buf)
		{
			_jniEnv->Vtbl->SetByteArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
		}
	}

	public void SetCharArrayRegion(JCharArray array, JSize start, ReadOnlySpan<char> buf)
	{
		fixed (char* bufPtr = buf)
		{
			_jniEnv->Vtbl->SetCharArrayRegion(_jniEnv, array, start, buf.Length, (JChar*)bufPtr);
		}
	}

	public void SetShortArrayRegion(JShortArray array, JSize start, ReadOnlySpan<JShort> buf)
	{
		fixed (JShort* bufPtr = buf)
		{
			_jniEnv->Vtbl->SetShortArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
		}
	}

	public void SetIntArrayRegion(JIntArray array, JSize start, ReadOnlySpan<JInt> buf)
	{
		fixed (JInt* bufPtr = buf)
		{
			_jniEnv->Vtbl->SetIntArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
		}
	}

	public void SetLongArrayRegion(JLongArray array, JSize start, ReadOnlySpan<JLong> buf)
	{
		fixed (JLong* bufPtr = buf)
		{
			_jniEnv->Vtbl->SetLongArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
		}
	}

	public void SetFloatArrayRegion(JFloatArray array, JSize start, ReadOnlySpan<JFloat> buf)
	{
		fixed (JFloat* bufPtr = buf)
		{
			_jniEnv->Vtbl->SetFloatArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
		}
	}

	public void SetDoubleArrayRegion(JDoubleArray array, JSize start, ReadOnlySpan<JDouble> buf)
	{
		fixed (JDouble* bufPtr = buf)
		{
			_jniEnv->Vtbl->SetDoubleArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
		}
	}

	public JInt RegisterNatives(JClass clazz, ReadOnlySpan<JNINativeMethod> methods)
	{
		fixed (JNINativeMethod* methodsPtr = methods)
		{
			return _jniEnv->Vtbl->RegisterNatives(_jniEnv, clazz, methodsPtr, methods.Length);
		}
	}

	public JInt UnregisterNatives(JClass clazz)
	{
		return _jniEnv->Vtbl->UnregisterNatives(_jniEnv, clazz);
	}

	public JInt MonitorEnter(JObject obj)
	{
		return _jniEnv->Vtbl->MonitorEnter(_jniEnv, obj);
	}

	public JInt MonitorExit(JObject obj)
	{
		return _jniEnv->Vtbl->MonitorExit(_jniEnv, obj);
	}

	public void GetStringRegion(JString str, JSize start, Span<char> buf)
	{
		fixed (char* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetStringRegion(_jniEnv, str, start, buf.Length, (JChar*)bufPtr);
		}
	}

	public void GetStringUTFRegion(JString str, JSize start, Span<byte> buf)
	{
		fixed (byte* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetStringUTFRegion(_jniEnv, str, start, buf.Length, bufPtr);
		}
	}

	public void* GetPrimitiveArrayCritical(JArray array)
	{
		return _jniEnv->Vtbl->GetPrimitiveArrayCritical(_jniEnv, array, null);
	}

	public void ReleasePrimitiveArrayCritical(JArray array, void* carray)
	{
		_jniEnv->Vtbl->ReleasePrimitiveArrayCritical(_jniEnv, array, carray, 0);
	}

	public char* GetStringCritical(JString string_)
	{
		return (char*)_jniEnv->Vtbl->GetPrimitiveArrayCritical(_jniEnv, string_, null);
	}

	public void ReleaseStringCritical(JString string_, char* carray)
	{
		_jniEnv->Vtbl->ReleaseStringCritical(_jniEnv, string_, (JChar*)carray);
	}

	public JWeak NewWeakGlobalRef(JObject obj)
	{
		return _jniEnv->Vtbl->NewWeakGlobalRef(_jniEnv, obj);
	}

	public void DeleteWeakGlobalRef(JWeak obj)
	{
		_jniEnv->Vtbl->DeleteWeakGlobalRef(_jniEnv, obj);
	}

	public bool ExceptionCheck()
	{
		return _jniEnv->Vtbl->ExceptionCheck(_jniEnv) != JNI.FALSE;
	}

	public JObject NewDirectByteBuffer(byte* address, JLong capacity)
	{
		return _jniEnv->Vtbl->NewDirectByteBuffer(_jniEnv, address, capacity);
	}

	public byte* GetDirectBufferAddress(JObject buf)
	{
		return (byte*)_jniEnv->Vtbl->GetDirectBufferAddress(_jniEnv, buf);
	}

	public JLong GetDirectBufferCapacity(JObject buf)
	{
		return _jniEnv->Vtbl->GetDirectBufferCapacity(_jniEnv, buf);
	}
}

//#endif
