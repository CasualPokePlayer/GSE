// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;

using GSR.Input;

namespace GSR.Emu.Controllers;

public sealed class GBController(InputManager inputManager, EmuControllerBindings bindings, Func<InputGate> inputGateCallback) : IEmuController
{
	public EmuControllerState GetState(bool immediateUpdate)
	{
		if (immediateUpdate)
		{
			inputManager.Update();
		}

		EmuButtons emuButtons = 0;
		var inputGate = inputGateCallback();

		if (inputManager.GetInputForBindings(bindings.AButtonBindings, inputGate))
		{
			emuButtons |= EmuButtons.A;
		}

		if (inputManager.GetInputForBindings(bindings.BButtonBindings, inputGate))
		{
			emuButtons |= EmuButtons.B;
		}

		if (inputManager.GetInputForBindings(bindings.SelectButtonBindings, inputGate))
		{
			emuButtons |= EmuButtons.Select;
		}

		if (inputManager.GetInputForBindings(bindings.StartButtonBindings, inputGate))
		{
			emuButtons |= EmuButtons.Start;
		}

		if (inputManager.GetInputForBindings(bindings.RightButtonBindings, inputGate))
		{
			emuButtons |= EmuButtons.Right;
		}

		if (inputManager.GetInputForBindings(bindings.LeftButtonBindings, inputGate))
		{
			emuButtons |= EmuButtons.Left;
		}

		if (inputManager.GetInputForBindings(bindings.UpButtonBindings, inputGate))
		{
			emuButtons |= EmuButtons.Up;
		}

		if (inputManager.GetInputForBindings(bindings.DownButtonBindings, inputGate))
		{
			emuButtons |= EmuButtons.Down;
		}

		if (inputManager.GetInputForBindings(bindings.HardResetButtonBindings, inputGate))
		{
			emuButtons |= EmuButtons.HardReset;
		}

		if ((emuButtons & EmuButtons.LR_DIR_MASK) == EmuButtons.LR_DIR_MASK)
		{
			emuButtons &= ~EmuButtons.LR_DIR_MASK;
		}

		if ((emuButtons & EmuButtons.UD_DIR_MASK) == EmuButtons.UD_DIR_MASK)
		{
			emuButtons &= ~EmuButtons.UD_DIR_MASK;
		}

		return new(emuButtons);
	}
}
