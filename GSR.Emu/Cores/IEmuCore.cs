// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;

namespace GSR.Emu.Cores;

internal interface IEmuCore : IDisposable
{
	void Advance(EmuControllerState controllerState, out bool completedFrame, out uint samples, out uint cpuCycles);

	bool LoadSave(ReadOnlySpan<byte> sav);

	ReadOnlySpan<byte> SaveState();
	bool LoadState(ReadOnlySpan<byte> state);

	void GetMemoryExport(ExportHelper.MemExport which, out nint ptr, out nuint len);
	void SetColorCorrectionEnable(bool enable);

	ReadOnlySpan<uint> VideoBuffer { get; }
	int VideoWidth { get; }
	int VideoHeight { get; }

	ReadOnlySpan<short> AudioBuffer { get; }
	int AudioFrequency { get; }

	uint CpuFrequency { get; }
}
