// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;

namespace GSR.Input;

/// <summary>
/// Wraps an exception thrown on the input thread
/// Stack trace will refer to the input thread's stack, not the gui thread's stack!
/// </summary>
internal sealed class InputThreadException(Exception innerException) : Exception
{
	public override string StackTrace { get; } = innerException.StackTrace;
	public override string Message { get; } = $"Input thread threw an exception -> {innerException.Message}";
}
