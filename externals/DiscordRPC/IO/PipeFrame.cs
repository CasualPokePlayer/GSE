using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Text.Json;

using DiscordRPC.Helper;

namespace DiscordRPC.IO;

/// <summary>
/// A frame received and sent to the Discord client for RPC communications.
/// </summary>
public struct PipeFrame : IEquatable<PipeFrame>
{
	/// <summary>
	/// The maximum size of a pipe frame (16KiB).
	/// </summary>
	public const int MAX_SIZE = 16 * 1024;

	/// <summary>
	/// The opcode of the frame
	/// </summary>
	public Opcode Opcode { get; set; }

	/// <summary>
	/// The length of the frame data
	/// </summary>
	public readonly uint Length => (uint)Data.Length;

	/// <summary>
	/// The data in the frame
	/// </summary>
	public byte[] Data { get; set; }
		
	/// <summary>
	/// The data represented as a string.
	/// </summary>
	public readonly string Message => GetMessage();

	/// <summary>
	/// Creates a new pipe frame instance
	/// </summary>
	/// <param name="opcode">The opcode of the frame</param>
	/// <param name="data">The data of the frame that will be serialized as JSON</param>
	internal PipeFrame(Opcode opcode, object data)
	{
		// Set the opcode and a temp field for data
		Opcode = opcode;
		Data = null;

		// Set the data
		SetObject(data);
	}

	/// <summary>
	/// Gets the encoding used for the pipe frames
	/// </summary>
	public static Encoding MessageEncoding => Encoding.UTF8;

	/// <summary>
	/// Sets the data based of a string
	/// </summary>
	/// <param name="str"></param>
	private void SetMessage(string str)
	{
		Data = MessageEncoding.GetBytes(str);
	}

	/// <summary>
	/// Gets a string based of the data
	/// </summary>
	/// <returns></returns>
	private readonly string GetMessage()
	{
		return MessageEncoding.GetString(Data);
	}

	/// <summary>
	/// Serializes the object into json string then encodes it into <see cref="Data"/>.
	/// </summary>
	/// <param name="obj"></param>
	private void SetObject<T>(T obj) where T : class
	{
		var json = JsonSerializer.Serialize(obj, typeof(T), JsonSerializationContext.Default);
		SetMessage(json);
	}

	/// <summary>
	/// Sets the opcodes and serializes the object into a json string.
	/// </summary>
	/// <param name="opcode"></param>
	/// <param name="obj"></param>
	public void SetObject(Opcode opcode, object obj)
	{
		Opcode = opcode;
		SetObject(obj);
	}

	/// <summary>
	/// Deserializes the data into the supplied type using JSON.
	/// </summary>
	/// <typeparam name="T">The type to deserialize into</typeparam>
	/// <returns></returns>
	public readonly T GetObject<T>() where T : class
	{
		var json = GetMessage();
		return (T)JsonSerializer.Deserialize(json, typeof(T), JsonSerializationContext.Default);
	}

	/// <summary>
	/// Attempts to read the contents of the frame from the stream
	/// </summary>
	/// <param name="stream"></param>
	/// <returns></returns>
	public bool ReadStream(Stream stream)
	{
		// Try to read the opcode
		if (!TryReadUInt32(stream, out var op))
			return false;

		// Try to read the length
		if (!TryReadUInt32(stream, out var len))
			return false;

		var readsRemaining = len;

		// Read the contents
		using var mem = new MemoryStream((int)len);
		var chunkSize = Math.Min(2048U, len); // read in chunks of 2KiB
		var buffer = ArrayPool<byte>.Shared.Rent((int)chunkSize);
		try
		{
			int bytesRead;
			while ((bytesRead = stream.Read(buffer, 0, (int)Math.Min(chunkSize, readsRemaining))) > 0)
			{
				readsRemaining -= chunkSize;
				mem.Write(buffer, 0, bytesRead);
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}

		var result = mem.ToArray();
		if (result.LongLength != len)
			return false;

		Opcode = (Opcode)op;
		Data = result;
		return true;
	}

	/// <summary>
	/// Attempts to read a UInt32
	/// </summary>
	/// <param name="stream"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	private static bool TryReadUInt32(Stream stream, out uint value)
	{
		// Read the bytes available to us
		Span<byte> bytes = stackalloc byte[4];
		var cnt = stream.Read(bytes);

		// Make sure we actually have a valid value
		if (cnt != 4)
		{
			value = 0;
			return false;
		}

		value = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
		return true;
	}

	/// <summary>
	/// Writes the frame into the target frame as one big byte block.
	/// </summary>
	/// <param name="stream"></param>
	public readonly void WriteStream(Stream stream)
	{
		var fullBlockSize = sizeof(uint) + sizeof(uint) + Length;
		var fullBlock = ArrayPool<byte>.Shared.Rent((int)fullBlockSize);
		try
		{
			// Get all the bytes
			BinaryPrimitives.WriteUInt32LittleEndian(fullBlock, (uint)Opcode);
			BinaryPrimitives.WriteUInt32LittleEndian(fullBlock.AsSpan(4), Length);
			Data.CopyTo(fullBlock.AsSpan(8));

			// Write it to the stream
			stream.Write(fullBlock, 0, (int)fullBlockSize);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(fullBlock);
		}
	}

	/// <summary>
	/// Compares if the frame equals the other frame.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public readonly bool Equals(PipeFrame other)
	{
		return Opcode == other.Opcode &&
		       Length == other.Length &&
		       Data == other.Data;
	}

	public readonly override bool Equals(object obj)
	{
		return obj is PipeFrame frame && Equals(frame);
	}

	public readonly override int GetHashCode()
	{
		return HashCode.Combine((int)Opcode, Length, Data);
	}

	public static bool operator ==(PipeFrame left, PipeFrame right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(PipeFrame left, PipeFrame right)
	{
		return !(left == right);
	}
}
