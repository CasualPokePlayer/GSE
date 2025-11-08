// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

#if GSE_WINDOWS
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
#endif

using GSE.Input.Keyboards;
using GSE.Input.Joysticks;

namespace GSE.Input;

public sealed class InputManager : IDisposable
{
	// This fake "+" symbol is used to distinguish against +s from input combos
	// However, this won't necessarily display correctly
	// So it's converted back to a normal + on deserialization
	private const char POSITIVE_CHAR = '＋';

	private readonly Thread _inputThread;
	private readonly ManualResetEventSlim _inputThreadInitFinished = new();
	private readonly ManualResetEventSlim _inputThreadThrottleEnd = new();
	private readonly AutoResetEvent _inputThreadLoopThrottle = new(false);

	private volatile bool _disposing;
	private volatile InputThreadException _inputThreadException;

	private readonly uint _mainWindowProperties;
	private volatile bool _enableDirectInput;

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
			_keyInput = KeyInputFactory.CreateKeyInput(_mainWindowProperties);
			_sdlJoysticks = new(_enableDirectInput);

			_inputThreadInitFinished.Set();

			while (!_disposing)
			{
#if GSE_WINDOWS
				// on windows, we must message pump this thread for underlying input apis to work
				while (PInvoke.PeekMessage(out var msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
				{
					PInvoke.TranslateMessage(in msg);
					PInvoke.DispatchMessage(in msg);
				}
#endif
				var keyEvents = _keyInput.GetEvents();
				var joystickInputs = _sdlJoysticks.GetInputs(_enableDirectInput);

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
			_inputThreadException = new(e);
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

	public InputManager(uint mainWindowProperties, bool enableDirectInput)
	{
		_mainWindowProperties = mainWindowProperties;
		_enableDirectInput = enableDirectInput;
		_inputThread = new(InputThreadProc) { IsBackground = true, Name = "Input Thread" };
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

#if GSE_WINDOWS
	public void SetDirectInputEnable(bool enableDirectInput)
	{
		_enableDirectInput = enableDirectInput;
	}
#endif

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
		return e.ScanCode.HasValue ? $"SC {(byte)e.ScanCode.Value}" : e.InputName.Replace('+', POSITIVE_CHAR);
	}

	// migrating from v0.4 SDL2 inputs to newer SDL3 inputs
	private static string MigrateJoystickSerializationLabel(string serializationLabel)
	{
		if (serializationLabel.EndsWith(" A", StringComparison.Ordinal))
		{
			return serializationLabel.Replace(" A", " South");
		}

		if (serializationLabel.EndsWith(" B", StringComparison.Ordinal))
		{
			return serializationLabel.Replace(" B", " East");
		}

		if (serializationLabel.EndsWith(" X", StringComparison.Ordinal))
		{
			return serializationLabel.Replace(" X", " West");
		}

		if (serializationLabel.EndsWith(" Y", StringComparison.Ordinal))
		{
			return serializationLabel.Replace(" Y", " North");
		}

		// ReSharper disable once ConvertIfStatementToReturnStatement
		if (serializationLabel.EndsWith(" Misc", StringComparison.Ordinal))
		{
			return serializationLabel.Replace(" Misc", " Misc 1");
		}

		return serializationLabel;
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
			serializationLabel = MigrateJoystickSerializationLabel(serializationLabel);
			// hard to really check if this is valid, we'll just assume it is at this point
			return new(serializationLabel, null, serializationLabel.Replace(POSITIVE_CHAR, '+'));
		}

		return null;
	}

	/// <summary>
	/// NOTE: CALLED ON GUI THREAD
	/// </summary>
	public InputBinding DeserializeInputBinding(string serializationLabel)
	{
		// Old (v0.1) configs are faulty and contain +s for positive joystick inputs
		// Fix them up here (converting them to "normal" serialized labels without +)
		serializationLabel = serializationLabel.Replace("X+", $"X{POSITIVE_CHAR}");
		serializationLabel = serializationLabel.Replace("Y+", $"Y{POSITIVE_CHAR}");
		serializationLabel = serializationLabel.Replace(" +", $" {POSITIVE_CHAR}");

		var serializationLabels = serializationLabel.Split("+", 2, StringSplitOptions.RemoveEmptyEntries);
		switch (serializationLabels.Length)
		{
			case 0:
				return null;
			case 1:
				return DeserializeSingleInputBinding(serializationLabels[0]);
			default:
			{
				var modifierBinding = DeserializeSingleInputBinding(serializationLabels[0]);
				var mainBinding = DeserializeSingleInputBinding(serializationLabels[1]);
				if (modifierBinding is null || mainBinding is null)
				{
					return null;
				}

				return new(
					SerializationLabel: $"{modifierBinding.SerializationLabel}+{mainBinding.SerializationLabel}",
					ModifierLabel: modifierBinding.MainInputLabel,
					MainInputLabel: mainBinding.MainInputLabel
				);
			}
		}
	}

	/// <summary>
	/// Must be handed to DeserializeInputBinding afterwards!
	/// </summary>
	/// <param name="scanCode">Scancode to use for binding</param>
	/// <param name="modiferScanCode">Optional modifer scancode</param>
	/// <returns>InputBinding for passed scancode</returns>
	public static InputBinding CreateInputBindingForScanCode(ScanCode scanCode, ScanCode? modiferScanCode = null)
	{
		var modiferSerializationLabel = modiferScanCode.HasValue ? $"SC {(byte)modiferScanCode}+" : string.Empty;
		return new($"{modiferSerializationLabel}SC {(byte)scanCode}", null, null);
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
					inputBinding = inputBinding with
					{
						SerializationLabel = ConvertToSeralizableLabel(inputEvent),
						ModifierLabel = inputEvent.InputName
					};

					// we aren't finished unless this another key is pressed, or this key is released
					continue;
				}

				// pressed a second key, we'll consider this the non-modifier key
				inputBinding = inputBinding with
				{
					SerializationLabel = $"{inputBinding.SerializationLabel}+{ConvertToSeralizableLabel(inputEvent)}",
					MainInputLabel = inputEvent.InputName
				};

				return true;
			}

			// released key, see if this matches our current modifier (and set it as the only input if so)
			if (inputBinding.ModifierLabel == inputEvent.InputName)
			{
				inputBinding = inputBinding with
				{
					ModifierLabel = null,
					MainInputLabel = inputEvent.InputName
				};

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool GetInputState(string inputLabel, InputGate inputGate)
	{
		var isJoystickInput = inputLabel.StartsWith("JS", StringComparison.Ordinal);
		if (!inputGate.KeyInputAllowed && !isJoystickInput)
		{
			return false;
		}

		if (!inputGate.JoystickInputAllowed && isJoystickInput)
		{
			return false;
		}

		return _inputState.GetValueOrDefault(inputLabel);
	}

	/// <summary>
	/// NOTE: CALLED ON GUI OR EMU THREAD
	/// </summary>
	public bool GetInputForBindings(List<InputBinding> bindings, InputGate inputGate)
	{
		// ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
		foreach (var binding in bindings)
		{
			var modifierState = binding.ModifierLabel == null || GetInputState(binding.ModifierLabel, inputGate);
			if (modifierState && GetInputState(binding.MainInputLabel, inputGate))
			{
				return true;
			}
		}

		return false;
	}

	// ReSharper disable once UnusedMember.Global
	public void DispatchAndroidKeyEvent(int keycode, bool pressed)
	{
		((AndroidKeyInput)_keyInput).DispatchKeyEvent(keycode, pressed);
	}
}
