using System.Collections.Generic;

using GSR.Input;
using GSR.Input.Keyboards;

namespace GSR.Emu;

public sealed class EmuControllerBindings
{
	public List<InputBinding> AButtonBindings;
	public List<InputBinding> BButtonBindings;
	public List<InputBinding> SelectButtonBindings;
	public List<InputBinding> StartButtonBindings;
	public List<InputBinding> RightButtonBindings;
	public List<InputBinding> LeftButtonBindings;
	public List<InputBinding> UpButtonBindings;
	public List<InputBinding> DownButtonBindings;
	public List<InputBinding> RButtonBindings;
	public List<InputBinding> LButtonBindings;
	public List<InputBinding> HardResetButtonBindings;

	public void SetDefaultBindings(InputManager inputManager)
	{
		AButtonBindings = [ inputManager.CreateInputBindingForScanCode(ScanCode.SC_Z) ];
		BButtonBindings = [ inputManager.CreateInputBindingForScanCode(ScanCode.SC_X) ];
		SelectButtonBindings = [ inputManager.CreateInputBindingForScanCode(ScanCode.SC_A) ];
		StartButtonBindings = [ inputManager.CreateInputBindingForScanCode(ScanCode.SC_S) ];
		RightButtonBindings = [ inputManager.CreateInputBindingForScanCode(ScanCode.SC_RIGHT) ];
		LeftButtonBindings = [ inputManager.CreateInputBindingForScanCode(ScanCode.SC_LEFT) ];
		UpButtonBindings = [ inputManager.CreateInputBindingForScanCode(ScanCode.SC_UP) ];
		DownButtonBindings = [ inputManager.CreateInputBindingForScanCode(ScanCode.SC_DOWN) ];
		RButtonBindings = [ inputManager.CreateInputBindingForScanCode(ScanCode.SC_W) ];
		LButtonBindings = [ inputManager.CreateInputBindingForScanCode(ScanCode.SC_Q) ];
		HardResetButtonBindings = [ inputManager.CreateInputBindingForScanCode(ScanCode.SC_R) ];
	}

	public void DeserializeInputBindings(InputManager inputManager)
	{
		for (var i = 0; i < AButtonBindings.Count; i++)
		{
			AButtonBindings[i] = inputManager.DeserializeInputBinding(AButtonBindings[i].SerializationLabel);
		}

		for (var i = 0; i < BButtonBindings.Count; i++)
		{
			BButtonBindings[i] = inputManager.DeserializeInputBinding(BButtonBindings[i].SerializationLabel);
		}

		for (var i = 0; i < SelectButtonBindings.Count; i++)
		{
			SelectButtonBindings[i] = inputManager.DeserializeInputBinding(SelectButtonBindings[i].SerializationLabel);
		}

		for (var i = 0; i < StartButtonBindings.Count; i++)
		{
			StartButtonBindings[i] = inputManager.DeserializeInputBinding(StartButtonBindings[i].SerializationLabel);
		}

		for (var i = 0; i < RightButtonBindings.Count; i++)
		{
			RightButtonBindings[i] = inputManager.DeserializeInputBinding(RightButtonBindings[i].SerializationLabel);
		}

		for (var i = 0; i < LeftButtonBindings.Count; i++)
		{
			LeftButtonBindings[i] = inputManager.DeserializeInputBinding(LeftButtonBindings[i].SerializationLabel);
		}

		for (var i = 0; i < UpButtonBindings.Count; i++)
		{
			UpButtonBindings[i] = inputManager.DeserializeInputBinding(UpButtonBindings[i].SerializationLabel);
		}

		for (var i = 0; i < DownButtonBindings.Count; i++)
		{
			DownButtonBindings[i] = inputManager.DeserializeInputBinding(DownButtonBindings[i].SerializationLabel);
		}

		for (var i = 0; i < RButtonBindings.Count; i++)
		{
			RButtonBindings[i] = inputManager.DeserializeInputBinding(RButtonBindings[i].SerializationLabel);
		}

		for (var i = 0; i < LButtonBindings.Count; i++)
		{
			LButtonBindings[i] = inputManager.DeserializeInputBinding(LButtonBindings[i].SerializationLabel);
		}

		for (var i = 0; i < HardResetButtonBindings.Count; i++)
		{
			HardResetButtonBindings[i] = inputManager.DeserializeInputBinding(HardResetButtonBindings[i].SerializationLabel);
		}
	}
}
