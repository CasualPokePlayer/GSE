// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static SDL2.SDL;

namespace GSR.Audio;

// NOTE: Previously I tried a threading approach which offloaded resampling to a separate thread...
// That approach was very complicated and audio did not work at all for reasons I can't comprehend (edit: blipbuf managed impl was bugged, fixed now, maybe can reintroduce?)
// Thus, resampling is done on the emu thread, and we let SDL handle obtaining samples in its audio callback (called on a separate thread anyways)
public sealed class AudioManager : IDisposable
{
	public const string DEFAULT_AUDIO_DEVICE = "[Default Audio Device]";
	public const int MINIMUM_LATENCY_MS = 0;
	public const int MAXIMUM_LATENCY_MS = 100;

	private readonly AudioRingBuffer OutputAudioBuffer = new();
 
	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void SDLAudioCallback(IntPtr userdata, IntPtr stream, int len)
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

	private readonly object _audioLock = new();

	private int _inputAudioFrequency;
	private int _outputAudioFrequency;

	private BlipBuffer _resampler;
	private int _lastL, _lastR;
	private short[] _resamplingBuffer = [];

	public string AudioDeviceName { get; private set; }
	private int _latencyMs;
	private int _volume;

	private uint _sdlAudioDeviceId;

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
		wantedSdlAudioSpec.samples = 4096; // we'll let this change to however SDL best wants it

		unsafe
		{
			wantedSdlAudioSpec.callback = &SDLAudioCallback;
			var handle = GCHandle.Alloc(this, GCHandleType.Weak);
			wantedSdlAudioSpec.userdata = GCHandle.ToIntPtr(handle);
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
				throw new($"Failed to open audio device, SDL error: {SDL_GetError()}");
			}
		}

		_sdlAudioDeviceId = deviceId;
		AudioDeviceName = deviceName ?? DEFAULT_AUDIO_DEVICE;
		_outputAudioFrequency = obtainedAudioSpec.freq;
		SDL_PauseAudioDevice(_sdlAudioDeviceId, pause_on: 0);
	}

	public void Reset()
	{
		lock (_audioLock)
		{
			_lastL = _lastR = 0;
			_resampler.SetRates(_inputAudioFrequency, _outputAudioFrequency);
			_resampler.Clear();
			OutputAudioBuffer.Reset(MAXIMUM_LATENCY_MS * 2 * _outputAudioFrequency * 2 / 1000, _latencyMs * _outputAudioFrequency * 2 / 1000);
		}
	}

	public void SetInputAudioFrequency(int freq)
	{
		if (_inputAudioFrequency != freq)
		{
			lock (_audioLock)
			{
				_inputAudioFrequency = freq;
				Reset();
			}
		}
	}

	public void ChangeConfig(string audioDeviceName, int latencyMs, int volume)
	{
		if (AudioDeviceName != audioDeviceName || _latencyMs != latencyMs || _volume != volume)
		{
			lock (_audioLock)
			{
				_volume = volume;

				if (AudioDeviceName != audioDeviceName || _latencyMs != latencyMs)
				{
					if (AudioDeviceName != audioDeviceName)
					{
						_resampler?.Dispose();
						_resampler = null;

						if (_sdlAudioDeviceId != 0)
						{
							SDL_CloseAudioDevice(_sdlAudioDeviceId);
							_sdlAudioDeviceId = 0;
						}

						OpenAudioDevice(audioDeviceName);
						_resampler = new(BitOperations.RoundUpToPowerOf2((uint)(_outputAudioFrequency * 20 / 1000)));
					}

					_latencyMs = latencyMs;
					Reset();
				}
			}
		}
	}

	public bool RecoverLostAudioDeviceIfNeeded()
	{
		// if the device stops, it's no longer valid, and must be reset with the default device (which shouldn't ever stop?)
		if (SDL_GetAudioDeviceStatus(_sdlAudioDeviceId) == SDL_AudioStatus.SDL_AUDIO_STOPPED)
		{
			ChangeConfig(DEFAULT_AUDIO_DEVICE, _latencyMs, _volume);
			return true;
		}

		return false;
	}

	public void Pause()
	{
		SDL_PauseAudioDevice(_sdlAudioDeviceId, pause_on: 1);
	}

	public void Unpause()
	{
		lock (_audioLock)
		{
			Reset();
			SDL_PauseAudioDevice(_sdlAudioDeviceId, pause_on: 0);
		}
	}

	public void DispatchAudio(ReadOnlySpan<short> samples)
	{
		lock (_audioLock)
		{
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
			ChangeConfig(audioDeviceName, latencyMs, volume);
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

		SDL_QuitSubSystem(SDL_INIT_AUDIO);
	}
}
