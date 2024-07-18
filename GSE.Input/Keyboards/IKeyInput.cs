// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;

namespace GSE.Input.Keyboards;

internal interface IKeyInput : IDisposable
{
	IEnumerable<KeyEvent> GetEvents();
	string ConvertScanCodeToString(ScanCode key);
}
