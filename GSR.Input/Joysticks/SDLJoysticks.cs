// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;

using static SDL2.SDL;

namespace GSR.Input.Joysticks;

internal sealed class SDLJoysticks : IDisposable
{
	// fake "+" symbol, needed as we need to distinguish against +s from input combos
	private const char POSITIVE_CHAR = '＋';

	private readonly Dictionary<int, SDL2Joystick> Joysticks = [];
	private readonly SDL_Event[] _sdlEvents = new SDL_Event[10];
	private bool _enableDirectInput;

	static SDLJoysticks()
	{
		SDL_SetHint(SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");
		SDL_SetHint(SDL_HINT_JOYSTICK_HIDAPI, "1");
	}

	public SDLJoysticks(bool enableDirectInput)
	{
		Initialize(enableDirectInput);
	}

	private void Initialize(bool enableDirectInput)
	{
		_enableDirectInput = enableDirectInput;
		SDL_SetHint("SDL_DIRECTINPUT_ENABLED", _enableDirectInput ? "1" : "0");
		if (SDL_Init(SDL_INIT_EVENTS | SDL_INIT_JOYSTICK | SDL_INIT_GAMECONTROLLER) != 0)
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

		SDL_QuitSubSystem(SDL_INIT_EVENTS | SDL_INIT_JOYSTICK | SDL_INIT_GAMECONTROLLER);
		SDL_FlushEvents(SDL_EventType.SDL_JOYDEVICEADDED, SDL_EventType.SDL_JOYDEVICEREMOVED);
	}

	private void RefreshJoyIndexes()
	{
		var numJoysticks = SDL_NumJoysticks();
		for (var i = 0; i < numJoysticks; i++)
		{
			var joystickId = SDL_JoystickGetDeviceInstanceID(i);
			if (Joysticks.TryGetValue(joystickId, out var joystick))
			{
				joystick.UpdateIndex(i);
			}
		}
	}

	private void AddJoyDevice(int deviceIndex)
	{
		var instanceId = SDL_JoystickGetDeviceInstanceID(deviceIndex);
		if (!Joysticks.ContainsKey(instanceId))
		{
			var joystick = SDL_IsGameController(deviceIndex) == SDL_bool.SDL_TRUE
				? new SDL2GameController(deviceIndex)
				: new SDL2Joystick(deviceIndex);
			Joysticks.Add(joystick.InstanceID, joystick);

			Console.WriteLine($"Connected SDL joystick, device index {deviceIndex}, instance ID {joystick.InstanceID}, name {joystick.DeviceName}");
		}
		else
		{
			Console.WriteLine($"Joysticks contained a joystick with instance ID {instanceId}, ignoring add device event");
		}

		RefreshJoyIndexes();
	}

	public void RemoveJoyDevice(int deviceInstanceId)
	{
		if (Joysticks.TryGetValue(deviceInstanceId, out var joystick))
		{
			joystick.Dispose();
			Joysticks.Remove(deviceInstanceId);
		}
		else
		{
			Console.WriteLine($"Joysticks did not contain a joystick with instance ID {deviceInstanceId}, ignoring remove device event");
		}

		RefreshJoyIndexes();
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
		SDL_JoystickUpdate();

		while (true)
		{
			var numEvents = SDL_PeepEvents(_sdlEvents, _sdlEvents.Length, SDL_eventaction.SDL_GETEVENT, SDL_EventType.SDL_JOYDEVICEADDED, SDL_EventType.SDL_JOYDEVICEREMOVED);
			if (numEvents == 0)
			{
				break;
			}

			for (var i = 0; i < numEvents; i++)
			{
				// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
				switch (_sdlEvents[i].type)
				{
					case SDL_EventType.SDL_JOYDEVICEADDED:
						AddJoyDevice(_sdlEvents[i].jdevice.which);
						break;
					case SDL_EventType.SDL_JOYDEVICEREMOVED:
						RemoveJoyDevice(_sdlEvents[i].jdevice.which);
						break;
				}
			}
		}

		// One more time for good measure, in case we just connected a joystick in the event loop
		SDL_JoystickUpdate();

		var ret = new List<JoystickInput>();
		foreach (var joystick in Joysticks.Values)
		{
			joystick.GetInputs(ret);
		}

		return ret;
	}

	private class SDL2Joystick : IDisposable
	{
		/// <summary>SDL_Joystick handle</summary>
		private readonly nint _opaque;

		protected string InputNamePrefix;

		public readonly int InstanceID;
		public string DeviceName { get; protected init; }

		public SDL2Joystick(int index)
		{
			_opaque = SDL_JoystickOpen(index);
			InstanceID = SDL_JoystickGetDeviceInstanceID(index);
			DeviceName = SDL_JoystickName(_opaque);
			UpdateIndex(index);
		}

		public virtual void Dispose()
		{
			SDL_JoystickClose(_opaque);
		}

		public virtual void GetInputs(IList<JoystickInput> inputs)
		{
			var numButtons = SDL_JoystickNumButtons(_opaque);
			for (var i = 0; i < numButtons; i++)
			{
				var isPressed = SDL_JoystickGetButton(_opaque, i) == 1;
				inputs.Add(new($"{InputNamePrefix} Button {i}", isPressed));
			}

			var numAxes = SDL_JoystickNumAxes(_opaque);
			for (var i = 0; i < numAxes; i++)
			{
				var axisVal = SDL_JoystickGetAxis(_opaque, i);
				inputs.Add(new($"{InputNamePrefix} Axis {i} {POSITIVE_CHAR}", axisVal >= 20000));
				inputs.Add(new($"{InputNamePrefix} Axis {i} -", axisVal <= -20000));
			}

			var numHats = SDL_JoystickNumHats(_opaque);
			for (var i = 0; i < numHats; i++)
			{
				var hatVal = SDL_JoystickGetHat(_opaque, i);
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
	}

	private sealed class SDL2GameController : SDL2Joystick
	{
		private static readonly string[] _buttonStrings =
		[
			"A",
			"B",
			"X",
			"Y",
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
			"Misc",
			"Paddle 1",
			"Paddle 2",
			"Paddle 3",
			"Paddle 4",
			"Touchpad"
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

		/// <summary>SDL_GameController handle</summary>
		private readonly nint _opaque;

		public SDL2GameController(int index)
			: base(index)
		{
			_opaque = SDL_GameControllerOpen(index);
			DeviceName = SDL_GameControllerName(_opaque);
		}

		public override void Dispose()
		{
			SDL_GameControllerClose(_opaque);
			base.Dispose();
		}

		public override void GetInputs(IList<JoystickInput> inputs)
		{
			for (var i = 0; i < _buttonStrings.Length; i++)
			{
				if (SDL_GameControllerHasButton(_opaque, (SDL_GameControllerButton)i) == SDL_bool.SDL_TRUE)
				{
					var isPressed = SDL_GameControllerGetButton(_opaque, (SDL_GameControllerButton)i) == 1;
					inputs.Add(new($"{InputNamePrefix} {_buttonStrings[i]}", isPressed));
				}
			}

			for (var i = 0; i < _stickStrings.Length; i++)
			{
				if (SDL_GameControllerHasAxis(_opaque, (SDL_GameControllerAxis)i) == SDL_bool.SDL_TRUE)
				{
					var axisVal = SDL_GameControllerGetAxis(_opaque, (SDL_GameControllerAxis)i);
					inputs.Add(new($"{InputNamePrefix} {_stickStrings[i]}{POSITIVE_CHAR}", axisVal >= 20000));
					inputs.Add(new($"{InputNamePrefix} {_stickStrings[i]}-", axisVal <= -20000));
				}
			}

			for (var i = 0; i < _triggerStrings.Length; i++)
			{
				if (SDL_GameControllerHasAxis(_opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT + i) == SDL_bool.SDL_TRUE)
				{
					var axisVal = SDL_GameControllerGetAxis(_opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT + i);
					inputs.Add(new($"{InputNamePrefix} {_triggerStrings[i]}", axisVal >= 5000));
				}
			}
		}
	}
}
