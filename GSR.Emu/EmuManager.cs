using System;
using System.Diagnostics;
using System.Threading;

using Windows.Win32;

using GSR.Audio;
using GSR.Emu.Cores;
using GSR.Emu.Controllers;

namespace GSR.Emu;

public sealed class EmuManager : IDisposable
{
	private readonly Thread _emuThread;
	private readonly object _emuThreadLock = new();
	private readonly object _emuCoreLock = new();

	private volatile bool _disposing;
	private volatile Exception _emuThreadException;

	private IEmuCore _emuCore;
	private IEmuController _emuController;

	private uint[] _lastVideoFrame;
	private readonly object _videoLock = new();

	private readonly AudioManager _audioManager;

	private readonly IntPtr _sdlRenderer;
	private EmuVideoTexture _emuVideoTexture;
	public IntPtr SdlVideoTexture => _emuVideoTexture?.Texture ?? IntPtr.Zero;

	// this shift is just used to make throttle math more accurate, as it aligns timer frequency better to emulated cpu frequencies
	// (a left shift is the same as multiplying by a power of 2, which emulated cpu frequencies will be)
	// 15 is the largest we can get away with too, any higher will potentially run into overflow issues
	private const int TIMER_FIXED_SHIFT = 15;
	private static readonly long _timerFreq = Stopwatch.Frequency << TIMER_FIXED_SHIFT;

	private UInt128 _lastTime = (UInt128)Stopwatch.GetTimestamp() << TIMER_FIXED_SHIFT;
	private long _throttleError;

	private void Throttle(uint cpuCycles)
	{
		// same as samples / audioFreq * timerFreq, but avoids needing to use float math
		// note that Stopwatch.Frequency is typically 10MHz on Windows, and always 1000MHz on non-Windows
		// (ulong cast is needed here, due to the amount of cpu cycles a gba could produce)
		var timeToThrottle = (long)((ulong)_timerFreq * cpuCycles / _emuCore.CpuFrequency);

		if (_throttleError >= timeToThrottle)
		{
			_throttleError -= timeToThrottle;
			return;
		}

		timeToThrottle -= _throttleError;

		while (true)
		{
			var curTime = (UInt128)Stopwatch.GetTimestamp() << TIMER_FIXED_SHIFT;
			var elaspedTime = (long)(curTime - _lastTime);
			_lastTime = curTime;

			// the time elasped would by the time actually spent sleeping
			// also would be the time spent emulating, which we want to discount for throttling obviously

			if (elaspedTime >= timeToThrottle)
			{
				_throttleError = elaspedTime - timeToThrottle;
				break;
			}

			timeToThrottle -= elaspedTime;

			var timeToThrottleMs = timeToThrottle * 1000 / _timerFreq;
			// if we're under 1 ms, don't throttle, leave it for the next time
			if (timeToThrottleMs < 1)
			{
				_throttleError = -timeToThrottle;
				break;
			}

			// we'll likely oversleep by at least a millisecond, so reduce throttle time by 1 ms
			// note that Thread.Sleep(0) is the same as Thread.Yield() (which we want in that case)
			Thread.Sleep((int)(timeToThrottleMs - 1));
		}
	}

	private void EmuThreadProc()
	{
		try
		{
			if (OperatingSystem.IsWindowsVersionAtLeast(5))
			{
				// raise timer resolution to 1 ms
				// TODO: it's possible to raise this to 0.5ms using the undocumented NtSetTimerResolution function, consider using that?
				_ = PInvoke.timeBeginPeriod(1);
			}

			while (!_disposing)
			{
				lock (_emuThreadLock)
				{
					var controllerState = _emuController.GetState();

					bool completedFrame;
					uint samples, cpuCycles;
					lock (_emuCoreLock)
					{
						_emuCore.Advance(controllerState, out completedFrame, out samples, out cpuCycles);
					}

					_audioManager.DispatchAudio(_emuCore.AudioBuffer[..(int)(samples * 2)]);
					Throttle(cpuCycles);

					if (completedFrame)
					{
						lock (_videoLock)
						{
							_emuCore.VideoBuffer.CopyTo(_lastVideoFrame);
						}
					}
				}
			}
		}
		catch (Exception e)
		{
			_emuThreadException = e;
		}
		finally
		{
			if (OperatingSystem.IsWindowsVersionAtLeast(5))
			{
				// restore old timer resolution
				_ = PInvoke.timeEndPeriod(1);
			}
		}
	}

	private void CheckEmuThreadException()
	{
		if (_emuThreadException != null)
		{
			throw _emuThreadException;
		}
	}

	public EmuManager(AudioManager audioManager, IntPtr sdlRenderer)
	{
		_audioManager = audioManager;
		_sdlRenderer = sdlRenderer;
		SetToNullCore();

		_emuThread = new(EmuThreadProc) { IsBackground = true };
		_emuThread.Start();
	}

	public void Dispose()
	{
		_disposing = true;
		_emuThread.Join();
		_emuCore.Dispose();
		_emuVideoTexture?.Dispose();
	}

	private void SetToNullCore()
	{
		_emuCore = NullCore.Singleton;
		_emuController = NullController.Singleton;
		_audioManager.SetInputAudioFrequency(_emuCore.AudioFrequency);
		_emuVideoTexture?.Dispose();
		_emuVideoTexture = null;
	}

	private void ResetThrottleState()
	{
		_lastTime = (UInt128)Stopwatch.GetTimestamp() << TIMER_FIXED_SHIFT;
		_throttleError = 0;
	}

	public void LoadRom(EmuCoreType coreType, IEmuController emuController, byte[] romBuffer, byte[] biosBuffer)
	{
		lock (_emuThreadLock)
		{
			CheckEmuThreadException();
			_emuCore.Dispose();
			_emuCore = null;
			try
			{
				_emuCore = EmuCoreFactory.CreateEmuCore(coreType, romBuffer, biosBuffer);
				_emuController = emuController;
				_lastVideoFrame = new uint[_emuCore.VideoBuffer.Length];
				ResetThrottleState();
				_audioManager.SetInputAudioFrequency(_emuCore.AudioFrequency);
				_emuVideoTexture = new(_sdlRenderer, _emuCore.VideoWidth, _emuCore.VideoHeight);
			}
			catch
			{
				_emuCore?.Dispose();
				SetToNullCore();
				throw;
			}
		}
	}

	public void DrawLastFrameToTexture()
	{
		lock (_videoLock)
		{
			_emuVideoTexture?.DrawVideo(_lastVideoFrame);
		}
	}
}
