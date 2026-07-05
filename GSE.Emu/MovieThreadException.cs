// Copyright (c) 2026 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;

namespace GSE.Emu;

/// <summary>
/// Wraps an exception thrown on the movie thread
/// Stack trace will refer to the movie thread's stack, not the gui thread's stack!
/// </summary>
internal sealed class MovieThreadException(Exception innerException) : Exception
{
	public override string StackTrace { get; } = innerException.StackTrace;
	public override string Message { get; } = $"Movie thread threw an exception -> {innerException.Message}";
}
