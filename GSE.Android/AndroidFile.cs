// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.Win32.SafeHandles;

using GSE.Android.JNI;

namespace GSE.Android;

/// <summary>
/// Holds methods for opening Android files
/// </summary>
public static class AndroidFile
{
	private static JClass _gseActivityClassId;
	private static JMethodID _requestDocumentMethodId;
	private static JMethodID _openContentMethodId;
	private static JMethodID _openFileManagerMethodId;

	internal static void InitializeJNI(JNIEnvPtr env, JClass gseActivityClassId)
	{
		_gseActivityClassId = gseActivityClassId;
		_requestDocumentMethodId = env.GetStaticMethodID(_gseActivityClassId, "RequestDocument"u8, "()V"u8);
		_openContentMethodId = env.GetStaticMethodID(_gseActivityClassId, "OpenContent"u8, "(Ljava/lang/String;)I"u8);
		_openFileManagerMethodId = env.GetStaticMethodID(_gseActivityClassId, "OpenFileManager"u8, "()V"u8);
	}

	private static readonly AutoResetEvent _documentRequestDone = new(false);
	private static string _requestedDocument;

	// ReSharper disable once UnusedMember.Global
	public static string RequestDocument()
	{
		var env = JNIEnvPtr.GetEnv();
		env.CallStaticVoidMethodA(_gseActivityClassId, _requestDocumentMethodId, []);
		_documentRequestDone.WaitOne();
		return _requestedDocument;
	}

	[UnmanagedCallersOnly]
	internal static void SetDocumentRequestResult(JNIEnvPtr env, JClass cls, JString uriAndPath)
	{
		if (!uriAndPath.IsNull)
		{
			var len = env.GetStringLength(uriAndPath);
			var strBuf = len > 1024 ? new char[len] : stackalloc char[len];
			env.GetStringRegion(uriAndPath, 0, MemoryMarshal.Cast<char, JChar>(strBuf));
			_requestedDocument = new(strBuf);
		}
		else
		{
			_requestedDocument = null;
		}

		_documentRequestDone.Set();
	}

	// ReSharper disable once UnusedMember.Global
	public static MemoryStream OpenBufferedStream(string contentUri)
	{
		var env = JNIEnvPtr.GetEnv();

		using var contentUriJStr = new LocalRefWrapper<JString>(env,
			env.NewString(MemoryMarshal.Cast<char, JChar>(contentUri.AsSpan())));
		Span<JValue> args = stackalloc JValue[1];
		args[0].l = contentUriJStr.LocalRef;
		var fd = env.CallStaticIntMethodA(_gseActivityClassId, _openContentMethodId, args);
		if (fd == -1)
		{
			throw new("Could not open content");
		}

		var sfh = new SafeFileHandle(fd, true);
		using var fs = new FileStream(sfh, FileAccess.Read);
		var ms = new MemoryStream();
		fs.CopyTo(ms);
		ms.Seek(0, SeekOrigin.Begin);
		return ms;
	}

	// ReSharper disable once UnusedMember.Global
	public static void OpenFileManager()
	{
		var env = JNIEnvPtr.GetEnv();
		env.CallStaticVoidMethodA(_gseActivityClassId, _openFileManagerMethodId, []);
	}
}
