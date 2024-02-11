using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GSR.Emu.Cores;

internal static partial class Gambatte
{
	/// <returns>opaque state pointer</returns>
	[LibraryImport("libgambatte")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial IntPtr gambatte_create();

	/// <param name="core">opaque state pointer</param>
	[LibraryImport("libgambatte")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial void gambatte_destroy(IntPtr core);

	[Flags]
	public enum LoadFlags : uint
	{
		/// <summary>Treat the ROM as having CGB support regardless of what its header advertises</summary>
		CGB_MODE = 1,
		/// <summary>Use GBA intial CPU register values when in CGB mode.</summary>
		GBA_FLAG = 2,
		/// <summary>Previously a multicart heuristic enable. Reserved for future use.</summary>
		RESERVED_FLAG = 4,
		/// <summary>Treat the ROM as having SGB support regardless of what its header advertises.</summary>
		SGB_MODE = 8,
		/// <summary>Prevent implicit saveSavedata calls for the ROM.</summary>
		READONLY_SAV = 16,
		/// <summary>Use heuristics to boot without a BIOS.</summary>
		NO_BIOS = 32
	}

	/// <summary>
	/// Load ROM image.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="romData">the rom data, can be disposed of once this function returns</param>
	/// <param name="length">length of romData in bytes</param>
	/// <param name="flags">ORed combination of LoadFlags.</param>
	/// <returns>0 on success, negative value on failure.</returns>
	[LibraryImport("libgambatte")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial int gambatte_loadbuf(IntPtr core, ReadOnlySpan<byte> romData, uint length, LoadFlags flags);

	/// <summary>
	/// Load GB(C) BIOS image.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="biosData">the bios data, can be disposed of once this function returns</param>
	/// <param name="length">length of biosData in bytes</param>
	/// <returns>0 on success, negative value on failure.</returns>
	[LibraryImport("libgambatte")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial int gambatte_loadbiosbuf(IntPtr core, ReadOnlySpan<byte> biosData, uint length);

	/// <summary>
	/// Emulates until at least 'samples' stereo sound samples are produced in the supplied buffer,
	/// or until a video frame has been drawn.
	///
	/// There are 35112 stereo sound samples in a video frame.
	/// May run for up to 2064 stereo samples too long.
	/// A stereo sample consists of two native endian 2s complement 16-bit PCM samples,
	/// with the left sample preceding the right one.
	///
	/// Returns early when a new video frame has finished drawing in the video buffer,
	/// such that the caller may update the video output before the frame is overwritten.
	/// The return value indicates whether a new video frame has been drawn, and the
	/// exact time (in number of samples) at which it was drawn.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="videoBuf">160x144 ARGB32 (native endian) video frame buffer or 0</param>
	/// <param name="pitch">distance in number of pixels (not bytes) from the start of one line to the next in videoBuf</param>
	/// <param name="soundBuf">buffer with space >= samples + 2064</param>
	/// <param name="samples">in: number of stereo samples to produce, out: actual number of samples produced</param>
	/// <returns>sample number at which the video frame was produced. -1 means no frame was produced.</returns>
	[LibraryImport("libgambatte")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static unsafe partial int gambatte_runfor(IntPtr core, uint* videoBuf, int pitch, [Out] uint[] soundBuf, ref uint samples);

	[LibraryImport("libgambatte")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static unsafe partial int gambatte_updatescreenborder(IntPtr core, uint* videobuf, int pitch);

	/// <summary>
	/// Reset to initial state.
	/// Equivalent to reloading a ROM image, or turning a Game Boy Color off and on again.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="samplesToStall">samples of reset stall</param>
	[LibraryImport("libgambatte")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial void gambatte_reset(IntPtr core, uint samplesToStall);

	/// <summary>
	/// set cgb palette lookup
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="lut">uint32[32768], input color (r,g,b) is at lut[r | g &lt;&lt; 5 | b &lt;&lt; 10]</param>
	[LibraryImport("libgambatte")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial void gambatte_setcgbpalette(IntPtr core, ReadOnlySpan<uint> lut);

	/// <summary>
	/// combination of button flags used by the input callback
	/// </summary>
	[Flags]
	public enum Buttons : uint
	{
		A = 0x01,
		B = 0x02,
		SELECT = 0x04,
		START = 0x08,
		RIGHT = 0x10,
		LEFT = 0x20,
		UP = 0x40,
		DOWN = 0x80
	}

	/// <summary>
	/// Sets the callback used for getting input state.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="getInput">input getter</param>
	/// <param name="p">input getter userdata</param>
	[LibraryImport("libgambatte")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static unsafe partial void gambatte_setinputgetter(IntPtr core, delegate* unmanaged[Cdecl]<IntPtr, Buttons> getInput, IntPtr p);

	/// <summary>
	/// Get persistant cart memory.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="dest">byte buffer to write into. gambatte_getsavedatalength() bytes will be written</param>
	[LibraryImport("libgambatte")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial void gambatte_savesavedata(IntPtr core, [Out] byte[] dest);

	/// <summary>
	/// restore persistant cart memory.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="data">byte buffer to read from. gambatte_getsavedatalength() bytes will be read</param>
	[LibraryImport("libgambatte")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial void gambatte_loadsavedata(IntPtr core, [In] byte[] data);

	/// <summary>
	/// get the size of the persistant cart memory block. this value DEPENDS ON THE PARTICULAR CART LOADED
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <returns>length in bytes. 0 means no internal persistant cart memory</returns>
	[LibraryImport("libgambatte")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial int gambatte_getsavedatalength(IntPtr core);

	/// <summary>
	/// Saves emulator state to the buffer given by 'stateBuf'.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="videoBuf">160x144 RGB32 (native endian) video frame buffer or 0. Used for saving a thumbnail.</param>
	/// <param name="pitch">pitch distance in number of pixels (not bytes) from the start of one line to the next in videoBuf.</param>
	/// <param name="stateBuf">buffer for savestate</param>
	/// <returns>size</returns>
	[LibraryImport("libgambatte")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial int gambatte_savestate(IntPtr core, [In] uint[] videoBuf, int pitch, [Out] byte[] stateBuf);

	/// <summary>
	/// Loads emulator state from the buffer given by 'stateBuf' of size 'size'.
	/// </summary>
	/// <param name="core">opaque state pointer</param>
	/// <param name="stateBuf">buffer for savestate</param>
	/// <param name="size">size of savestate buffer</param>
	/// <returns>success</returns>
	[LibraryImport("libgambatte")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool gambatte_loadstate(IntPtr core, ReadOnlySpan<byte> stateBuf, int size);
}
