// Copyright (c) 2025 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#if GSE_WINDOWS
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

using Windows.Win32;
using Windows.Win32.Foundation;

namespace GSE;

/// <summary>
/// Basic SynchronizationContext which blocks for waits with standard Win32 PInvoke
/// This is used on the main thread, which is a STA thread
/// .NET will try to keep messages pumping when waiting on a STA thread (using CoWaitForMultipleHandles)
/// However, this causes issues if anything, so this sync context is used to force normal blocking waits
/// </summary>
internal sealed class Win32BlockingWaitSyncContext : SynchronizationContext
{
	public static readonly Win32BlockingWaitSyncContext Singleton = new();

	private Win32BlockingWaitSyncContext()
	{
		SetWaitNotificationRequired();
	}

	public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
	{
		var waitResult = PInvoke.WaitForMultipleObjects(
			MemoryMarshal.Cast<IntPtr, HANDLE>(waitHandles), waitAll, (uint)millisecondsTimeout);
		if (waitResult == WAIT_EVENT.WAIT_FAILED)
		{
			throw new Win32Exception();
		}

		return (int)waitResult;
	}
}
#endif
