// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static SDL2.SDL;

namespace GSE.Audio;

// NOTE: Previously I tried a threading approach which offloaded resampling to a separate thread...
// That approach was very complicated and audio did not work at all for reasons I can't comprehend (edit: blipbuf managed impl was bugged, fixed now, maybe can reintroduce?)
// Thus, resampling is done on the emu thread, and we let SDL handle obtaining samples in its audio callback (called on a separate thread anyways)
public sealed class AudioManager : IDisposable
{
	public const string DEFAULT_AUDIO_DEVICE = "[Default Audio Device]";
	public const int MINIMUM_LATENCY_MS = 0;
	public const int MAXIMUM_LATENCY_MS = 128;

	private readonly AudioRingBuffer OutputAudioBuffer = new();

	[UnmanagedCallersOnly(CallConvs = [ typeof(CallConvCdecl) ])]
	private static unsafe void SDLAudioCallback(nint userdata, nint stream, int len)
	{
		var manager = (AudioManager)GCHandle.FromIntPtr(userdata).Target!;
		var samples = len / 2;
		var samplesRead = manager.OutputAudioBuffer.Read(new((void*)stream, samples));
		if (samplesRead < samples)
		{
			Debug.WriteLine($"AUDIO UNDERRUN! Only read {samplesRead} samples (wanted {samples} samples)");
			new Span<short>((void*)(stream + samplesRead * 2), samples - samplesRead).Clear();
		}
	}

	private readonly object _resamplerLock = new();

	private int _inputAudioFrequency;
	private int _outputAudioFrequency;

	private BlipBuffer _resampler;
	private int _lastL, _lastR;
	private short[] _resamplingBuffer = [];

	public string AudioDeviceName { get; private set; }
	private int _latencyMs;
	private int _volume;

	private uint _sdlAudioDeviceId;
	private GCHandle _sdlUserData;

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public static string[] EnumerateAudioDevices()
	{
		var deviceCount = SDL_GetNumAudioDevices(iscapture: 0);
		var ret = new string[deviceCount + 1];
		ret[0] = DEFAULT_AUDIO_DEVICE;
		for (var i = 0; i < deviceCount; i++)
		{
			ret[i + 1] = SDL_GetAudioDeviceName(i, iscapture: 0);
		}

		return ret;
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	private static int GetDeviceSampleRate(string deviceName)
	{
		const int FALLBACK_FREQ = 48000;
		if (deviceName == null)
		{
			return SDL_GetDefaultAudioInfo(out _, out var spec, iscapture: 0) == 0 ? spec.freq : FALLBACK_FREQ;
		}

		var deviceCount = SDL_GetNumAudioDevices(iscapture: 0);
		for (var i = 0; i < deviceCount; i++)
		{
			if (SDL_GetAudioDeviceName(i, iscapture: 0) == deviceName)
			{
				return SDL_GetAudioDeviceSpec(i, iscapture: 0, out var spec) == 0 ? spec.freq : FALLBACK_FREQ;
			}
		}

		return FALLBACK_FREQ;
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	private void OpenAudioDevice(string deviceName)
	{
		if (deviceName == DEFAULT_AUDIO_DEVICE)
		{
			deviceName = null;
		}

		var wantedSdlAudioSpec = default(SDL_AudioSpec);
		wantedSdlAudioSpec.freq = GetDeviceSampleRate(deviceName); // try to use the device sample rate, so we can avoid a secondary resampling by SDL or whatever native api is used
		wantedSdlAudioSpec.format = AUDIO_S16SYS;
		wantedSdlAudioSpec.channels = 2;
		wantedSdlAudioSpec.samples = 512; // we'll let this change to however SDL best wants it
		wantedSdlAudioSpec.userdata = GCHandle.ToIntPtr(_sdlUserData);

		unsafe
		{
			wantedSdlAudioSpec.callback = &SDLAudioCallback;
		}

		var deviceId = SDL_OpenAudioDevice(
			device: deviceName,
			iscapture: 0,
			desired: ref wantedSdlAudioSpec,
			obtained: out var obtainedAudioSpec,
			allowed_changes: (int)(SDL_AUDIO_ALLOW_SAMPLES_CHANGE | SDL_AUDIO_ALLOW_FREQUENCY_CHANGE)
		);

		if (deviceId == 0)
		{
			if (deviceName != null)
			{
				wantedSdlAudioSpec.freq = GetDeviceSampleRate(null);
				deviceId = SDL_OpenAudioDevice(
					device: null,
					iscapture: 0,
					desired: ref wantedSdlAudioSpec,
					obtained: out obtainedAudioSpec,
					allowed_changes: (int)(SDL_AUDIO_ALLOW_SAMPLES_CHANGE | SDL_AUDIO_ALLOW_FREQUENCY_CHANGE)
				);

				deviceName = null;
			}

			if (deviceId == 0)
			{
				AudioDeviceName = DEFAULT_AUDIO_DEVICE;
				throw new($"Failed to open audio device, SDL error: {SDL_GetError()}");
			}
		}

		_sdlAudioDeviceId = deviceId;
		AudioDeviceName = deviceName ?? DEFAULT_AUDIO_DEVICE;
		_outputAudioFrequency = obtainedAudioSpec.freq;
		SDL_PauseAudioDevice(_sdlAudioDeviceId, pause_on: 0);
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
			if (_sdlAudioDeviceId != 0)
			{
				SDL_CloseAudioDevice(_sdlAudioDeviceId);
				_sdlAudioDeviceId = 0;
			}

			OpenAudioDevice(audioDeviceName);

			lock (_resamplerLock)
			{
				_resampler?.Dispose();
				_resampler = null;
				_resampler = new(BitOperations.RoundUpToPowerOf2((uint)(_outputAudioFrequency * 20 / 1000)));
				Reset();
			}
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
	public bool RecoverLostAudioDeviceIfNeeded()
	{
		// if the device stops, it's no longer valid, and must be reset with the default device (which shouldn't ever stop?)
		if (SDL_GetAudioDeviceStatus(_sdlAudioDeviceId) == SDL_AudioStatus.SDL_AUDIO_STOPPED)
		{
			AudioDeviceName = null;
			SetAudioDevice(DEFAULT_AUDIO_DEVICE);
			return true;
		}

		return false;
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public void Pause()
	{
		SDL_PauseAudioDevice(_sdlAudioDeviceId, pause_on: 1);
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public void Unpause()
	{
		Reset();
		SDL_PauseAudioDevice(_sdlAudioDeviceId, pause_on: 0);
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
				// if we have > latency + 20 ms of the buffer used, we're likely out of sync, and thus need an audio buffer reset
				// we have quite some tolerance here, latency should normally be balanced out
				var toleratedBufferUsage = (_latencyMs + 20) * _outputAudioFrequency * 2 / 1000;
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
		if (SDL_Init(SDL_INIT_AUDIO) != 0)
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

		if (_sdlAudioDeviceId != 0)
		{
			SDL_CloseAudioDevice(_sdlAudioDeviceId);
		}

		if (_sdlUserData.IsAllocated)
		{
			_sdlUserData.Free();
		}

		SDL_QuitSubSystem(SDL_INIT_AUDIO);
	}
}
