using System;
using System.Diagnostics;
using System.IO;
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
	private bool _emuPaused;
	private ulong _emuCycleCount;

	public bool EmuAcceptingInputs => RomIsLoaded && !_emuPaused;
	public bool RomIsLoaded { get; private set; }
	public string CurrentRomDirectory { get; private set; }
	public string CurrentRomName { get; private set; }
	public GBPlatform CurrentGbPlatform { get; private set; }

	private uint[] _lastVideoFrame;
	private uint[] _lastVideoFrameCopy;
	private readonly object _videoLock = new();

	private readonly AudioManager _audioManager;

	// this shift is just used to make throttle math more accurate, as it aligns timer frequency better to emulated cpu frequencies
	// (a left shift is the same as multiplying by a power of 2, which emulated cpu frequencies will be)
	// 15 is the largest we can get away with too, any higher will potentially run into overflow issues
	private const int TIMER_FIXED_SHIFT = 15;
	private static readonly long _timerFreq = Stopwatch.Frequency << TIMER_FIXED_SHIFT;

	private UInt128 _lastTime = (UInt128)Stopwatch.GetTimestamp() << TIMER_FIXED_SHIFT;
	private long _throttleError;
	private int _speedFactor = 1;

	private void Throttle(uint cpuCycles)
	{
		// same as samples / audioFreq * timerFreq, but avoids needing to use float math
		// note that Stopwatch.Frequency is typically 10MHz on Windows, and always 1000MHz on non-Windows
		// (ulong cast is needed here, due to the amount of cpu cycles a gba could produce)
		var timeToThrottle = (long)((ulong)_timerFreq * cpuCycles / _emuCore.CpuFrequency);
		if (_speedFactor != 1)
		{
			timeToThrottle /= _speedFactor;
		}

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
					bool completedFrame;
					uint samples, cpuCycles;
					lock (_emuCoreLock)
					{
						if (!_emuPaused)
						{
							var controllerState = _emuController.GetState(_speedFactor == 1);
							_emuCore.Advance(controllerState, out completedFrame, out samples, out cpuCycles);
							_emuCycleCount += cpuCycles;
						}
						else
						{
							completedFrame = false;
							samples = 0;
							cpuCycles = _emuCore.CpuFrequency / 60;
						}
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

	public EmuManager(AudioManager audioManager)
	{
		_audioManager = audioManager;
		SetToNullCore();

		_emuThread = new(EmuThreadProc) { IsBackground = true, Name = "Emu Thread" };
		_emuThread.Start();
	}

	public void Dispose()
	{
		_disposing = true;
		_emuThread.Join();
		_emuCore.Dispose();
	}

	private void SetToNullCore()
	{
		_emuCore?.Dispose();
		_emuCore = NullCore.Singleton;
		_emuController = NullController.Singleton;
		_emuCycleCount = 0;
		_audioManager.SetInputAudioFrequency(_emuCore.AudioFrequency);
		CurrentRomDirectory = CurrentRomName = null;
		RomIsLoaded = false;
	}

	private void ResetThrottleState()
	{
		_lastTime = (UInt128)Stopwatch.GetTimestamp() << TIMER_FIXED_SHIFT;
		_throttleError = 0;
	}

	public bool Pause()
	{
		lock (_emuCoreLock)
		{
			if (!_emuPaused)
			{
				_audioManager.Pause();
				_emuPaused = true;
				return true;
			}

			return false;
		}
	}

	public void Unpause()
	{
		lock (_emuCoreLock)
		{
			if (_emuPaused)
			{
				_audioManager.Unpause();
				_emuPaused = false;
			}
		}
	}

	public void TogglePause()
	{
		if (_emuPaused)
		{
			Unpause();
		}
		else
		{
			Pause();
		}
	}

	public void LoadRom(EmuLoadArgs loadArgs)
	{
		lock (_emuThreadLock)
		{
			UnloadRom();
			try
			{
				_emuCore = EmuCoreFactory.CreateEmuCore(loadArgs);
				_emuController = loadArgs.EmuController;
				_emuCycleCount = 0;
				CurrentRomDirectory = loadArgs.RomDirectory;
				CurrentRomName = loadArgs.RomName;
				CurrentGbPlatform = loadArgs.GbPlatform;
				RomIsLoaded = true;
				_lastVideoFrame = new uint[_emuCore.VideoBuffer.Length];
				_lastVideoFrameCopy = new uint[_emuCore.VideoBuffer.Length];
				_audioManager.SetInputAudioFrequency(_emuCore.AudioFrequency);
				ResetThrottleState();
			}
			catch
			{
				SetToNullCore();
				throw;
			}
		}
	}

	public void UnloadRom()
	{
		lock (_emuThreadLock)
		{
			CheckEmuThreadException();
			SetToNullCore();
		}
	}

	public EmuVideoBuffer GetVideoBuffer()
	{
		lock (_videoLock)
		{
			CheckEmuThreadException();
			_lastVideoFrame.AsSpan().CopyTo(_lastVideoFrameCopy);
		}

		return new(_lastVideoFrameCopy, _emuCore.VideoWidth, _emuCore.VideoHeight);
	}

	public (int Width, int Height) GetVideoDimensions(bool hideSgbBorder)
	{
		if (!RomIsLoaded)
		{
			return (240, 160); // default to GBA resolution, I guess
		}

		return hideSgbBorder && CurrentGbPlatform == GBPlatform.SGB2
			? (160, 144) // kind of annoying hardcoding, but eh
			: (_emuCore.VideoWidth, _emuCore.VideoHeight);
	}

	public bool SaveState(string statePath)
	{
		ReadOnlySpan<byte> stateBuf;
		lock (_emuCoreLock)
		{
			CheckEmuThreadException();
			stateBuf = _emuCore.SaveState();
		}

		try
		{
			using var fs = File.OpenWrite(statePath);
			fs.Write(stateBuf);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool LoadState(string statePath)
	{
		byte[] stateBuf;
		try
		{
			stateBuf = File.ReadAllBytes(statePath);
		}
		catch
		{
			return false;
		}

		lock (_emuCoreLock)
		{
			CheckEmuThreadException();
			return _emuCore.LoadState(stateBuf);
		}
	}

	public void SetSpeedFactor(int speedFactor)
	{
		lock (_emuThreadLock)
		{
			_speedFactor = speedFactor;
			if (_speedFactor == 1)
			{
				_audioManager.Reset();
				ResetThrottleState();
			}
		}
	}

	public void SetColorCorrectionEnable(bool enable)
	{
		lock (_emuCoreLock)
		{
			CheckEmuThreadException();
			_emuCore.SetColorCorrectionEnable(enable);
		}
	}

	public ulong GetCycleCount()
	{
		lock (_emuCoreLock)
		{
			return _emuCycleCount;
		}
	}
}
