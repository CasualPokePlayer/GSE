// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static SDL3.SDL;

namespace GSE.Audio;

// NOTE: Previously I tried a threading approach which offloaded resampling to a separate thread...
// That approach was very complicated and audio did not work at all for reasons I can't comprehend (edit: blipbuf managed impl was bugged, fixed now, maybe can reintroduce?)
// Thus, resampling is done on the emu thread, and we let SDL handle obtaining samples in its audio callback (called on a separate thread anyways)
public sealed class AudioManager : IDisposable
{
	public const string DEFAULT_AUDIO_DEVICE = "[Default Audio Device]";
	public const int MINIMUM_LATENCY_MS = 0;
	public const int MAXIMUM_LATENCY_MS = 128;

	static AudioManager()
	{
		// aim for as low latency as possible here
		SDL_SetHint(SDL_HINT_AUDIO_DEVICE_RAW_STREAM, "1");
		SDL_SetHint(SDL_HINT_AUDIO_DEVICE_SAMPLE_FRAMES, "32");
	}

	private readonly AudioRingBuffer OutputAudioBuffer = new();

	[UnmanagedCallersOnly(CallConvs = [ typeof(CallConvCdecl) ])]
	private static unsafe void SDLAudioCallback(nint userdata, nint stream, int additionalAmount, int totalAmount)
	{
		var manager = (AudioManager)GCHandle.FromIntPtr(userdata).Target!;
		var samples = additionalAmount / 2;
		var sampleBuffer = samples > 1024
			? new short[samples]
			: stackalloc short[samples];
		var samplesRead = manager.OutputAudioBuffer.Read(sampleBuffer);
		if (samplesRead < samples)
		{
			Debug.WriteLine($"AUDIO UNDERRUN! Only read {samplesRead} samples (wanted {samples} samples)");
			sampleBuffer[samplesRead..].Clear();
		}

		fixed (short* sampleBufferPtr = sampleBuffer)
		{
			SDL_PutAudioStreamData(stream, (nint)sampleBufferPtr, sampleBuffer.Length);
		}
	}

	private readonly object _resamplerLock = new();

	private int _inputAudioFrequency;
	private int _outputAudioFrequency;
	private int _outputAudioSampleBatchSize; // in stereo samples

	private BlipBuffer _resampler;
	private int _lastL, _lastR;
	private short[] _resamplingBuffer = [];

	public string AudioDeviceName { get; private set; }
	private int _latencyMs;
	private int _volume;

	private uint _sdlAudioDeviceId;
	private nint _sdlAudioDeviceStream;
	private GCHandle _sdlUserData;

	/// <summary>
	/// Helper ref struct for getting the audio device list in an RAII style
	/// </summary>
	private readonly ref struct SDLAudioDeviceList
	{
		private readonly nint _audioDevices;
		private readonly int _numDevices;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe ReadOnlySpan<uint> AsSpan()
		{
			return new((void*)_audioDevices, _numDevices);
		}

		public SDLAudioDeviceList()
		{
			_audioDevices = SDL_GetAudioPlaybackDevices(out _numDevices);
			if (_audioDevices == 0)
			{
				throw new($"Failed to get audio devices, SDL error: {SDL_GetError()}");
			}
		}

		public void Dispose()
		{
			SDL_free(_audioDevices);
		}
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public static string[] EnumerateAudioDevices()
	{
		using var devices = new SDLAudioDeviceList();
		var deviceIds = devices.AsSpan();
		var ret = new string[deviceIds.Length + 1];
		ret[0] = DEFAULT_AUDIO_DEVICE;
		for (var i = 0; i < deviceIds.Length; i++)
		{
			ret[i + 1] = SDL_GetAudioDeviceName(deviceIds[i]);
		}

		return ret;
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	private unsafe void OpenAudioDevice(string deviceName)
	{
		using var devices = new SDLAudioDeviceList();
		var deviceIds = devices.AsSpan();

		var deviceAudioSpec = default(SDL_AudioSpec);
		var deviceId = 0u;

		if (deviceName != DEFAULT_AUDIO_DEVICE)
		{
			foreach (var id in deviceIds)
			{
				if (SDL_GetAudioDeviceName(id) == deviceName)
				{
					// if this fails, that means the audio device ended up disconnected right after getting its name
					if (SDL_GetAudioDeviceFormat(id, out deviceAudioSpec, out _))
					{
						deviceId = id;
					}

					break;
				}
			}
		}

		if (deviceId == 0)
		{
			if (!SDL_GetAudioDeviceFormat(SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, out deviceAudioSpec, out _))
			{
				throw new($"Failed to obtain the default audio device format, SDL error: {SDL_GetError()}");
			}

			deviceId = SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK;
			deviceName = DEFAULT_AUDIO_DEVICE;
		}

		var wantedAudioSpec = default(SDL_AudioSpec);
		wantedAudioSpec.freq = deviceAudioSpec.freq; // try to use the device sample rate, so we can avoid a secondary resampling by SDL or whatever native api is used
		wantedAudioSpec.format = BitConverter.IsLittleEndian ? SDL_AudioFormat.SDL_AUDIO_S16LE : SDL_AudioFormat.SDL_AUDIO_S16BE;
		wantedAudioSpec.channels = 2;

		var audioDeviceStream = SDL_OpenAudioDeviceStream(
			devid: deviceId,
			spec: ref wantedAudioSpec,
			callback: &SDLAudioCallback,
			userdata: GCHandle.ToIntPtr(_sdlUserData)
		);

		if (audioDeviceStream == 0)
		{
			// this should rarely happen, but regardless it's possible the audio device disconnected right after getting its format
			if (deviceName != null)
			{
				if (!SDL_GetAudioDeviceFormat(SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, out deviceAudioSpec, out _))
				{
					throw new($"Failed to obtain the default audio device format, SDL error: {SDL_GetError()}");
				}

				deviceId = SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK;
				deviceName = DEFAULT_AUDIO_DEVICE;

				audioDeviceStream = SDL_OpenAudioDeviceStream(
					devid: deviceId,
					spec: ref wantedAudioSpec,
					callback: &SDLAudioCallback,
					userdata: GCHandle.ToIntPtr(_sdlUserData)
				);
			}

			if (audioDeviceStream == 0)
			{
				AudioDeviceName = DEFAULT_AUDIO_DEVICE;
				throw new($"Failed to open audio device, SDL error: {SDL_GetError()}");
			}
		}

		// the device sample frames isn't properly known until after the device is opened
		if (!SDL_GetAudioDeviceFormat(_sdlAudioDeviceId, out _, out var deviceSampleBatchSize))
		{
			// audio device probably disconnected at this point
			// we'll handle that later, just give a dummy deviceSampleBatchSize for now
			deviceSampleBatchSize = 512;
		}

		_sdlAudioDeviceId = SDL_GetAudioStreamDevice(audioDeviceStream);
		_sdlAudioDeviceStream = audioDeviceStream;
		AudioDeviceName = deviceName;

		lock (_resamplerLock)
		{
			_outputAudioSampleBatchSize = deviceSampleBatchSize;
			_outputAudioFrequency = wantedAudioSpec.freq;
			_resampler?.Dispose();
			_resampler = null;
			_resampler = new(BitOperations.RoundUpToPowerOf2((uint)(_outputAudioFrequency * 20 / 1000)));
			Reset();
		}

		SDL_ResumeAudioDevice(_sdlAudioDeviceId);
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public void Reset()
	{
		lock (_resamplerLock)
		{
			_lastL = _lastR = 0;
			_resampler.SetRates(_inputAudioFrequency, _outputAudioFrequency);
			_resampler.Clear();
			OutputAudioBuffer.Reset(MAXIMUM_LATENCY_MS * 2 * _outputAudioFrequency * 2 / 1000, _latencyMs * _outputAudioFrequency * 2 / 1000);
		}
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public void SetInputAudioFrequency(int freq)
	{
		if (_inputAudioFrequency != freq)
		{
			_inputAudioFrequency = freq;
			Reset();
		}
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public void SetAudioDevice(string audioDeviceName)
	{
		if (AudioDeviceName != audioDeviceName)
		{
			if (_sdlAudioDeviceStream != 0)
			{
				SDL_DestroyAudioStream(_sdlAudioDeviceStream);
				_sdlAudioDeviceStream = 0;
				_sdlAudioDeviceId = 0;
			}

			OpenAudioDevice(audioDeviceName);
		}
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public void SetLatency(int latencyMs)
	{
		if (_latencyMs != latencyMs)
		{
			lock (_resamplerLock)
			{
				_latencyMs = latencyMs;
				Reset();
			}
		}
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public void SetVolume(int volume)
	{
		if (_volume != volume)
		{
			lock (_resamplerLock)
			{
				_volume = volume;
			}
		}
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public void RecoverLostAudioDeviceIfNeeded(uint deviceIdLost)
	{
		// if the current device was lost, it's no longer valid, and must be reset with the default device (which never stops)
		if (deviceIdLost == _sdlAudioDeviceId)
		{
			AudioDeviceName = null;
			SetAudioDevice(DEFAULT_AUDIO_DEVICE);
		}
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public void ResetAudioDeviceIfNeeded(uint deviceIdFormatChanged)
	{
		// if the current device format changed, we may need to reset some things
		if (deviceIdFormatChanged == _sdlAudioDeviceId)
		{
			if (!SDL_GetAudioDeviceFormat(_sdlAudioDeviceId, out var deviceSpec, out var deviceSampleBatchSize))
			{
				AudioDeviceName = null;
				SetAudioDevice(DEFAULT_AUDIO_DEVICE);
				return;
			}

			if (_outputAudioFrequency != deviceSpec.freq || _outputAudioSampleBatchSize != _outputAudioFrequency)
			{
				lock (_resamplerLock)
				{
					_outputAudioSampleBatchSize = deviceSampleBatchSize;
					_outputAudioFrequency = deviceSpec.freq;
					_resampler?.Dispose();
					_resampler = null;
					_resampler = new(BitOperations.RoundUpToPowerOf2((uint)(_outputAudioFrequency * 20 / 1000)));
					Reset();
				}
			}
		}
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public void Pause()
	{
		SDL_PauseAudioDevice(_sdlAudioDeviceId);
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public void Unpause()
	{
		Reset();
		SDL_ResumeAudioDevice(_sdlAudioDeviceId);
	}

	/// <summary>
	/// NOTE: CALLED ON EMU THREAD
	/// </summary>
	public void DispatchAudio(ReadOnlySpan<short> samples, bool isFastForwarding)
	{
		lock (_resamplerLock)
		{
			if (!isFastForwarding)
			{
				var bufferUsed = OutputAudioBuffer.BufferUsed();
				// if we have > latency + 2.5 sample batches of the buffer used, we're likely out of sync, and thus need an audio buffer reset
				// we have quite some tolerance here, latency should normally be balanced out by eventual audio callbacks
				// 2.5 sample batches is chosen to avoid hopefully overzealous buffer resets
				var toleratedBufferUsage = _latencyMs * _outputAudioFrequency * 2 / 1000 + _outputAudioSampleBatchSize * 5;
				if (bufferUsed > toleratedBufferUsage)
				{
					Reset();
				}
			}

			uint resamplerTime = 0;
			for (var i = 0; i < samples.Length; i += 2)
			{
				int l = samples[i + 0];
				int r = samples[i + 1];
				_resampler.AddDelta(resamplerTime++, l - _lastL, r - _lastR);
				_lastL = l;
				_lastR = r;
			}

			_resampler.EndFrame(resamplerTime);
			var samplesAvail = _resampler.SamplesAvail * 2;
			if (samplesAvail > _resamplingBuffer.Length)
			{
				_resamplingBuffer = new short[samplesAvail];
			}

			var samplesRead = _resampler.ReadSamples(_resamplingBuffer, _volume);
			OutputAudioBuffer.Write(_resamplingBuffer.AsSpan()[..((int)samplesRead * 2)]);
		}
	}

	public AudioManager(string audioDeviceName, int latencyMs, int volume)
	{
		if (!SDL_Init(SDL_InitFlags.SDL_INIT_AUDIO))
		{
			throw new($"Could not init SDL audio! SDL error: {SDL_GetError()}");
		}

		_inputAudioFrequency = 48000;

		try
		{
			_sdlUserData = GCHandle.Alloc(this, GCHandleType.Weak);
			_latencyMs = latencyMs;
			_volume = volume;
			SetAudioDevice(audioDeviceName);
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	public void Dispose()
	{
		_resampler?.Dispose();

		if (_sdlAudioDeviceStream != 0)
		{
			SDL_DestroyAudioStream(_sdlAudioDeviceStream);
		}

		if (_sdlUserData.IsAllocated)
		{
			_sdlUserData.Free();
		}

		SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO);
	}
}
