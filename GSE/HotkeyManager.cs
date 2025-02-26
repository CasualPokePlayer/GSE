// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using GSE.Audio;
using GSE.Emu;
using GSE.Gui;
using GSE.Input;

namespace GSE;

internal sealed class HotkeyManager
{
	private interface IHotkey
	{
		// ReSharper disable once ReturnTypeCanBeEnumerable.Global
		public List<InputBinding> InputBindings { get; }
		public List<InputBinding> SuppressingBindings { get; }
		void UpdateHotkeyState(InputGate inputGate);
	}

	private sealed class PressTriggerHotkeyState(InputManager inputManager, List<InputBinding> inputBindings, Action onPress) : IHotkey
	{
		private bool _wasPressed;

		public List<InputBinding> InputBindings => inputBindings;
		public List<InputBinding> SuppressingBindings { get; } = [];

		public void UpdateHotkeyState(InputGate inputGate)
		{
			var newState = inputManager.GetInputForBindings(inputBindings, inputGate);
			if (newState && !_wasPressed)
			{
				if (!inputManager.GetInputForBindings(SuppressingBindings, inputGate))
				{
					onPress();
				}
			}

			_wasPressed = newState;
		}
	}

	private sealed class PressUnpressTriggerHotkeyState(InputManager inputManager, List<InputBinding> inputBindings, Action onPress, Action onUnpress) : IHotkey
	{
		private bool _wasPressed;

		public List<InputBinding> InputBindings => inputBindings;
		public List<InputBinding> SuppressingBindings { get; } = [];

		public void UpdateHotkeyState(InputGate inputGate)
		{
			var newState = inputManager.GetInputForBindings(inputBindings, inputGate);
			if (newState == _wasPressed)
			{
				return;
			}

			if (newState)
			{
				if (!inputManager.GetInputForBindings(SuppressingBindings, inputGate))
				{
					onPress();
				}
			}
			else
			{
				onUnpress();
			}

			_wasPressed = newState;
		}
	}

	private sealed class PressTriggerHotkeySlotState(InputManager inputManager, List<InputBinding> inputBindings, Action<int> onPress, int slot) : IHotkey
	{
		private bool _wasPressed;

		public List<InputBinding> InputBindings => inputBindings;
		public List<InputBinding> SuppressingBindings { get; } = [];

		public void UpdateHotkeyState(InputGate inputGate)
		{
			var newState = inputManager.GetInputForBindings(inputBindings, inputGate);
			if (newState && !_wasPressed)
			{
				if (!inputManager.GetInputForBindings(SuppressingBindings, inputGate))
				{
					onPress(slot);
				}
			}

			_wasPressed = newState;
		}
	}

	private readonly Config _config;
	private readonly EmuManager _emuManager;
	private readonly AudioManager _audioManager;
	private readonly OSDManager _osdManager;
	private readonly ImGuiWindow _mainWindow;
	private readonly Func<InputGate> _inputGateCallback;
	private readonly ImmutableArray<IHotkey> _hotkeys;

	public bool InputBindingsChanged;

	public HotkeyManager(Config config, EmuManager emuManager, AudioManager audioManager, OSDManager osdManager,
		InputManager inputManager, StateManager stateManager, ImGuiWindow mainWindow, Func<InputGate> inputGateCallback)
	{
		_config = config;
		_emuManager = emuManager;
		_audioManager = audioManager;
		_osdManager = osdManager;
		_mainWindow = mainWindow;
		_inputGateCallback = inputGateCallback;

		_hotkeys =
		[
			new PressTriggerHotkeyState(inputManager, config.HotkeyBindings.PauseButtonBindings, TogglePause),
#if !GSE_ANDROID
			new PressTriggerHotkeyState(inputManager, config.HotkeyBindings.FullScreenButtonBindings, mainWindow.ToggleFullscreen),
#endif
			new PressUnpressTriggerHotkeyState(inputManager, config.HotkeyBindings.FastForwardButtonBindings, EnableFastForward, DisableFastForward),
			new PressTriggerHotkeyState(inputManager, config.HotkeyBindings.FrameStepButtonBindings, DoFrameStep),
			new PressTriggerHotkeyState(inputManager, config.HotkeyBindings.VolumeUpButtonBindings, VolumeUp),
			new PressTriggerHotkeyState(inputManager, config.HotkeyBindings.VolumeDownButtonBindings, VolumeDown),
			new PressTriggerHotkeyState(inputManager, config.HotkeyBindings.VolumeUp10ButtonBindings, VolumeUp10),
			new PressTriggerHotkeyState(inputManager, config.HotkeyBindings.VolumeDown10ButtonBindings, VolumeDown10),
			new PressTriggerHotkeyState(inputManager, config.HotkeyBindings.SaveStateButtonBindings, stateManager.SaveStateCurSlot),
			new PressTriggerHotkeyState(inputManager, config.HotkeyBindings.LoadStateButtonBindings, stateManager.LoadStateCurSlot),
			new PressTriggerHotkeyState(inputManager, config.HotkeyBindings.PrevStateSetButtonBindings, stateManager.DecStateSet),
			new PressTriggerHotkeyState(inputManager, config.HotkeyBindings.NextStateSetButtonBindings, stateManager.IncStateSet),
			new PressTriggerHotkeyState(inputManager, config.HotkeyBindings.PrevStateSlotButtonBindings, stateManager.DecStateSlot),
			new PressTriggerHotkeyState(inputManager, config.HotkeyBindings.NextStateSlotButtonBindings, stateManager.IncStateSlot),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SelectStateSlot1ButtonBindings, stateManager.SetStateSlot, 0),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SelectStateSlot2ButtonBindings, stateManager.SetStateSlot, 1),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SelectStateSlot3ButtonBindings, stateManager.SetStateSlot, 2),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SelectStateSlot4ButtonBindings, stateManager.SetStateSlot, 3),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SelectStateSlot5ButtonBindings, stateManager.SetStateSlot, 4),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SelectStateSlot6ButtonBindings, stateManager.SetStateSlot, 5),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SelectStateSlot7ButtonBindings, stateManager.SetStateSlot, 6),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SelectStateSlot8ButtonBindings, stateManager.SetStateSlot, 7),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SelectStateSlot9ButtonBindings, stateManager.SetStateSlot, 8),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SelectStateSlot10ButtonBindings, stateManager.SetStateSlot, 9),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SaveStateSlot1ButtonBindings, stateManager.SaveStateSlot, 0),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SaveStateSlot2ButtonBindings, stateManager.SaveStateSlot, 1),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SaveStateSlot3ButtonBindings, stateManager.SaveStateSlot, 2),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SaveStateSlot4ButtonBindings, stateManager.SaveStateSlot, 3),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SaveStateSlot5ButtonBindings, stateManager.SaveStateSlot, 4),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SaveStateSlot6ButtonBindings, stateManager.SaveStateSlot, 5),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SaveStateSlot7ButtonBindings, stateManager.SaveStateSlot, 6),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SaveStateSlot8ButtonBindings, stateManager.SaveStateSlot, 7),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SaveStateSlot9ButtonBindings, stateManager.SaveStateSlot, 8),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.SaveStateSlot10ButtonBindings, stateManager.SaveStateSlot, 9),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.LoadStateSlot1ButtonBindings, stateManager.LoadStateSlot, 0),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.LoadStateSlot2ButtonBindings, stateManager.LoadStateSlot, 1),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.LoadStateSlot3ButtonBindings, stateManager.LoadStateSlot, 2),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.LoadStateSlot4ButtonBindings, stateManager.LoadStateSlot, 3),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.LoadStateSlot5ButtonBindings, stateManager.LoadStateSlot, 4),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.LoadStateSlot6ButtonBindings, stateManager.LoadStateSlot, 5),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.LoadStateSlot7ButtonBindings, stateManager.LoadStateSlot, 6),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.LoadStateSlot8ButtonBindings, stateManager.LoadStateSlot, 7),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.LoadStateSlot9ButtonBindings, stateManager.LoadStateSlot, 8),
			new PressTriggerHotkeySlotState(inputManager, config.HotkeyBindings.LoadStateSlot10ButtonBindings, stateManager.LoadStateSlot, 9)
		];

		InputBindingsChanged = true;
	}

	private void TogglePause()
	{
		_emuManager.TogglePause();

		if (_config.HideMenuBarOnUnpause && !_config.AllowManualResizing)
		{
			_mainWindow.UpdateMainWindowSize(_emuManager, _config);
		}
	}

	private void EnableFastForward()
	{
		_emuManager.SetSpeedFactor(_config.FastForwardSpeed);
	}

	private void DisableFastForward()
	{
		_emuManager.SetSpeedFactor(1);
	}

	private void DoFrameStep()
	{
		_emuManager.DoFrameStep();

		if (_config.HideMenuBarOnUnpause && !_config.AllowManualResizing)
		{
			_mainWindow.UpdateMainWindowSize(_emuManager, _config);
		}
	}

	private void VolumeUp()
	{
		if (_config.Volume < 100)
		{
			_config.Volume++;
			_audioManager.SetVolume(_config.Volume);
		}

		_osdManager.QueueMessage($"Volume set to {_config.Volume}%");
	}

	private void VolumeDown()
	{
		if (_config.Volume > 0)
		{
			_config.Volume--;
			_audioManager.SetVolume(_config.Volume);
		}

		_osdManager.QueueMessage($"Volume set to {_config.Volume}%");
	}

	private void VolumeUp10()
	{
		if (_config.Volume < 100)
		{
			_config.Volume = Math.Min(_config.Volume + 10, 100);
			_audioManager.SetVolume(_config.Volume);
		}

		_osdManager.QueueMessage($"Volume set to {_config.Volume}%");
	}

	private void VolumeDown10()
	{
		if (_config.Volume > 0)
		{
			_config.Volume = Math.Max(_config.Volume - 10, 0);
			_audioManager.SetVolume(_config.Volume);
		}

		_osdManager.QueueMessage($"Volume set to {_config.Volume}%");
	}

	public void OnInputBindingsChange()
	{
		foreach (var hotkey in _hotkeys)
		{
			var suppressingBindings = (from inputBinding in hotkey.InputBindings
				where inputBinding.ModifierLabel == null
				from otherHotkey in _hotkeys
				where hotkey != otherHotkey
				from otherHotkeyBindings in otherHotkey.InputBindings
				where otherHotkeyBindings.ModifierLabel != null && otherHotkeyBindings.MainInputLabel == inputBinding.MainInputLabel
				select otherHotkeyBindings).ToArray();

			hotkey.SuppressingBindings.Clear();
			hotkey.SuppressingBindings.AddRange(suppressingBindings.Distinct());
		}

		InputBindingsChanged = false;
	}

	public void ProcessHotkeys()
	{
		var inputGate = _inputGateCallback();
		foreach (var hotkey in _hotkeys)
		{
			hotkey.UpdateHotkeyState(inputGate);
		}
	}
}
