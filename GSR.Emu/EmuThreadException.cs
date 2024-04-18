// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;

namespace GSR.Emu;

/// <summary>
/// Wraps an exception thrown on the emu thread
/// Stack trace will refer to the emu thread's stack, not the gui thread's stack!
/// </summary>
internal sealed class EmuThreadException(Exception innerException) : Exception
{
	public override string StackTrace { get; } = innerException.StackTrace;
	public override string Message { get; } = $"Emu thread threw an exception -> {innerException.Message}";
}
