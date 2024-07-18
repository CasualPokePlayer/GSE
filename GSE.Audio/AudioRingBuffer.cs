// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;

namespace GSE.Audio;

// simple thread-safe ring buffer for audio
internal sealed class AudioRingBuffer
{
	private readonly object _audioBufferLock = new();

	private short[] _buffer = [];
	private int _readIndex;
	private int _writeIndex;

	public void Reset(int size, int initialFilledSize)
	{
		lock (_audioBufferLock)
		{
			_buffer = new short[size];
			_readIndex = 0;
			_writeIndex = Math.Min(initialFilledSize, size - 1);
		}
	}

	public int BufferUsed()
	{
		lock (_audioBufferLock)
		{
			return _readIndex <= _writeIndex
				? _writeIndex - _readIndex
				: _buffer.Length - _readIndex + _writeIndex;
		}
	}

	public int Read(Span<short> output)
	{
		lock (_audioBufferLock)
		{
			var input = _buffer.AsSpan();
			var toCopy = Math.Min(output.Length, BufferUsed());
			var remaining = input.Length - _readIndex;

			// if enough samples are in the first segment, just do a single copy
			if (remaining > toCopy)
			{
				input.Slice(_readIndex, toCopy).CopyTo(output);
				_readIndex += toCopy;
				return toCopy;
			}

			// first copy will be the entire first segment, all remaining items will be copied
			input[_readIndex..].CopyTo(output);
			toCopy -= remaining;

			// read index wraps back around, toCopy items should all be able to be copied now
			input[..toCopy].CopyTo(output[remaining..]);
			_readIndex = toCopy;
			return toCopy + remaining;
		}
	}

	public int BufferAvail()
	{
		lock (_audioBufferLock)
		{
			return _writeIndex < _readIndex
				? _readIndex - _writeIndex
				: _buffer.Length - _writeIndex + _readIndex;
		}
	}

	public void Write(ReadOnlySpan<short> input)
	{
		lock (_audioBufferLock)
		{
			var output = _buffer.AsSpan();
			var toCopy = Math.Min(input.Length, BufferAvail());
			var remaining = output.Length - _writeIndex;

			// if the first segment has enough space, just do a single copy
			if (remaining > toCopy)
			{
				input[..toCopy].CopyTo(output[_writeIndex..]);
				_writeIndex += toCopy;
				return;
			}

			// first copy will cover the entire first segment, all remaining items will be copied
			input[..remaining].CopyTo(output[_writeIndex..]);
			toCopy -= remaining;

			// write index wraps back around, toCopy items should all be able to be copied now
			input.Slice(remaining, toCopy).CopyTo(output);
			_writeIndex = toCopy;
		}
	}
}
