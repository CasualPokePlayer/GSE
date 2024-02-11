using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GSR.Emu.Cores;

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
	/// <returns>opaque state pointer</returns>
	[LibraryImport("libmgba")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial IntPtr mgba_create(ReadOnlySpan<byte> romData, int romLength, ReadOnlySpan<byte> biosData, int biosLength, [MarshalAs(UnmanagedType.U1)] bool forceDisableRtc);

	/// <param name="core">opaque state pointer</param>
	[LibraryImport("libmgba")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial void mgba_destroy(IntPtr core);

	/// <summary>
	/// set color palette lookup
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="colorLut">uint32[32768], input color (r,g,b) is at lut[r | g &lt;&lt; 5 | b &lt;&lt; 10]</param>
	[LibraryImport("libmgba")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial void mgba_setcolorlut(IntPtr core, ReadOnlySpan<uint> colorLut);

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
	/// <param name="soundBuf">buffer with at least 1024 stereo samples (2048 16-bit integers)</param>
	/// <param name="samples">number of stereo samples produced (double this to get 16-bit integer count)</param>
	/// <param name="cpuCycles">number of cpu cycles advanced</param>
	[LibraryImport("libmgba")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial void mgba_advance(IntPtr core, Buttons buttons, [Out] uint[] videoBuf, [Out] short[] soundBuf, out uint samples, out uint cpuCycles);

	/// <summary>
	/// Reset to initial state.
	/// Equivalent to reloading a ROM image, or turning a Game Boy Advance off and on again.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	[LibraryImport("libmgba")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial void mgba_reset(IntPtr core);

	/// <summary>
	/// Get persistant cart memory.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="dest">byte buffer to write into. mgba_getsavedatalength() bytes will be written</param>
	[LibraryImport("libmgba")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial void mgba_savesavedata(IntPtr core, [Out] byte[] dest);

	/// <summary>
	/// restore persistant cart memory.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="data">byte buffer to read from. mgba_getsavedatalength() bytes will be read</param>
	[LibraryImport("libmgba")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial void mgba_loadsavedata(IntPtr core, [In] byte[] data);

	/// <summary>
	/// get the size of the persistant cart memory block. this value DEPENDS ON THE PARTICULAR CART LOADED
	/// NOTE: mgba will dynamically detect save type, so this may shrink in the first few frames. maximum size will be 0x20000 + 16 bytes
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <returns>length in bytes. 0 means no internal persistant cart memory</returns>
	[LibraryImport("libmgba")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial int mgba_getsavedatalength(IntPtr core);

	/// <summary>
	/// Calculates the savestate length. Must be called every time before making a savestate!
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <returns>save state size in bytes</returns>
	[LibraryImport("libmgba")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial int mgba_getsavestatelength(IntPtr core);

	/// <summary>
	/// Saves emulator state to the buffer given by 'stateBuf'.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="stateBuf">buffer for savestate</param>
	/// <returns>success</returns>
	[LibraryImport("libmgba")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool mgba_savestate(IntPtr core, [Out] byte[] stateBuf);

	/// <summary>
	/// Loads emulator state from the buffer given by 'stateBuf' of size 'size'.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="stateBuf">buffer for savestate</param>
	/// <param name="size">size of savestate buffer</param>
	/// <returns>success</returns>
	[LibraryImport("libmgba")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool mgba_loadstate(IntPtr core, ReadOnlySpan<byte> stateBuf, int size);
}
