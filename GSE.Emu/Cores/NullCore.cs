// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;

namespace GSE.Emu.Cores;

internal sealed class NullCore : IEmuCore
{
	public static readonly NullCore Singleton = new();

	private static readonly short[] _nullAudioBuffer = new short[48000 / 60 * 2];

	private NullCore()
	{
	}

	public void Dispose()
	{
	}

	public void Advance(EmuControllerState controllerState, out bool completedFrame, out uint samples, out uint cpuCycles)
	{
		completedFrame = false;
		cpuCycles = samples = 48000 / 60;
	}

	public bool LoadSave(ReadOnlySpan<byte> sav) => false;

	public ReadOnlySpan<byte> SaveState() => [];
	public bool LoadState(ReadOnlySpan<byte> state) => false;

	public void GetMemoryExport(ExportHelper.MemExport which, out nint ptr, out nuint len)
	{
		ptr = 0;
		len = 0;
	}

	public void SetColorCorrectionEnable(bool enable)
	{
	}

	// no need to actually provide these, these will never be used for this core
	public ReadOnlySpan<uint> VideoBuffer => [];
	public int VideoWidth => 0;
	public int VideoHeight => 0;

	// we still need to provide a dummy audio buffer however
	public ReadOnlySpan<short> AudioBuffer => _nullAudioBuffer;
	public int AudioFrequency => 48000;

	public uint CpuFrequency => 48000;
}
