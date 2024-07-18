// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

namespace GSE.Input.Keyboards;

internal readonly record struct KeyEvent(ScanCode Key, bool IsPressed);
