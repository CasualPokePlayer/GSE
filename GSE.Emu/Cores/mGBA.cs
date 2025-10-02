// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GSE.Emu.Cores;

internal static partial class MGBA
{
	/// <summary>
	/// Create opaque state
	/// </summary>
	/// <param name="romData">the rom data, can be disposed of once this function returns</param>
	/// <param name="romLength">length of romData in bytes</param>
	/// <param name="biosData">the bios data, can be disposed of once this function returns</param>
	/// <param name="biosLength">length of biosData in bytes</param>
	/// <param name="forceDisableRtc">force disable rtc, if present</param>
	/// <param name="rtcStartTime">rtc start time to set, if present</param>
	/// <returns>opaque state pointer</returns>
	[LibraryImport("mgba")]
	[UnmanagedCallConv(CallConvs = [ typeof(CallConvCdecl) ])]
	public static partial nint mgba_create(ReadOnlySpan<byte> romData, int romLength,
		ReadOnlySpan<byte> biosData, int biosLength, [MarshalAs(UnmanagedType.U1)] bool forceDisableRtc, long rtcStartTime);

	/// <param name="core">opaque state pointer</param>
	[LibraryImport("mgba")]
	[UnmanagedCallConv(CallConvs = [ typeof(CallConvCdecl) ])]
	public static partial void mgba_destroy(nint core);

	/// <summary>
	/// set color palette lookup
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="colorLut">uint32[32768], input color (r,g,b) is at lut[r | g &lt;&lt; 5 | b &lt;&lt; 10]</param>
	[LibraryImport("mgba")]
	[UnmanagedCallConv(CallConvs = [ typeof(CallConvCdecl) ])]
	public static partial void mgba_setcolorlut(nint core, ReadOnlySpan<uint> colorLut);

	/// <summary>
	/// combination of button flags used in mgba_advance
	/// </summary>
	[Flags]
	public enum Buttons : ushort
	{
		A = 0x001,
		B = 0x002,
		SELECT = 0x004,
		START = 0x008,
		RIGHT = 0x010,
		LEFT = 0x020,
		UP = 0x040,
		DOWN = 0x080,
		R = 0x0100,
		L = 0x0200,
	}

	/// <summary>
	/// Emulates one frame
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="buttons">input for this frame</param>
	/// <param name="videoBuf">240x160 ARGB32 (native endian) video frame buffer</param>
	/// <param name="soundBuf">buffer with at least 8192 stereo samples (16384 16-bit integers)</param>
	/// <param name="samples">number of stereo samples produced (double this to get 16-bit integer count)</param>
	/// <param name="cpuCycles">number of cpu cycles advanced</param>
	[LibraryImport("mgba")]
	[UnmanagedCallConv(CallConvs = [ typeof(CallConvCdecl) ])]
	public static partial void mgba_advance(nint core, Buttons buttons, Span<uint> videoBuf, Span<short> soundBuf, out uint samples, out uint cpuCycles);

	/// <summary>
	/// Reset to initial state.
	/// Equivalent to reloading a ROM image, or turning a Game Boy Advance off and on again.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	[LibraryImport("mgba")]
	[UnmanagedCallConv(CallConvs = [ typeof(CallConvCdecl) ])]
	public static partial void mgba_reset(nint core);

	/// <summary>
	/// Get persistant cart memory.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="dest">byte buffer to write into.</param>
	/// <returns>length in bytes. 0 means no internal persistant cart memory (or not yet detected)</returns>
	[LibraryImport("mgba")]
	[UnmanagedCallConv(CallConvs = [ typeof(CallConvCdecl) ])]
	public static partial int mgba_savesavedata(nint core, Span<byte> dest);

	/// <summary>
	/// Restore persistant cart memory.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="data">byte buffer to read from.</param>
	/// <param name="size">size of data</param>
	/// <param name="rtcStartTime">rtc start time to set</param>
	[LibraryImport("mgba")]
	[UnmanagedCallConv(CallConvs = [ typeof(CallConvCdecl) ])]
	public static partial void mgba_loadsavedata(nint core, ReadOnlySpan<byte> data, int size, long rtcStartTime);

	/// <summary>
	/// Gets the current RTC time.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <returns>current rtc time as unix timestamp</returns>
	[LibraryImport("mgba")]
	[UnmanagedCallConv(CallConvs = [ typeof(CallConvCdecl) ])]
	public static partial long mgba_getrtctime(nint core);

	/// <summary>
	/// Calculates the savestate length. Must be called every time before making a savestate!
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <returns>save state size in bytes</returns>
	[LibraryImport("mgba")]
	[UnmanagedCallConv(CallConvs = [ typeof(CallConvCdecl) ])]
	public static partial int mgba_getsavestatelength(nint core);

	/// <summary>
	/// Saves emulator state to the buffer given by 'stateBuf'.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="stateBuf">buffer for savestate</param>
	/// <returns>success</returns>
	[LibraryImport("mgba")]
	[UnmanagedCallConv(CallConvs = [ typeof(CallConvCdecl) ])]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool mgba_savestate(nint core, Span<byte> stateBuf);

	/// <summary>
	/// Loads emulator state from the buffer given by 'stateBuf' of size 'size'.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="stateBuf">buffer for savestate</param>
	/// <param name="size">size of savestate buffer</param>
	/// <param name="rtcTime">rtc time to set (if not found in state)</param>
	/// <returns>success</returns>
	[LibraryImport("mgba")]
	[UnmanagedCallConv(CallConvs = [ typeof(CallConvCdecl) ])]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool mgba_loadstate(nint core, ReadOnlySpan<byte> stateBuf, int size, long rtcTime);

	/// <summary>
	/// memory blocks that mgba_getmemoryblock() can return
	/// </summary>
	public enum MemoryBlocks : int
	{
		IWRAM = 0,
		EWRAM = 1,
		SRAM = 2,
		END,
	}

	/// <summary>
	/// get memory block
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="which">which memory block</param>
	/// <param name="ptr">memory block pointer, or NULL if not present</param>
	/// <param name="len">memory block length, or 0 if not present</param>
	[LibraryImport("mgba")]
	[UnmanagedCallConv(CallConvs = [ typeof(CallConvCdecl) ])]
	public static partial void mgba_getmemoryblock(nint core, MemoryBlocks which, out nint ptr, out nuint len);
}
