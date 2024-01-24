using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

using GSR.Input.Keyboards;
using GSR.Input.Joysticks;

namespace GSR.Input;

public sealed class InputManager : IDisposable
{
	private readonly Thread _inputThread;
	private readonly ManualResetEventSlim _inputThreadInitFinished = new();
	private readonly ManualResetEventSlim _inputThreadThrottleEnd = new();
	private readonly AutoResetEvent _inputThreadLoopThrottle = new(false);

	private volatile bool _disposing;
	private volatile Exception _inputThreadException;

	// These must be created/destroyed on the input thread!
	private IKeyInput _keyInput;
	private SDLJoysticks _sdlJoysticks;

	private readonly record struct InputEvent(string InputName, ScanCode? ScanCode, bool IsPressed);

	private readonly ConcurrentDictionary<string, bool> _inputState = new();
	private readonly ConcurrentQueue<InputEvent> _inputEvents = new();
	private volatile bool _inInputBinding;

	private void InputThreadProc()
	{
		try
		{
			_keyInput = KeyInputFactory.CreateKeyInput();
			_sdlJoysticks = new();

			_inputThreadInitFinished.Set();

			var isWindows = OperatingSystem.IsWindowsVersionAtLeast(5); // the check for windows 2000 is superfluous, it just gets rid of a warning
			while (!_disposing)
			{
				if (isWindows)
				{
					// on windows, we must message pump this thread for underlying input apis to work
					while (PInvoke.PeekMessage(out var msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
					{
						PInvoke.TranslateMessage(in msg);
						PInvoke.DispatchMessage(in msg);
					}
				}

				var keyEvents = _keyInput.GetEvents();
				var joystickInputs = _sdlJoysticks.GetInputs();

				foreach (var keyEvent in keyEvents)
				{
					var keyName = _keyInput.ConvertScanCodeToString(keyEvent.Key);
					if (keyName != null)
					{
						var oldValue = _inputState.GetOrAdd(keyName, false);
						if (oldValue != keyEvent.IsPressed)
						{
							_inputState[keyName] = keyEvent.IsPressed;

							if (_inInputBinding)
							{
								_inputEvents.Enqueue(new(keyName, keyEvent.Key, keyEvent.IsPressed));
							}
						}
					}
				}

				foreach (var joystickInput in joystickInputs)
				{
					var oldValue = _inputState.GetOrAdd(joystickInput.ButtonName, false);
					if (oldValue != joystickInput.IsPressed)
					{
						_inputState[joystickInput.ButtonName] = joystickInput.IsPressed;

						if (_inInputBinding)
						{
							_inputEvents.Enqueue(new(joystickInput.ButtonName, null, joystickInput.IsPressed));
						}
					}
				}

				_inputThreadLoopThrottle.WaitOne(10);
				_inputThreadThrottleEnd.Set();
			}
		}
		catch (Exception e)
		{
			_inputThreadException = e;
			_inputThreadInitFinished.Set();
		}
		finally
		{
			_keyInput?.Dispose();
			_sdlJoysticks?.Dispose();
		}
	}

	private void CheckInputThreadException()
	{
		if (_inputThreadException != null)
		{
			throw _inputThreadException;
		}
	}

	public InputManager()
	{
		_inputThread = new(InputThreadProc) { IsBackground = true };
		_inputThread.Start();
		_inputThreadInitFinished.Wait();

		if (_inputThreadException != null)
		{
			Dispose();
			throw _inputThreadException;
		}
	}

	public void Dispose()
	{
		_disposing = true;
		_inputThread.Join();
		_inputThreadInitFinished.Dispose();
		_inputThreadThrottleEnd.Dispose();
		_inputThreadLoopThrottle.Dispose();
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public void BeginInputBinding()
	{
		_inInputBinding = true;
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public void EndInputBinding()
	{
		_inInputBinding = false;
		// do an update so we aren't in the middle of input polling when clearing out events
		// note that the emu thread must be paused (which will be the case if we're binding inputs)
		Update();
		_inputEvents.Clear();
	}

	private static string ConvertToSeralizableLabel(in InputEvent e)
	{
		return e.ScanCode.HasValue ? $"SC {(byte)e.ScanCode.Value}" : e.InputName;
	}

	private InputBinding DeserializeSingleInputBinding(string serializationLabel)
	{
		// scancode
		if (serializationLabel.StartsWith("SC", StringComparison.Ordinal))
		{
			if (byte.TryParse(serializationLabel.AsSpan()[2..], out var scancode))
			{
				return new(serializationLabel, null, _keyInput.ConvertScanCodeToString((ScanCode)scancode));
			}
		}
		// joystick
		else if (serializationLabel.StartsWith("JS", StringComparison.Ordinal))
		{
			// hard to really check if this is valid, we'll just assume it is at this point
			return new(serializationLabel, null, serializationLabel);
		}

		return default;
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public InputBinding DeserializeInputBinding(string serializationLabel)
	{
		var serializationLabels = serializationLabel.Split("+", 2, StringSplitOptions.RemoveEmptyEntries);
		switch (serializationLabels.Length)
		{
			case 0:
				return default;
			case 1:
				return DeserializeSingleInputBinding(serializationLabels[0]);
			default:
			{
				var modifierBinding = DeserializeSingleInputBinding(serializationLabels[0]);
				var mainBinding = DeserializeSingleInputBinding(serializationLabels[1]);
				if (modifierBinding == default || mainBinding == default)
				{
					return default;
				}

				return new(
					SerializationLabel: $"{modifierBinding.SerializationLabel}+{mainBinding.SerializationLabel}",
					ModifierLabel: modifierBinding.MainInputLabel,
					MainInputLabel: mainBinding.MainInputLabel
				);
			}
		}
	}

	public InputBinding CreateInputBindingForScanCode(ScanCode scanCode)
	{
		return new($"SC {(byte)scanCode}", null, _keyInput.ConvertScanCodeToString(scanCode));
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public bool UpdateInputBinding(ref InputBinding inputBinding)
	{
		CheckInputThreadException();
		while (_inputEvents.TryDequeue(out var inputEvent))
		{
			if (inputEvent.IsPressed)
			{
				if (inputBinding.ModifierLabel == null)
				{
					inputBinding.ModifierLabel = inputEvent.InputName;
					inputBinding.SerializationLabel = ConvertToSeralizableLabel(inputEvent);
					// we aren't finished unless this another key is pressed, or this key is released
					continue;
				}

				// pressed a second key, we'll consider this the non-modifier key
				inputBinding.MainInputLabel = inputEvent.InputName;
				inputBinding.SerializationLabel += $"+{ConvertToSeralizableLabel(inputEvent)}";
				return true;
			}

			// released key, see if this matches our current modifier (and set it as the only input if so)
			if (inputBinding.ModifierLabel == inputEvent.InputName)
			{
				inputBinding.ModifierLabel = null;
				inputBinding.MainInputLabel = inputEvent.InputName;
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// NOTE: ONLY ALLOWED ON EMU THREAD
	/// </summary>
	public void Update()
	{
		_inputThreadThrottleEnd.Reset();
		_inputThreadLoopThrottle.Set();
		while (!_inputThreadThrottleEnd.Wait(20))
		{
			CheckInputThreadException();
		}
	}

	/// <summary>
	/// NOTE: CALLED ON GUI OR EMU THREAD
	/// </summary>
	public bool GetInputForBindings(IEnumerable<InputBinding> bindings)
	{
		// ReSharper disable once LoopCanBeConvertedToQuery
		foreach (var binding in bindings)
		{
			var modifierState = binding.ModifierLabel == null || _inputState.GetValueOrDefault(binding.ModifierLabel);
			if (modifierState && _inputState.GetValueOrDefault(binding.MainInputLabel))
			{
				return true;
			}
		}

		return false;
	}
}
