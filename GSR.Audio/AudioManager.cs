using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static SDL2.SDL;

namespace GSR.Audio;

// TODO: previously I tried a threading approach which offloaded resampling to a separate thread...
// that approach was very complicated and audio did not work at all for reasons I can't comprehend (edit: blipbuf managed impl was bugged, fixed now, maybe can reintroduce?)
// thus, resampling is done on the emu thread, and we let SDL handle obtaining samples in its audio callback (called on a separate thread anyways)
public sealed class AudioManager : IDisposable
{
	public const int MINIMUM_BUFFER_MS = 30;
	public const int MAXIMUM_BUFFER_MS = 100;

	private readonly AudioRingBuffer OutputAudioBuffer = new();
 
	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void SDLAudioCallback(IntPtr userdata, IntPtr stream, int len)
	{
		var manager = (AudioManager)GCHandle.FromIntPtr(userdata).Target!;
		var samples = len / 2;
		var samplesRead = manager.OutputAudioBuffer.Read(new((void*)stream, samples));
		if (samplesRead < samples)
		{
			new Span<short>((void*)(stream + samplesRead * 2), samples - samplesRead).Clear();
		}
	}

	private readonly object _audioLock = new();

	private int _inputAudioFrequency;
	private int _outputAudioFrequency;
	private int _bufferMs;

	private BlipBuffer _resampler;
	private int _lastL, _lastR;
	private short[] _resamplingBuffer = [];

	private uint _sdlAudioDeviceId;

	private void OpenAudioDevice(string deviceName)
	{
		var wantedSdlAudioSpec = default(SDL_AudioSpec);
		wantedSdlAudioSpec.freq = 48000; // we'll allow this to change, as we need to handle resampling anyways
		wantedSdlAudioSpec.format = AUDIO_S16SYS;
		wantedSdlAudioSpec.channels = 2;
		wantedSdlAudioSpec.samples = 1024; // we'll also let this change, we're doing buffering already...

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
			deviceId = SDL_OpenAudioDevice(
				device: null,
				iscapture: 0,
				desired: ref wantedSdlAudioSpec,
				obtained: out obtainedAudioSpec,
				allowed_changes: (int)(SDL_AUDIO_ALLOW_SAMPLES_CHANGE | SDL_AUDIO_ALLOW_FREQUENCY_CHANGE)
			);

			if (deviceId == 0)
			{
				throw new($"Failed to open audio device, SDL error: {SDL_GetError()}");
			}
		}

		_sdlAudioDeviceId = deviceId;
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
			var outputBufferSize = (int)BitOperations.RoundUpToPowerOf2((uint)(_bufferMs * _outputAudioFrequency / 1000));
			OutputAudioBuffer.Reset(outputBufferSize);
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

	public void SetBufferMs(int bufferMs)
	{
		if (_bufferMs != bufferMs)
		{
			lock (_audioLock)
			{
				_bufferMs = bufferMs;
				Reset();
			}
		}
	}

	public void RecoverLostAudioDeviceIfNeeded()
	{
		// if the device stops, it's no longer valid, and must be reset with the default device (which shouldn't ever stop?)
		if (SDL_GetAudioDeviceStatus(_sdlAudioDeviceId) == SDL_AudioStatus.SDL_AUDIO_STOPPED)
		{
			lock (_audioLock)
			{
				_resampler?.Dispose();
				_resampler = null;
				
				if (_sdlAudioDeviceId != 0)
				{
					SDL_CloseAudioDevice(_sdlAudioDeviceId);
					_sdlAudioDeviceId = 0;
				}

				OpenAudioDevice(null);
				_resampler = new(BitOperations.RoundUpToPowerOf2((uint)(_outputAudioFrequency * 20 / 1000)));
				Reset();
			}
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

			var samplesRead = _resampler.ReadSamples(_resamplingBuffer);
			OutputAudioBuffer.Write(_resamplingBuffer.AsSpan()[..((int)samplesRead * 2)]);
		}
	}

	public AudioManager(int bufferMs, string deviceName)
	{
		if (SDL_Init(SDL_INIT_AUDIO) != 0)
		{
			throw new($"SDL failed to init, SDL error: {SDL_GetError()}");
		}

		_inputAudioFrequency = 48000;
		_bufferMs = bufferMs;

		try
		{
			OpenAudioDevice(deviceName);
			_resampler = new(BitOperations.RoundUpToPowerOf2((uint)(_outputAudioFrequency * 20 / 1000)));
			Reset();
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
