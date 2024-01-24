using GSR.Input;

namespace GSR.Emu.Controllers;

public sealed class GBAController(InputManager inputManager, EmuControllerBindings bindings) : IEmuController
{
	public EmuControllerState GetState()
	{
		EmuButtons emuButtons = 0;

		if (inputManager.GetInputForBindings(bindings.AButtonBindings))
		{
			emuButtons |= EmuButtons.A;
		}

		if (inputManager.GetInputForBindings(bindings.BButtonBindings))
		{
			emuButtons |= EmuButtons.B;
		}

		if (inputManager.GetInputForBindings(bindings.SelectButtonBindings))
		{
			emuButtons |= EmuButtons.Select;
		}

		if (inputManager.GetInputForBindings(bindings.StartButtonBindings))
		{
			emuButtons |= EmuButtons.Start;
		}

		if (inputManager.GetInputForBindings(bindings.RightButtonBindings))
		{
			emuButtons |= EmuButtons.Right;
		}

		if (inputManager.GetInputForBindings(bindings.LeftButtonBindings))
		{
			emuButtons |= EmuButtons.Left;
		}

		if (inputManager.GetInputForBindings(bindings.UpButtonBindings))
		{
			emuButtons |= EmuButtons.Up;
		}

		if (inputManager.GetInputForBindings(bindings.DownButtonBindings))
		{
			emuButtons |= EmuButtons.Down;
		}

		if (inputManager.GetInputForBindings(bindings.RButtonBindings))
		{
			emuButtons |= EmuButtons.R;
		}

		if (inputManager.GetInputForBindings(bindings.LButtonBindings))
		{
			emuButtons |= EmuButtons.L;
		}

		if (inputManager.GetInputForBindings(bindings.HardResetButtonBindings))
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
