// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using ZstdCompressionStream = ZstdSharp.CompressionStream;

namespace GSE.Emu;

/// <summary>
/// Implements an emu input log, one which could be played back with external tooling
/// This input log is intended to record GB/C/A inputs, along with various emu actions (e.g. save/state loads)
/// It uses the .gm2 extension, as it is an effective successor to Gambatte-Speedrun's .gm format
/// </summary>
internal sealed class EmuInputLog : IDisposable
{
	public enum EmuPlatform : uint
	{
		GB,
		GBC,
		GBC_GBA,
		SGB2,
		GBA,
	}

	[Flags]
	public enum MovieFlags : uint
	{
		/// <summary>
		/// Movie starts from a savestate, rather than power-on + save file
		/// </summary>
		StartsFromSaveState = 1 << 0,

		/// <summary>
		/// Data after the header is zstd compressed
		/// All movies made in GSE are zstd compressed
		/// </summary>
		IsZstdCompressed = 1 << 1,

		/// <summary>
		/// GBA RTC should be force disabled
		/// </summary>
		GbaRtcDisabled = 1 << 2,
	}

	/// <summary>
	/// Strings within the input log header comprise of 1 byte for length and a 255 byte buffer to hold UTF8 chars
	/// Strings which exceed these limitations must be truncated
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	private struct HeaderString
	{
		public byte Length;
		public unsafe fixed byte Buffer[255];

		public unsafe void SetString(string str)
		{
			var utf8Str = Encoding.UTF8.GetBytes(str);
			Length = (byte)(utf8Str.Length > 255 ? 255 : utf8Str.Length);
			fixed (byte* buffer = Buffer)
			{
				utf8Str.AsSpan()[..Length].CopyTo(new(buffer, Length));
			}
		}
	}

	private const int GM2_VERSION = 1;
	private const ulong GM2_MAGIC = 0x4753454D4F564945;

	[StructLayout(LayoutKind.Sequential)]
	private struct EmuInputLogHeader
	{
		/// <summary>
		/// Input log signature, to mark this is a .gm2
		/// Should always be 0x4753454D4F564945 in big endian
		/// (i.e. GSEMOVIE)
		/// </summary>
		public ulong InputLogMagic;

		/// <summary>
		/// Input log version, increased on any movie format change.
		/// </summary>
		public uint InputLogVersion;

		/// <summary>
		/// The emu platform. This defines the platform which should be chosen for the emu core.
		/// For GB/C games, GB, GBC, GBC in GBA, and SGB2 are valid modes.
		/// For GBA games, only GBA is a valid mode.
		/// </summary>
		public EmuPlatform Platform;

		/// <summary>
		/// Reset stall for GB/C games. Should be ignored for GBA games
		/// </summary>
		public uint ResetStall;

		/// <summary>
		/// Movie flags. Generally used for movie quirks and movie sync settings
		/// </summary>
		public MovieFlags Flags;

		/// <summary>
		/// Unix timestamp when this movie was started
		/// </summary>
		public long StartTimestamp;

		/// <summary>
		/// GB/C RTC dividers (2^21/sec) for movie sync, appropriate for gambatte_settime
		/// Should be ignored for movies which start from a savestate 
		/// </summary>
		public ulong GbRtcDividers;

		/// <summary>
		/// The starting savestate or save file size
		/// If the movie starts from a savestate, the savestate will proceed after the movie header
		/// If the movie starts from power-on + save file, the save file will proceed after the movie header
		/// Note this size is for the uncompressed data, not the potentially compressed data
		/// </summary>
		public uint StateOrSaveSize;

		/// <summary>
		/// The ROM file name, without the extension
		/// </summary>
		public HeaderString RomName;

		/// <summary>
		/// GSE version string
		/// </summary>
		public HeaderString EmuVersion;

		/// <summary>
		/// Normalizes native endianness to little endian
		/// (Except for magic, which is enforced to be big endian)
		/// </summary>
		public void NormalizeEndianness()
		{
			if (!BitConverter.IsLittleEndian)
			{
				InputLogVersion = BinaryPrimitives.ReverseEndianness(InputLogVersion);
				Platform = (EmuPlatform)BinaryPrimitives.ReverseEndianness((uint)Platform);
				ResetStall = BinaryPrimitives.ReverseEndianness(ResetStall);
				Flags = (MovieFlags)BinaryPrimitives.ReverseEndianness((uint)Flags);
				StartTimestamp = BinaryPrimitives.ReverseEndianness(StartTimestamp);
				GbRtcDividers = BinaryPrimitives.ReverseEndianness(GbRtcDividers);
				StateOrSaveSize = BinaryPrimitives.ReverseEndianness(StateOrSaveSize);
			}
			else
			{
				InputLogMagic = BinaryPrimitives.ReverseEndianness(InputLogMagic);
			}
		}
	}

	private readonly FileStream _gm2File;
	// all data after the header is compressed with zstd
	private readonly ZstdCompressionStream _inputStream;
	private readonly BinaryWriter _inputWriter;

	private readonly record struct MovieInput(uint CpuCyclesRan, EmuButtons Inputs);

	private readonly ConcurrentQueue<MovieInput> _inputQueue;
	private readonly AutoResetEvent _inputReadyEvent;
	private readonly Thread _movieThread;
	private volatile bool _disposing;

	public EmuInputLog(
		string basePath,
		string romName,
		string emuVersion,
		GBPlatform gbPlatform,
		bool isGba,
		bool disableGbaRtc,
		ulong gbRtcDividers,
		bool startsFromSaveState,
		ReadOnlySpan<byte> stateOrSaveFile)
	{
		try
		{
			var now = DateTime.UtcNow;
			var filename = $"{now.ToLocalTime().ToString("s", CultureInfo.InvariantCulture).Replace(':', '-')}-{romName}";
			// maximum length of a filename is 255 chars
			// (this includes 4 chars for .gm2)
			if (filename.Length > 255 - 4)
			{
				filename = filename[..(255 - 4)];
			}

			var path = Path.Combine(basePath, filename) + ".gm2";
			_gm2File = File.Create(path);

			var header = default(EmuInputLogHeader);
			header.InputLogMagic = GM2_MAGIC;
			header.InputLogVersion = GM2_VERSION;

			if (isGba)
			{
				header.Platform = EmuPlatform.GBA;
				header.ResetStall = 0;

				if (disableGbaRtc)
				{
					header.Flags |= MovieFlags.GbaRtcDisabled;
				}
			}
			else
			{
				header.Platform = gbPlatform switch
				{
					GBPlatform.GB => EmuPlatform.GB,
					GBPlatform.GBC => EmuPlatform.GBC,
					GBPlatform.GBA or GBPlatform.GBP => EmuPlatform.GBC_GBA,
					GBPlatform.SGB2 => EmuPlatform.SGB2,
					_ => throw new InvalidOperationException()
				};

				header.ResetStall = gbPlatform switch
				{
					GBPlatform.GB or GBPlatform.GBC or GBPlatform.GBA => 0u,
					GBPlatform.GBP => 101 * (2 << 14),
					GBPlatform.SGB2 => 128 * (2 << 14),
					_ => throw new InvalidOperationException()
				};
			}

			if (startsFromSaveState)
			{
				header.Flags |= MovieFlags.StartsFromSaveState;
			}

			header.Flags |= MovieFlags.IsZstdCompressed;

			header.StartTimestamp = (long)(now - DateTime.UnixEpoch).TotalSeconds;
			header.GbRtcDividers = gbRtcDividers;
			header.StateOrSaveSize = (uint)stateOrSaveFile.Length;

			header.RomName.SetString(romName);
			header.EmuVersion.SetString(emuVersion);

			header.NormalizeEndianness();
			_gm2File.Write(MemoryMarshal.AsBytes<EmuInputLogHeader>(new(ref header)));

			_inputStream = new(_gm2File);
			// the state or save file is after the header, compressed
			_inputStream.Write(stateOrSaveFile);

			_inputWriter = new(_inputStream);

			_inputQueue = new();
			_inputReadyEvent = new(false);
			_movieThread = new Thread(MovieThreadProc) { IsBackground = true, Name = "Movie Thread" };
			_movieThread.Start();
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	private void MovieThreadProc()
	{
		while (!_disposing)
		{
			while (_inputQueue.TryDequeue(out var movieInput))
			{
				_inputWriter.Write(movieInput.CpuCyclesRan);
				_inputWriter.Write((uint)movieInput.Inputs);
			}

			_inputReadyEvent.WaitOne();
		}

		// make sure any final inputs are written to the movie file
		while (_inputQueue.TryDequeue(out var movieInput))
		{
			_inputWriter.Write(movieInput.CpuCyclesRan);
			_inputWriter.Write((uint)movieInput.Inputs);
		}
	}

	public void SubmitInput(uint cpuCyclesRan, EmuButtons emuButtons)
	{
		_inputQueue.Enqueue(new(cpuCyclesRan, emuButtons));
		_inputReadyEvent.Set();
	}

	public void SubmitHardReset()
	{
		_inputQueue.Enqueue(new(0, EmuButtons.HardReset));
		_inputReadyEvent.Set();
	}

	public void Dispose()
	{
		_disposing = true;
		_inputReadyEvent?.Set();
		_movieThread?.Join();
		_inputReadyEvent?.Dispose();

		_inputWriter?.Dispose();
		_inputStream?.Dispose();
		_gm2File?.Dispose();
	}
}
