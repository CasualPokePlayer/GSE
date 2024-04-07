// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;

namespace GSR.Android.JNI;

/// <summary>
/// JNINativeInterface as described by https://docs.oracle.com/javase/8/docs/technotes/guides/jni/spec/functions.html
/// Note that various functions have V and A postfix variants
/// For these functions, the base function and the V variant cannot be used, only the A variant can be used
/// (base function uses variadic args, V variant uses va_list, neither are marshallable in C#)
/// Also note that we use SDL's JNI env, which will be using JNI v1.4 (therefore we don't support functions past that)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct JNINativeInterface
{
	public void* _reserved0;
	public void* _reserved1;
	public void* _reserved2;
	public void* _reserved3;

	public delegate* unmanaged<JNIEnv*, JInt> GetVersion;

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
