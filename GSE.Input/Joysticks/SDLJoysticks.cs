// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using static SDL3.SDL;

namespace GSE.Input.Joysticks;

internal sealed class SDLJoysticks : IDisposable
{
	private readonly Dictionary<uint, SDL3Joystick> Joysticks = [];
	private readonly SDL_Event[] _sdlEvents = new SDL_Event[10];
	private bool _enableDirectInput;

	static SDLJoysticks()
	{
		SDL_SetHint(SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");
		SDL_SetHint(SDL_HINT_JOYSTICK_HIDAPI, "1");
		SDL_SetHint(SDL_HINT_JOYSTICK_RAWINPUT, "1");
	}

	public SDLJoysticks(bool enableDirectInput)
	{
		Initialize(enableDirectInput);
	}

	private void Initialize(bool enableDirectInput)
	{
		_enableDirectInput = enableDirectInput;
		SDL_SetHint(SDL_HINT_JOYSTICK_DIRECTINPUT, _enableDirectInput ? "1" : "0");
		if (!SDL_Init(SDL_InitFlags.SDL_INIT_EVENTS | SDL_InitFlags.SDL_INIT_JOYSTICK | SDL_InitFlags.SDL_INIT_GAMEPAD))
		{
			throw new($"SDL failed to init, SDL error: {SDL_GetError()}");
		}
	}

	public void Dispose()
	{
		foreach (var joystick in Joysticks.Values)
		{
			joystick.Dispose();
		}

		SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_EVENTS | SDL_InitFlags.SDL_INIT_JOYSTICK | SDL_InitFlags.SDL_INIT_GAMEPAD);
		SDL_FlushEvents((uint)SDL_EventType.SDL_EVENT_JOYSTICK_ADDED, (uint)SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED);
	}

	/// <summary>
	/// Helper ref struct for getting the joystick list in an RAII style
	/// </summary>
	private readonly ref struct SDLJoystickList
	{
		private readonly nint _joysticks;
		private readonly int _numJoysticks;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe ReadOnlySpan<uint> AsSpan()
		{
			return new((void*)_joysticks, _numJoysticks);
		}

		public SDLJoystickList()
		{
			_joysticks = SDL_GetJoysticks(out _numJoysticks);
			if (_joysticks == 0)
			{
				throw new($"Failed to obtain joysticks, SDL error {SDL_GetError()}");
			}
		}

		public void Dispose()
		{
			SDL_free(_joysticks);
		}
	}

	private void RefreshJoyIndexes()
	{
		using var joysticks = new SDLJoystickList();
		var joystickIds = joysticks.AsSpan();

		foreach (var joystick in Joysticks.Values)
		{
			joystick.ClearIndex();
		}

		var joystickIndex = 0;
		foreach (var joystickId in joystickIds)
		{
			if (Joysticks.TryGetValue(joystickId, out var joystick))
			{
				joystick.UpdateIndex(joystickIndex);
				joystickIndex++;
			}
		}

		// check in case any joystick still have their index cleared
		var joysticksToRemove = new List<uint>();
		// ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
		foreach (var joystick in Joysticks.Values)
		{
			if (joystick.IndexIsCleared())
			{
				joysticksToRemove.Add(joystick.InstanceID);
			}
		}

		foreach (var joystickToRemove in joysticksToRemove)
		{
			Joysticks.Remove(joystickToRemove);
		}
	}

	private void AddJoyDevice(uint instanceId)
	{
		if (!Joysticks.ContainsKey(instanceId))
		{
			var joystick = SDL_IsGamepad(instanceId)
				? new SDL3Gamepad(instanceId)
				: new SDL3Joystick(instanceId);
			if (!joystick.IsValid)
			{
				joystick.Dispose();
				Console.WriteLine($"Failed to connect SDL joystick with instance ID {instanceId}");
			}
			else
			{
				Joysticks.Add(joystick.InstanceID, joystick);
				Console.WriteLine($"Connected SDL joystick, instance ID {joystick.InstanceID}, name {joystick.DeviceName}");
			}
		}
		else
		{
			Console.WriteLine($"Joysticks contained a joystick with instance ID {instanceId}, ignoring add device event");
		}
	}

	private void RemoveJoyDevice(uint instanceId)
	{
		if (Joysticks.TryGetValue(instanceId, out var joystick))
		{
			joystick.Dispose();
			Joysticks.Remove(instanceId);

			Console.WriteLine($"Removed SDL joystick with instance ID {instanceId}");
		}
		else
		{
			Console.WriteLine($"Joysticks did not contain a joystick with instance ID {instanceId}, ignoring remove device event");
		}
	}

	public IEnumerable<JoystickInput> GetInputs(bool enableDirectInput)
	{
		// to change direct input being enabled, we have to deinit and init again
		// this can only be done on the input thread, hence the deferring
		if (_enableDirectInput != enableDirectInput)
		{
			Dispose();
			Joysticks.Clear();
			Initialize(enableDirectInput);
		}

		// This is needed to add joy add/remove events
		SDL_UpdateJoysticks();

		var joyIndexesChanged = false;
		while (true)
		{
			var numEvents = SDL_PeepEvents(_sdlEvents, _sdlEvents.Length,
				SDL_EventAction.SDL_GETEVENT, (uint)SDL_EventType.SDL_EVENT_JOYSTICK_ADDED, (uint)SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED);
			if (numEvents == 0)
			{
				break;
			}

			for (var i = 0; i < numEvents; i++)
			{
				// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
				switch ((SDL_EventType)_sdlEvents[i].type)
				{
					case SDL_EventType.SDL_EVENT_JOYSTICK_ADDED:
						AddJoyDevice(_sdlEvents[i].jdevice.which);
						joyIndexesChanged = true;
						break;
					case SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED:
						RemoveJoyDevice(_sdlEvents[i].jdevice.which);
						joyIndexesChanged = true;
						break;
				}
			}
		}

		if (joyIndexesChanged)
		{
			RefreshJoyIndexes();
		}

		var ret = new List<JoystickInput>();
		foreach (var joystick in Joysticks.Values)
		{
			joystick.GetInputs(ret);
		}

		return ret;
	}

	private class SDL3Joystick : IDisposable
	{
		/// <summary>SDL_Joystick handle</summary>
		private readonly nint _opaque;

		protected string InputNamePrefix;

		public readonly uint InstanceID;
		public string DeviceName { get; protected init; }
		public bool IsValid { get; protected init; }

		public SDL3Joystick(uint instanceId)
		{
			_opaque = SDL_OpenJoystick(instanceId);
			InstanceID = instanceId;
			DeviceName = SDL_GetJoystickName(_opaque);
			IsValid |= _opaque != 0;
			// index has to be set later
		}

		public virtual void Dispose()
		{
			SDL_CloseJoystick(_opaque);
		}

		public virtual void GetInputs(List<JoystickInput> inputs)
		{
			var numButtons = SDL_GetNumJoystickButtons(_opaque);
			for (var i = 0; i < numButtons; i++)
			{
				var isPressed = SDL_GetJoystickButton(_opaque, i);
				inputs.Add(new($"{InputNamePrefix} Button {i}", isPressed));
			}

			var numAxes = SDL_GetNumJoystickAxes(_opaque);
			for (var i = 0; i < numAxes; i++)
			{
				var axisVal = SDL_GetJoystickAxis(_opaque, i);
				inputs.Add(new($"{InputNamePrefix} Axis {i} +", axisVal >= 20000));
				inputs.Add(new($"{InputNamePrefix} Axis {i} -", axisVal <= -20000));
			}

			var numHats = SDL_GetNumJoystickHats(_opaque);
			for (var i = 0; i < numHats; i++)
			{
				var hatVal = SDL_GetJoystickHat(_opaque, i);
				inputs.Add(new($"{InputNamePrefix} Hat {i} Up", (hatVal & SDL_HAT_UP) == SDL_HAT_UP));
				inputs.Add(new($"{InputNamePrefix} Hat {i} Right", (hatVal & SDL_HAT_RIGHT) == SDL_HAT_RIGHT));
				inputs.Add(new($"{InputNamePrefix} Hat {i} Down", (hatVal & SDL_HAT_DOWN) == SDL_HAT_DOWN));
				inputs.Add(new($"{InputNamePrefix} Hat {i} Left", (hatVal & SDL_HAT_LEFT) == SDL_HAT_LEFT));
			}
		}

		public void UpdateIndex(int index)
		{
			InputNamePrefix = $"JS{index + 1}";
		}

		public void ClearIndex()
		{
			InputNamePrefix = null;
		}

		public bool IndexIsCleared()
		{
			return InputNamePrefix == null;
		}
	}

	private sealed class SDL3Gamepad : SDL3Joystick
	{
		private static readonly string[] _buttonStrings =
		[
			"South",
			"East",
			"West",
			"North",
			"Back",
			"Guide",
			"Start",
			"Left Stick",
			"Right Stick",
			"Left Shoulder",
			"Right Shoulder",
			"Dpad Up",
			"Dpad Down",
			"Dpad Left",
			"Dpad Right",
			"Misc 1",
			"Paddle 1",
			"Paddle 2",
			"Paddle 3",
			"Paddle 4",
			"Touchpad",
			"Misc 2",
			"Misc 3",
			"Misc 4",
			"Misc 5",
			"Misc 6",
		];

		private static readonly string[] _stickStrings =
		[
			"Left Stick X",
			"Left Stick Y",
			"Right Stick X",
			"Right Stick Y",
		];

		private static readonly string[] _triggerStrings =
		[
			"Left Trigger",
			"Right Trigger",
		];

		/// <summary>SDL_Gamepad handle</summary>
		private readonly nint _opaque;

		public SDL3Gamepad(uint instanceId)
			: base(instanceId)
		{
			_opaque = SDL_OpenGamepad(instanceId);
			DeviceName = SDL_GetGamepadName(_opaque);
			IsValid |= _opaque != 0;
		}

		public override void Dispose()
		{
			SDL_CloseGamepad(_opaque);
			base.Dispose();
		}

		public override void GetInputs(List<JoystickInput> inputs)
		{
			for (var i = 0; i < _buttonStrings.Length; i++)
			{
				if (SDL_GamepadHasButton(_opaque, (SDL_GamepadButton)i))
				{
					var isPressed = SDL_GetGamepadButton(_opaque, (SDL_GamepadButton)i);
					inputs.Add(new($"{InputNamePrefix} {_buttonStrings[i]}", isPressed));
				}
			}

			for (var i = 0; i < _stickStrings.Length; i++)
			{
				if (SDL_GamepadHasAxis(_opaque, (SDL_GamepadAxis)i))
				{
					var axisVal = SDL_GetGamepadAxis(_opaque, (SDL_GamepadAxis)i);
					inputs.Add(new($"{InputNamePrefix} {_stickStrings[i]}+", axisVal >= 20000));
					inputs.Add(new($"{InputNamePrefix} {_stickStrings[i]}-", axisVal <= -20000));
				}
			}

			for (var i = 0; i < _triggerStrings.Length; i++)
			{
				if (SDL_GamepadHasAxis(_opaque, SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFT_TRIGGER + i))
				{
					var axisVal = SDL_GetGamepadAxis(_opaque, SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFT_TRIGGER + i);
					inputs.Add(new($"{InputNamePrefix} {_triggerStrings[i]}", axisVal >= 5000));
				}
			}
		}
	}
}
