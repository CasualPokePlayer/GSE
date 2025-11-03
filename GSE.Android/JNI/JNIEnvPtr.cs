// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Runtime.InteropServices;

using static SDL3.SDL;

namespace GSE.Android.JNI;

// ReSharper disable UnusedMember.Global

/// <summary>
/// Wraps JNIEnv*
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct JNIEnvPtr
{
	private readonly JNIEnv* _jniEnv;

	internal JNIEnvPtr(JNIEnv* jniEnv)
	{
		_jniEnv = jniEnv;
	}

	private JNIEnvPtr(nint jniEnv)
	{
		_jniEnv = (JNIEnv*)jniEnv;
	}

	public static JNIEnvPtr GetEnv()
	{
		var jniEnv = SDL_GetAndroidJNIEnv();
		if (jniEnv == 0)
		{
			throw new($"Failed to obtain JNI env, SDL error: {SDL_GetError()}");
		}

		return new(jniEnv);
	}

	public JInt GetVersion()
	{
		var version = _jniEnv->Vtbl->GetVersion(_jniEnv);
		if (version < 0)
		{
			JNIException.ThrowForErrorCode(version, nameof(GetVersion));
		}

		return version;
	}

	public JClass DefineClass(ReadOnlySpan<byte> name, JObject loader, ReadOnlySpan<JByte> buf)
	{
		fixed (byte* namePtr = name)
		fixed (JByte* bufPtr = buf)
		{
			var clazz = _jniEnv->Vtbl->DefineClass(_jniEnv, namePtr, loader, bufPtr, buf.Length);
			if (clazz.IsNull)
			{
				JNIException.ThrowForError(_jniEnv, nameof(DefineClass));
			}

			return clazz;
		}
	}

	public JClass FindClass(ReadOnlySpan<byte> name)
	{
		fixed (byte* namePtr = name)
		{
			var clazz = _jniEnv->Vtbl->FindClass(_jniEnv, namePtr);
			if (clazz.IsNull)
			{
				JNIException.ThrowForError(_jniEnv, nameof(FindClass));
			}

			return clazz;
		}
	}

	public JMethodID FromReflectedMethod(JObject method)
	{
		return _jniEnv->Vtbl->FromReflectedMethod(_jniEnv, method);
	}

	public JFieldID FromReflectedField(JObject field)
	{
		return _jniEnv->Vtbl->FromReflectedField(_jniEnv, field);
	}

	public JObject ToReflectedMethod(JClass cls, JMethodID methodID, JBoolean isStatic)
	{
		var method = _jniEnv->Vtbl->ToReflectedMethod(_jniEnv, cls, methodID, isStatic);
		if (method.IsNull)
		{
			JNIException.ThrowForError(_jniEnv, nameof(ToReflectedMethod));
		}

		return method;
	}

	public JClass GetSuperclass(JClass clazz)
	{
		// may return null, but that isn't an error
		return _jniEnv->Vtbl->GetSuperclass(_jniEnv, clazz);
	}

	public JBoolean IsAssignableFrom(JClass clazz1, JClass clazz2)
	{
		return _jniEnv->Vtbl->IsAssignableFrom(_jniEnv, clazz1, clazz2);
	}

	public JObject ToReflectedField(JClass cls, JFieldID fieldID, JBoolean isStatic)
	{
		var field = _jniEnv->Vtbl->ToReflectedField(_jniEnv, cls, fieldID, isStatic);
		if (field.IsNull)
		{
			JNIException.ThrowForError(_jniEnv, nameof(ToReflectedField));
		}

		return field;
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

	public void PushLocalFrame(JInt capacity)
	{
		var res = _jniEnv->Vtbl->PushLocalFrame(_jniEnv, capacity);
		if (res < 0)
		{
			JNIException.ThrowForError(_jniEnv, nameof(PushLocalFrame));
		}
	}

	public JObject PopLocalFrame(JObject result)
	{
		return _jniEnv->Vtbl->PopLocalFrame(_jniEnv, result);
	}

	public JObject NewGlobalRef(JObject obj)
	{
		var globalRef = _jniEnv->Vtbl->NewGlobalRef(_jniEnv, obj);
		if (globalRef.IsNull)
		{
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}

		return globalRef;
	}

	public void DeleteGlobalRef(JObject globalRef)
	{
		_jniEnv->Vtbl->DeleteGlobalRef(_jniEnv, globalRef);
	}

	public void DeleteLocalRef(JObject localRef)
	{
		_jniEnv->Vtbl->DeleteLocalRef(_jniEnv, localRef);
	}

	public JBoolean IsSameObject(JObject ref1, JObject ref2)
	{
		return _jniEnv->Vtbl->IsSameObject(_jniEnv, ref1, ref2);
	}

	public JObject NewLocalRef(JObject ref_)
	{
		return _jniEnv->Vtbl->NewLocalRef(_jniEnv, ref_);
	}

	public void EnsureLocalCapacity(JInt capacity)
	{
		var res = _jniEnv->Vtbl->EnsureLocalCapacity(_jniEnv, capacity);
		if (res < 0)
		{
			JNIException.ThrowForError(_jniEnv, nameof(EnsureLocalCapacity));
		}
	}

	public JObject AllocObject(JClass clazz)
	{
		var obj = _jniEnv->Vtbl->AllocObject(_jniEnv, clazz);
		if (obj.IsNull)
		{
			JNIException.ThrowForError(_jniEnv, nameof(AllocObject));
		}

		return obj;
	}

	public JObject NewObjectA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var obj = _jniEnv->Vtbl->NewObjectA(_jniEnv, clazz, methodID, argsPtr);
			if (obj.IsNull)
			{
				JNIException.ThrowForError(_jniEnv, nameof(NewObjectA));
			}

			return obj;
		}
	}

	public JClass GetObjectClass(JObject obj)
	{
		return _jniEnv->Vtbl->GetObjectClass(_jniEnv, obj);
	}

	public JBoolean IsInstanceOf(JObject obj, JClass clazz)
	{
		return _jniEnv->Vtbl->IsInstanceOf(_jniEnv, obj, clazz);
	}

	public JMethodID GetMethodID(JClass clazz, ReadOnlySpan<byte> name, ReadOnlySpan<byte> sig)
	{
		fixed (byte* namePtr = name, sigPtr = sig)
		{
			var methodID = _jniEnv->Vtbl->GetMethodID(_jniEnv, clazz, namePtr, sigPtr);
			if (methodID.IsNull)
			{
				JNIException.ThrowForError(_jniEnv, nameof(GetMethodID));
			}

			return methodID;
		}
	}

	public JObject CallObjectMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallObjectMethodA(_jniEnv, obj, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JBoolean CallBooleanMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallBooleanMethodA(_jniEnv, obj, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JByte CallByteMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallByteMethodA(_jniEnv, obj, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JChar CallCharMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallCharMethodA(_jniEnv, obj, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JShort CallShortMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallShortMethodA(_jniEnv, obj, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JInt CallIntMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallIntMethodA(_jniEnv, obj, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JLong CallLongMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallLongMethodA(_jniEnv, obj, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JFloat CallFloatMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallFloatMethodA(_jniEnv, obj, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JDouble CallDoubleMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallDoubleMethodA(_jniEnv, obj, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public void CallVoidMethodA(JObject obj, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			_jniEnv->Vtbl->CallVoidMethodA(_jniEnv, obj, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public JObject CallNonvirtualObjectMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallNonvirtualObjectMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JBoolean CallNonvirtualBooleanMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallNonvirtualBooleanMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JByte CallNonvirtualByteMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallNonvirtualByteMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JChar CallNonvirtualCharMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallNonvirtualCharMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JShort CallNonvirtualShortMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallNonvirtualShortMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JInt CallNonvirtualIntMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallNonvirtualIntMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JLong CallNonvirtualLongMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallNonvirtualLongMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JFloat CallNonvirtualFloatMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallNonvirtualFloatMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JDouble CallNonvirtualDoubleMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallNonvirtualDoubleMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public void CallNonvirtualVoidMethodA(JObject obj, JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			_jniEnv->Vtbl->CallNonvirtualVoidMethodA(_jniEnv, obj, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public JFieldID GetFieldID(JClass clazz, ReadOnlySpan<byte> name, ReadOnlySpan<byte> sig)
	{
		fixed (byte* namePtr = name, sigPtr = sig)
		{
			var fieldID = _jniEnv->Vtbl->GetFieldID(_jniEnv, clazz, namePtr, sigPtr);
			if (fieldID.IsNull)
			{
				JNIException.ThrowForError(_jniEnv, nameof(GetFieldID));
			}

			return fieldID;
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

	public JChar GetCharField(JObject obj, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetCharField(_jniEnv, obj, fieldID);
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

	public void SetCharField(JObject obj, JFieldID fieldID, JChar value)
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
			var methodID = _jniEnv->Vtbl->GetStaticMethodID(_jniEnv, clazz, namePtr, sigPtr);
			if (methodID.IsNull)
			{
				JNIException.ThrowForError(_jniEnv, nameof(GetStaticMethodID));
			}

			return methodID;
		}
	}

	public JObject CallStaticObjectMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallStaticObjectMethodA(_jniEnv, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JBoolean CallStaticBooleanMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallStaticBooleanMethodA(_jniEnv, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JByte CallStaticByteMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallStaticByteMethodA(_jniEnv, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JChar CallStaticCharMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallStaticCharMethodA(_jniEnv, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JShort CallStaticShortMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallStaticShortMethodA(_jniEnv, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JInt CallStaticIntMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallStaticIntMethodA(_jniEnv, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JLong CallStaticLongMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallStaticLongMethodA(_jniEnv, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JFloat CallStaticFloatMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallStaticFloatMethodA(_jniEnv, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public JDouble CallStaticDoubleMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			var ret = _jniEnv->Vtbl->CallStaticDoubleMethodA(_jniEnv, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
			return ret;
		}
	}

	public void CallStaticVoidMethodA(JClass clazz, JMethodID methodID, ReadOnlySpan<JValue> args)
	{
		fixed (JValue* argsPtr = args)
		{
			_jniEnv->Vtbl->CallStaticVoidMethodA(_jniEnv, clazz, methodID, argsPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public JFieldID GetStaticFieldID(JClass clazz, ReadOnlySpan<byte> name, ReadOnlySpan<byte> sig)
	{
		fixed (byte* namePtr = name, sigPtr = sig)
		{
			var fieldID = _jniEnv->Vtbl->GetStaticFieldID(_jniEnv, clazz, namePtr, sigPtr);
			if (fieldID.IsNull)
			{
				JNIException.ThrowForError(_jniEnv, nameof(GetStaticFieldID));
			}

			return fieldID;
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

	public JChar GetStaticCharField(JClass clazz, JFieldID fieldID)
	{
		return _jniEnv->Vtbl->GetStaticCharField(_jniEnv, clazz, fieldID);
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

	public void SetStaticCharField(JClass clazz, JFieldID fieldID, JChar value)
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

	public JString NewString(ReadOnlySpan<JChar> unicodeChars)
	{
		fixed (JChar* unicodeCharsPtr = unicodeChars)
		{
			var str = _jniEnv->Vtbl->NewString(_jniEnv, unicodeCharsPtr, unicodeChars.Length);
			if (str.IsNull)
			{
				JNIException.ThrowForError(_jniEnv, nameof(NewString));
			}

			return str;
		}
	}

	public JSize GetStringLength(JString str)
	{
		return _jniEnv->Vtbl->GetStringLength(_jniEnv, str);
	}

	public JChar* GetStringChars(JString str)
	{
		var chars = _jniEnv->Vtbl->GetStringChars(_jniEnv, str, null);
		if (chars == null)
		{
			JNIException.ThrowForError(_jniEnv, nameof(GetStringChars));
		}

		return chars;
	}

	public void ReleaseStringChars(JString str, JChar* chars)
	{
		_jniEnv->Vtbl->ReleaseStringChars(_jniEnv, str, chars);
	}

	public JString NewStringUTF(ReadOnlySpan<byte> bytes)
	{
		fixed (byte* bytesPtr = bytes)
		{
			var str = _jniEnv->Vtbl->NewStringUTF(_jniEnv, bytesPtr);
			if (str.IsNull)
			{
				JNIException.ThrowForError(_jniEnv, nameof(NewStringUTF));
			}

			return str;
		}
	}

	public JSize GetStringUTFLength(JString str)
	{
		return _jniEnv->Vtbl->GetStringUTFLength(_jniEnv, str);
	}

	public byte* GetStringUTFChars(JString str)
	{
		var chars = _jniEnv->Vtbl->GetStringUTFChars(_jniEnv, str, null);
		if (chars == null)
		{
			JNIException.ThrowForError(_jniEnv, nameof(GetStringUTFChars)); 
		}

		return chars;
	}

	public void ReleaseStringUTFChars(JString str, byte* chars)
	{
		_jniEnv->Vtbl->ReleaseStringUTFChars(_jniEnv, str, chars);
	}

	public JSize GetArrayLength(JArray array)
	{
		return _jniEnv->Vtbl->GetArrayLength(_jniEnv, array);
	}

	public JObjectArray NewObjectArray(JSize length, JClass elementClass, JObject initialElement)
	{
		var array = _jniEnv->Vtbl->NewObjectArray(_jniEnv, length, elementClass, initialElement);
		if (array.IsNull)
		{
			JNIException.ThrowForError(_jniEnv, nameof(NewObjectArray));
		}

		return array;
	}

	public JObject GetObjectArrayElement(JObjectArray array, JSize index)
	{
		var ret = _jniEnv->Vtbl->GetObjectArrayElement(_jniEnv, array, index);
		JNIException.ThrowIfExceptionPending(_jniEnv);
		return ret;
	}

	public void SetObjectArrayElement(JObjectArray array, JSize index, JObject value)
	{
		_jniEnv->Vtbl->SetObjectArrayElement(_jniEnv, array, index, value);
		JNIException.ThrowIfExceptionPending(_jniEnv);
	}

	public JBooleanArray NewBooleanArray(JSize length)
	{
		var array = _jniEnv->Vtbl->NewBooleanArray(_jniEnv, length);
		if (array.IsNull)
		{
			JNIException.ThrowForError(_jniEnv, nameof(NewBooleanArray));
		}

		return array;
	}

	public JByteArray NewByteArray(JSize length)
	{
		var array = _jniEnv->Vtbl->NewByteArray(_jniEnv, length);
		if (array.IsNull)
		{
			JNIException.ThrowForError(_jniEnv, nameof(NewByteArray));
		}

		return array;
	}

	public JCharArray NewCharArray(JSize length)
	{
		var array = _jniEnv->Vtbl->NewCharArray(_jniEnv, length);
		if (array.IsNull)
		{
			JNIException.ThrowForError(_jniEnv, nameof(NewCharArray));
		}

		return array;
	}

	public JShortArray NewShortArray(JSize length)
	{
		var array = _jniEnv->Vtbl->NewShortArray(_jniEnv, length);
		if (array.IsNull)
		{
			JNIException.ThrowForError(_jniEnv, nameof(NewShortArray));
		}

		return array;
	}

	public JIntArray NewIntArray(JSize length)
	{
		var array = _jniEnv->Vtbl->NewIntArray(_jniEnv, length);
		if (array.IsNull)
		{
			JNIException.ThrowForError(_jniEnv, nameof(NewIntArray));
		}

		return array;
	}

	public JLongArray NewLongArray(JSize length)
	{
		var array = _jniEnv->Vtbl->NewLongArray(_jniEnv, length);
		if (array.IsNull)
		{
			JNIException.ThrowForError(_jniEnv, nameof(NewLongArray));
		}

		return array;
	}

	public JFloatArray NewFloatArray(JSize length)
	{
		var array = _jniEnv->Vtbl->NewFloatArray(_jniEnv, length);
		if (array.IsNull)
		{
			JNIException.ThrowForError(_jniEnv, nameof(NewFloatArray));
		}

		return array;
	}

	public JDoubleArray NewDoubleArray(JSize length)
	{
		var array = _jniEnv->Vtbl->NewDoubleArray(_jniEnv, length);
		if (array.IsNull)
		{
			JNIException.ThrowForError(_jniEnv, nameof(NewDoubleArray));
		}

		return array;
	}

	public JBoolean* GetBooleanArrayElements(JBooleanArray array)
	{
		var elems = _jniEnv->Vtbl->GetBooleanArrayElements(_jniEnv, array, null);
		if (elems == null)
		{
			JNIException.ThrowForError(_jniEnv, nameof(GetBooleanArrayElements));
		}

		return elems;
	}

	public JByte* GetByteArrayElements(JByteArray array)
	{
		var elems = _jniEnv->Vtbl->GetByteArrayElements(_jniEnv, array, null);
		if (elems == null)
		{
			JNIException.ThrowForError(_jniEnv, nameof(GetByteArrayElements));
		}

		return elems;
	}

	public JChar* GetCharArrayElements(JCharArray array)
	{
		var elems = _jniEnv->Vtbl->GetCharArrayElements(_jniEnv, array, null);
		if (elems == null)
		{
			JNIException.ThrowForError(_jniEnv, nameof(GetCharArrayElements));
		}

		return elems;
	}

	public JShort* GetShortArrayElements(JShortArray array)
	{
		var elems = _jniEnv->Vtbl->GetShortArrayElements(_jniEnv, array, null);
		if (elems == null)
		{
			JNIException.ThrowForError(_jniEnv, nameof(GetShortArrayElements));
		}

		return elems;
	}

	public JInt* GetIntArrayElements(JIntArray array)
	{
		var elems = _jniEnv->Vtbl->GetIntArrayElements(_jniEnv, array, null);
		if (elems == null)
		{
			JNIException.ThrowForError(_jniEnv, nameof(GetIntArrayElements));
		}

		return elems;
	}

	public JLong* GetLongArrayElements(JLongArray array)
	{
		var elems = _jniEnv->Vtbl->GetLongArrayElements(_jniEnv, array, null);
		if (elems == null)
		{
			JNIException.ThrowForError(_jniEnv, nameof(GetLongArrayElements));
		}

		return elems;
	}

	public JFloat* GetFloatArrayElements(JFloatArray array)
	{
		var elems = _jniEnv->Vtbl->GetFloatArrayElements(_jniEnv, array, null);
		if (elems == null)
		{
			JNIException.ThrowForError(_jniEnv, nameof(GetFloatArrayElements));
		}

		return elems;
	}

	public JDouble* GetDoubleArrayElements(JDoubleArray array)
	{
		var elems = _jniEnv->Vtbl->GetDoubleArrayElements(_jniEnv, array, null);
		if (elems == null)
		{
			JNIException.ThrowForError(_jniEnv, nameof(GetDoubleArrayElements));
		}

		return elems;
	}

	public void ReleaseBooleanArrayElements(JBooleanArray array, JBoolean* elems)
	{
		_jniEnv->Vtbl->ReleaseBooleanArrayElements(_jniEnv, array, elems, 0);
	}

	public void ReleaseByteArrayElements(JByteArray array, JByte* elems)
	{
		_jniEnv->Vtbl->ReleaseByteArrayElements(_jniEnv, array, elems, 0);
	}

	public void ReleaseCharArrayElements(JCharArray array, JChar* elems)
	{
		_jniEnv->Vtbl->ReleaseCharArrayElements(_jniEnv, array, elems, 0);
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

	public void GetBooleanArrayRegion(JBooleanArray array, JSize start, Span<JBoolean> buf)
	{
		fixed (JBoolean* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetBooleanArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void GetByteArrayRegion(JByteArray array, JSize start, Span<JByte> buf)
	{
		fixed (JByte* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetByteArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void GetCharArrayRegion(JCharArray array, JSize start, Span<JChar> buf)
	{
		fixed (JChar* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetCharArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void GetShortArrayRegion(JShortArray array, JSize start, Span<JShort> buf)
	{
		fixed (JShort* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetShortArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void GetIntArrayRegion(JIntArray array, JSize start, Span<JInt> buf)
	{
		fixed (JInt* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetIntArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void GetLongArrayRegion(JLongArray array, JSize start, Span<JLong> buf)
	{
		fixed (JLong* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetLongArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void GetFloatArrayRegion(JFloatArray array, JSize start, Span<JFloat> buf)
	{
		fixed (JFloat* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetFloatArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void GetDoubleArrayRegion(JDoubleArray array, JSize start, Span<JDouble> buf)
	{
		fixed (JDouble* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetDoubleArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void SetBooleanArrayRegion(JBooleanArray array, JSize start, ReadOnlySpan<JBoolean> buf)
	{
		fixed (JBoolean* bufPtr = buf)
		{
			_jniEnv->Vtbl->SetBooleanArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void SetByteArrayRegion(JByteArray array, JSize start, ReadOnlySpan<JByte> buf)
	{
		fixed (JByte* bufPtr = buf)
		{
			_jniEnv->Vtbl->SetByteArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void SetCharArrayRegion(JCharArray array, JSize start, ReadOnlySpan<JChar> buf)
	{
		fixed (JChar* bufPtr = buf)
		{
			_jniEnv->Vtbl->SetCharArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void SetShortArrayRegion(JShortArray array, JSize start, ReadOnlySpan<JShort> buf)
	{
		fixed (JShort* bufPtr = buf)
		{
			_jniEnv->Vtbl->SetShortArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void SetIntArrayRegion(JIntArray array, JSize start, ReadOnlySpan<JInt> buf)
	{
		fixed (JInt* bufPtr = buf)
		{
			_jniEnv->Vtbl->SetIntArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void SetLongArrayRegion(JLongArray array, JSize start, ReadOnlySpan<JLong> buf)
	{
		fixed (JLong* bufPtr = buf)
		{
			_jniEnv->Vtbl->SetLongArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void SetFloatArrayRegion(JFloatArray array, JSize start, ReadOnlySpan<JFloat> buf)
	{
		fixed (JFloat* bufPtr = buf)
		{
			_jniEnv->Vtbl->SetFloatArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void SetDoubleArrayRegion(JDoubleArray array, JSize start, ReadOnlySpan<JDouble> buf)
	{
		fixed (JDouble* bufPtr = buf)
		{
			_jniEnv->Vtbl->SetDoubleArrayRegion(_jniEnv, array, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void RegisterNatives(JClass clazz, ReadOnlySpan<JNINativeMethod> methods)
	{
		fixed (JNINativeMethod* methodsPtr = methods)
		{
			var res = _jniEnv->Vtbl->RegisterNatives(_jniEnv, clazz, methodsPtr, methods.Length);
			if (res < 0)
			{
				JNIException.ThrowForError(_jniEnv, nameof(RegisterNatives));
			}
		}
	}

	public void UnregisterNatives(JClass clazz)
	{
		var res = _jniEnv->Vtbl->UnregisterNatives(_jniEnv, clazz);
		if (res < 0)
		{
			JNIException.ThrowForError(_jniEnv, nameof(UnregisterNatives));
		}
	}

	public void MonitorEnter(JObject obj)
	{
		var res = _jniEnv->Vtbl->MonitorEnter(_jniEnv, obj);
		if (res < 0)
		{
			JNIException.ThrowForError(_jniEnv, nameof(MonitorEnter));
		}
	}

	public void MonitorExit(JObject obj)
	{
		var res = _jniEnv->Vtbl->MonitorExit(_jniEnv, obj);
		if (res < 0)
		{
			JNIException.ThrowForError(_jniEnv, nameof(MonitorExit));
		}
	}

	public void GetStringRegion(JString str, JSize start, Span<JChar> buf)
	{
		fixed (JChar* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetStringRegion(_jniEnv, str, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void GetStringUTFRegion(JString str, JSize start, Span<byte> buf)
	{
		fixed (byte* bufPtr = buf)
		{
			_jniEnv->Vtbl->GetStringUTFRegion(_jniEnv, str, start, buf.Length, bufPtr);
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}
	}

	public void* GetPrimitiveArrayCritical(JArray array)
	{
		var carray = _jniEnv->Vtbl->GetPrimitiveArrayCritical(_jniEnv, array, null);
		if (carray == null)
		{
			JNIException.ThrowForError(_jniEnv, nameof(GetPrimitiveArrayCritical));
		}

		return carray;
	}

	public void ReleasePrimitiveArrayCritical(JArray array, void* carray)
	{
		_jniEnv->Vtbl->ReleasePrimitiveArrayCritical(_jniEnv, array, carray, 0);
	}

	public JChar* GetStringCritical(JString str)
	{
		var carray = _jniEnv->Vtbl->GetStringCritical(_jniEnv, str, null);
		if (carray == null)
		{
			JNIException.ThrowForError(_jniEnv, nameof(GetStringCritical));
		}

		return carray;
	}

	public void ReleaseStringCritical(JString str, JChar* carray)
	{
		_jniEnv->Vtbl->ReleaseStringCritical(_jniEnv, str, carray);
	}

	public JWeak NewWeakGlobalRef(JObject obj)
	{
		var ret = _jniEnv->Vtbl->NewWeakGlobalRef(_jniEnv, obj);
		if (ret.IsNull)
		{
			// null is not necessarily an error
			JNIException.ThrowIfExceptionPending(_jniEnv);
		}

		return ret;
	}

	public void DeleteWeakGlobalRef(JWeak obj)
	{
		_jniEnv->Vtbl->DeleteWeakGlobalRef(_jniEnv, obj);
	}

	public JBoolean ExceptionCheck()
	{
		return _jniEnv->Vtbl->ExceptionCheck(_jniEnv);
	}

	public JObject NewDirectByteBuffer(byte* address, JLong capacity)
	{
		var buf = _jniEnv->Vtbl->NewDirectByteBuffer(_jniEnv, address, capacity);
		if (buf.IsNull)
		{
			JNIException.ThrowForError(_jniEnv, nameof(NewDirectByteBuffer));
		}

		return buf;
	}

	public byte* GetDirectBufferAddress(JObject buf)
	{
		var address = _jniEnv->Vtbl->GetDirectBufferAddress(_jniEnv, buf);
		if (address == null)
		{
			JNIException.ThrowForError(_jniEnv, nameof(GetDirectBufferAddress));
		}

		return (byte*)address;
	}

	public JLong GetDirectBufferCapacity(JObject buf)
	{
		var capacity = _jniEnv->Vtbl->GetDirectBufferCapacity(_jniEnv, buf);
		if (capacity == -1)
		{
			JNIException.ThrowForError(_jniEnv, nameof(GetDirectBufferCapacity));
		}

		return capacity;
	}
}
