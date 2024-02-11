using System;
using System.Collections.Generic;
using System.IO;

using GSR.Emu;
using GSR.Gui;
using GSR.Input;

namespace GSR;

internal sealed class HotkeyManager(Config config, EmuManager emuManager, InputManager inputManager, ImGuiWindow mainWindow, Func<InputGate> inputGateCallback)
{
	private record struct HotkeyState(List<InputBinding> InputBindings, bool WasPressed = false);

	private HotkeyState _pauseHotkeyState = new(config.HotkeyBindings.PauseButtonBindings);
	private HotkeyState _fullScreenHotkeyState = new(config.HotkeyBindings.FullScreenButtonBindings);
	private HotkeyState _fastForwardHotkeyState = new(config.HotkeyBindings.FastForwardButtonBindings);
	private HotkeyState _saveStateHotkeyState = new(config.HotkeyBindings.SaveStateButtonBindings);
	private HotkeyState _loadStateHotkeyState = new(config.HotkeyBindings.LoadStateButtonBindings);
	private HotkeyState _prevStateSetHotkeyState = new(config.HotkeyBindings.PrevStateSetButtonBindings);
	private HotkeyState _nextStateSetHotkeyState = new(config.HotkeyBindings.NextStateSetButtonBindings);
	private HotkeyState _prevStateSlotHotkeyState = new(config.HotkeyBindings.PrevStateSlotButtonBindings);
	private HotkeyState _nextStateSlotHotkeyState = new(config.HotkeyBindings.NextStateSlotButtonBindings);

	private readonly HotkeyState[] _selectStateSlotHotkeyStates =
	[
		new(config.HotkeyBindings.SelectStateSlot1ButtonBindings),
		new(config.HotkeyBindings.SelectStateSlot2ButtonBindings),
		new(config.HotkeyBindings.SelectStateSlot3ButtonBindings),
		new(config.HotkeyBindings.SelectStateSlot4ButtonBindings),
		new(config.HotkeyBindings.SelectStateSlot5ButtonBindings),
		new(config.HotkeyBindings.SelectStateSlot6ButtonBindings),
		new(config.HotkeyBindings.SelectStateSlot7ButtonBindings),
		new(config.HotkeyBindings.SelectStateSlot8ButtonBindings),
		new(config.HotkeyBindings.SelectStateSlot9ButtonBindings),
		new(config.HotkeyBindings.SelectStateSlot10ButtonBindings)
	];

	private readonly HotkeyState[] _saveStateSlotHotkeyStates =
	[
		new(config.HotkeyBindings.SaveStateSlot1ButtonBindings),
		new(config.HotkeyBindings.SaveStateSlot2ButtonBindings),
		new(config.HotkeyBindings.SaveStateSlot3ButtonBindings),
		new(config.HotkeyBindings.SaveStateSlot4ButtonBindings),
		new(config.HotkeyBindings.SaveStateSlot5ButtonBindings),
		new(config.HotkeyBindings.SaveStateSlot6ButtonBindings),
		new(config.HotkeyBindings.SaveStateSlot7ButtonBindings),
		new(config.HotkeyBindings.SaveStateSlot8ButtonBindings),
		new(config.HotkeyBindings.SaveStateSlot9ButtonBindings),
		new(config.HotkeyBindings.SaveStateSlot10ButtonBindings)
	];

	private readonly HotkeyState[] _loadStateSlotHotkeyStates =
	[
		new(config.HotkeyBindings.LoadStateSlot1ButtonBindings),
		new(config.HotkeyBindings.LoadStateSlot2ButtonBindings),
		new(config.HotkeyBindings.LoadStateSlot3ButtonBindings),
		new(config.HotkeyBindings.LoadStateSlot4ButtonBindings),
		new(config.HotkeyBindings.LoadStateSlot5ButtonBindings),
		new(config.HotkeyBindings.LoadStateSlot6ButtonBindings),
		new(config.HotkeyBindings.LoadStateSlot7ButtonBindings),
		new(config.HotkeyBindings.LoadStateSlot8ButtonBindings),
		new(config.HotkeyBindings.LoadStateSlot9ButtonBindings),
		new(config.HotkeyBindings.LoadStateSlot10ButtonBindings)
	];

	private void UpdateHotkeyState(ref HotkeyState hotkeyState, InputGate inputGate, Action onPress, Action onUnpress = null)
	{
		var newState = inputManager.GetInputForBindings(hotkeyState.InputBindings, inputGate);
		if (newState == hotkeyState.WasPressed)
		{
			return;
		}

		if (newState)
		{
			onPress();
		}
		else
		{
			onUnpress?.Invoke();
		}

		hotkeyState.WasPressed = newState;
	}

	private void UpdateSlotHotkeyState(ref HotkeyState slotHotkeyState, InputGate inputGate, Action<int> onPress, int slot)
	{
		var newState = inputManager.GetInputForBindings(slotHotkeyState.InputBindings, inputGate);
		if (newState && !slotHotkeyState.WasPressed)
		{
			onPress(slot);
		}

		slotHotkeyState.WasPressed = newState;
	}

	public void SaveStateCurSlot()
	{
		SaveStateSlot(config.SaveStateSlot);
	}

	public void LoadStateCurSlot()
	{
		LoadStateSlot(config.SaveStateSlot);
	}

	// TODO: a lot of these probably should be moved to Config
	public void DecStateSet()
	{
		config.SaveStateSet = config.SaveStateSet == 0 ? 9 : config.SaveStateSet - 1;
	}

	public void IncStateSet()
	{
		config.SaveStateSet = config.SaveStateSet == 9 ? 0 : config.SaveStateSet + 1;
	}

	public void DecStateSlot()
	{
		config.SaveStateSlot = config.SaveStateSlot == 0 ? 9 : config.SaveStateSlot - 1;
	}

	public void IncStateSlot()
	{
		config.SaveStateSlot = config.SaveStateSlot == 9 ? 0 : config.SaveStateSlot + 1;
	}

	public void SetStateSlot(int i)
	{
		config.SaveStateSlot = i;
	}

	public void SaveStateSlot(int slot)
	{
		var stateSlot = config.SaveStateSet * 10 + slot + 1;
		var statePath = $"{Path.Combine(emuManager.CurrentRomDirectory, emuManager.CurrentRomName)}_{stateSlot}.gqs";
		_ = emuManager.SaveState(statePath);
	}

	public void LoadStateSlot(int slot)
	{
		var stateSlot = config.SaveStateSet * 10 + slot + 1;
		var statePath = $"{Path.Combine(emuManager.CurrentRomDirectory, emuManager.CurrentRomName)}_{stateSlot}.gqs";
		_ = emuManager.LoadState(statePath);
	}

	public void ProcessHotkeys()
	{
		var inputGate = inputGateCallback();
		UpdateHotkeyState(ref _pauseHotkeyState, inputGate, emuManager.TogglePause);
		UpdateHotkeyState(ref _fullScreenHotkeyState, inputGate, mainWindow.ToggleFullscreen);
		UpdateHotkeyState(ref _fastForwardHotkeyState, inputGate, () =>
		{
			emuManager.SetSpeedFactor(config.FastForwardSpeed);
		}, () =>
		{
			emuManager.SetSpeedFactor(1);
		});
		UpdateHotkeyState(ref _saveStateHotkeyState, inputGate, SaveStateCurSlot);
		UpdateHotkeyState(ref _loadStateHotkeyState, inputGate, LoadStateCurSlot);
		UpdateHotkeyState(ref _prevStateSetHotkeyState, inputGate, DecStateSet);
		UpdateHotkeyState(ref _nextStateSetHotkeyState, inputGate, IncStateSet);
		UpdateHotkeyState(ref _prevStateSlotHotkeyState, inputGate, DecStateSlot);
		UpdateHotkeyState(ref _nextStateSlotHotkeyState, inputGate, IncStateSlot);

		for (var i = 0; i < 10; i++)
		{
			UpdateSlotHotkeyState(ref _selectStateSlotHotkeyStates[i], inputGate, SetStateSlot, i);
			UpdateSlotHotkeyState(ref _saveStateSlotHotkeyStates[i], inputGate, SaveStateSlot, i);
			UpdateSlotHotkeyState(ref _loadStateSlotHotkeyStates[i], inputGate, LoadStateSlot, i);
		}
	}
}
