using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using GSR.Emu;
using GSR.Gui;
using GSR.Input;

namespace GSR;

internal sealed class HotkeyManager
{
	private interface IHotkey
	{
		public List<InputBinding> InputBindings { get; }
		public List<InputBinding> SuppressingBindings { get; }
		void UpdateHotkeyState(InputGate inputGate);
	}

	private class PressTriggerHotkeyState(InputManager inputManager, List<InputBinding> inputBindings, Action onPress) : IHotkey
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

	private class PressUnpressTriggerHotkeyState(InputManager inputManager, List<InputBinding> inputBindings, Action onPress, Action onUnpress) : IHotkey
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

	private class PressTriggerHotkeySlotState(InputManager inputManager, List<InputBinding> inputBindings, Action<int> onPress, int slot) : IHotkey
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
	private readonly Func<InputGate> _inputGateCallback;
	private readonly ImmutableArray<IHotkey> _hotkeys;

	public bool InputBindingsChanged;

	public HotkeyManager(Config config, EmuManager emuManager, InputManager inputManager, StateManager stateManager, ImGuiWindow mainWindow, Func<InputGate> inputGateCallback)
	{
		_config = config;
		_emuManager = emuManager;
		_inputGateCallback = inputGateCallback;

		_hotkeys =
		[
			new PressTriggerHotkeyState(inputManager, config.HotkeyBindings.PauseButtonBindings, emuManager.TogglePause),
			new PressTriggerHotkeyState(inputManager, config.HotkeyBindings.FullScreenButtonBindings, mainWindow.ToggleFullscreen),
			new PressUnpressTriggerHotkeyState(inputManager, config.HotkeyBindings.FastForwardButtonBindings, EnableFastForward, DisableFastForward),
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

	private void EnableFastForward()
	{
		_emuManager.SetSpeedFactor(_config.FastForwardSpeed);
	}

	private void DisableFastForward()
	{
		_emuManager.SetSpeedFactor(1);
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
