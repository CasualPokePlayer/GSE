// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#if GSR_ANDROID

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.Win32.SafeHandles;

namespace GSR.Android;

/// <summary>
/// Holds methods for opening Android files
/// </summary>
internal static unsafe class AndroidFile
{
	private static JNIEnvPtr _env;

	private static JClass _gsrActivityClassId;

	private static JMethodID _requestDocumentMethodId;
	private static JMethodID _openContentMethodId;

	public static void InitializeJNI()
	{
		_env = JNIEnvPtr.GetEnv();

		_gsrActivityClassId = _env.FindClass("org/psr/gsr/GSRActivity"u8);
		if (_gsrActivityClassId == 0)
		{
			throw new("Failed to find GSR class ID");
		}

		_requestDocumentMethodId = _env.GetStaticMethodID(_gsrActivityClassId, "RequestDocument"u8, "()V"u8);
		if (_requestDocumentMethodId == 0)
		{
			throw new("Failed to find RequestDocument method ID");
		}

		_openContentMethodId = _env.GetStaticMethodID(_gsrActivityClassId, "OpenContent"u8, "(Ljava/lang/String;)I"u8);
		if (_openContentMethodId == 0)
		{
			throw new("Failed to find OpenContent method ID");
		}
	}

	private static readonly AutoResetEvent _documentRequestDone = new(false);
	private static string _requestedDocument;

	public static string RequestDocument()
	{
		_env.CallStaticVoidMethodA(_gsrActivityClassId, _requestDocumentMethodId, []);
		_documentRequestDone.WaitOne();
		return _requestedDocument;
	}

	[UnmanagedCallersOnly(EntryPoint = "Java_org_psr_gsr_GSRActivity_SetDocumentRequestResult")]
	public static void SetDocumentRequestResult(JNIEnvPtr env, JClass cls, JString uriAndPath)
	{
		if (uriAndPath != 0)
		{
			var len = env.GetStringLength(uriAndPath);
			var strBuf = len > 1024 ? new char[len] : stackalloc char[len];
			env.GetStringRegion(uriAndPath, 0, strBuf);
			_requestedDocument = new(strBuf);
		}
		else
		{
			_requestedDocument = null;
		}

		_documentRequestDone.Set();
	}

	public static MemoryStream OpenBufferedStream(string contentUri)
	{
		var contentUriJStr = _env.NewString(contentUri.AsSpan());
		if (contentUriJStr == 0)
		{
			throw new("Failed to create JString for content uri");
		}

		Span<JValue> args = stackalloc JValue[1];
		args[0].l = contentUriJStr;
		var fd = _env.CallStaticIntMethodA(_gsrActivityClassId, _openContentMethodId, args);
		_env.DeleteLocalRef(contentUriJStr);
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
}

#endif
