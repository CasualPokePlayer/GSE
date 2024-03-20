// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System.Collections.Generic;
using System.Text.Json.Serialization;

using GSR.Input;
using GSR.Input.Keyboards;

namespace GSR.Emu;

public sealed class EmuControllerBindings
{
	public List<InputBinding> AButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_Z) ];
	public List<InputBinding> BButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_X) ];
	public List<InputBinding> SelectButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_A) ];
	public List<InputBinding> StartButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_S) ];
	public List<InputBinding> RightButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_RIGHT) ];
	public List<InputBinding> LeftButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_LEFT) ];
	public List<InputBinding> UpButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_UP) ];
	public List<InputBinding> DownButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_DOWN) ];
	public List<InputBinding> RButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_W) ];
	public List<InputBinding> LButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_Q) ];
	public List<InputBinding> HardResetButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_R) ];

	[JsonConstructor]
	public EmuControllerBindings()
	{
	}

	public void DeserializeInputBindings(InputManager inputManager)
	{
		List<InputBinding>[] bindings =
		[
			AButtonBindings,
			BButtonBindings,
			SelectButtonBindings,
			StartButtonBindings,
			RightButtonBindings,
			LeftButtonBindings,
			UpButtonBindings,
			DownButtonBindings,
			RButtonBindings,
			LButtonBindings,
			HardResetButtonBindings
		];

		foreach (var binding in bindings)
		{
			for (var i = 0; i < binding.Count; i++)
			{
				binding[i] = inputManager.DeserializeInputBinding(binding[i].SerializationLabel);
			}

			binding.RemoveAll(b => b is null);
		}
	}
}
