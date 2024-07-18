// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System.Collections.Generic;
using System.Text.Json.Serialization;

using GSE.Input;
using GSE.Input.Keyboards;

namespace GSE;

internal sealed class HotkeyBindings
{
	public List<InputBinding> PauseButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_P) ];
	public List<InputBinding> FullScreenButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F, ScanCode.SC_LEFTCONTROL) ];
	public List<InputBinding> FastForwardButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_TAB) ];
	public List<InputBinding> FrameStepButtonBindings { get; set; } = [];
	public List<InputBinding> VolumeUpButtonBindings { get; set; } = [];
	public List<InputBinding> VolumeDownButtonBindings { get; set; } = [];
	public List<InputBinding> VolumeUp10ButtonBindings { get; set; } = [];
	public List<InputBinding> VolumeDown10ButtonBindings { get; set; } = [];
	public List<InputBinding> SaveStateButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_LEFTBRACKET) ];
	public List<InputBinding> LoadStateButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_RIGHTBRACKET) ];
	public List<InputBinding> PrevStateSetButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_MINUS, ScanCode.SC_LEFTSHIFT) ];
	public List<InputBinding> NextStateSetButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_EQUALS, ScanCode.SC_LEFTSHIFT) ];
	public List<InputBinding> PrevStateSlotButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_MINUS) ];
	public List<InputBinding> NextStateSlotButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_EQUALS) ];
	public List<InputBinding> SelectStateSlot1ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_1) ];
	public List<InputBinding> SelectStateSlot2ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_2) ];
	public List<InputBinding> SelectStateSlot3ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_3) ];
	public List<InputBinding> SelectStateSlot4ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_4) ];
	public List<InputBinding> SelectStateSlot5ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_5) ];
	public List<InputBinding> SelectStateSlot6ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_6) ];
	public List<InputBinding> SelectStateSlot7ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_7) ];
	public List<InputBinding> SelectStateSlot8ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_8) ];
	public List<InputBinding> SelectStateSlot9ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_9) ];
	public List<InputBinding> SelectStateSlot10ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_0) ];
	public List<InputBinding> SaveStateSlot1ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F1, ScanCode.SC_LEFTSHIFT) ];
	public List<InputBinding> SaveStateSlot2ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F2, ScanCode.SC_LEFTSHIFT) ];
	public List<InputBinding> SaveStateSlot3ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F3, ScanCode.SC_LEFTSHIFT) ];
	public List<InputBinding> SaveStateSlot4ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F4, ScanCode.SC_LEFTSHIFT) ];
	public List<InputBinding> SaveStateSlot5ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F5, ScanCode.SC_LEFTSHIFT) ];
	public List<InputBinding> SaveStateSlot6ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F6, ScanCode.SC_LEFTSHIFT) ];
	public List<InputBinding> SaveStateSlot7ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F7, ScanCode.SC_LEFTSHIFT) ];
	public List<InputBinding> SaveStateSlot8ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F8, ScanCode.SC_LEFTSHIFT) ];
	public List<InputBinding> SaveStateSlot9ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F9, ScanCode.SC_LEFTSHIFT) ];
	public List<InputBinding> SaveStateSlot10ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F10, ScanCode.SC_LEFTSHIFT) ];
	public List<InputBinding> LoadStateSlot1ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F1) ];
	public List<InputBinding> LoadStateSlot2ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F2) ];
	public List<InputBinding> LoadStateSlot3ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F3) ];
	public List<InputBinding> LoadStateSlot4ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F4) ];
	public List<InputBinding> LoadStateSlot5ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F5) ];
	public List<InputBinding> LoadStateSlot6ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F6) ];
	public List<InputBinding> LoadStateSlot7ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F7) ];
	public List<InputBinding> LoadStateSlot8ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F8) ];
	public List<InputBinding> LoadStateSlot9ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F9) ];
	public List<InputBinding> LoadStateSlot10ButtonBindings { get; set; } = [ InputManager.CreateInputBindingForScanCode(ScanCode.SC_F10) ];

	[JsonConstructor]
	public HotkeyBindings()
	{
	}

	public void DeserializeInputBindings(InputManager inputManager)
	{
		// Make sure to update this every time a hotkey is added!
		List<InputBinding>[] bindings =
		[
			PauseButtonBindings,
			FullScreenButtonBindings,
			FastForwardButtonBindings,
			FrameStepButtonBindings,
			VolumeUpButtonBindings,
			VolumeDownButtonBindings,
			VolumeUp10ButtonBindings,
			VolumeDown10ButtonBindings,
			SaveStateButtonBindings,
			LoadStateButtonBindings,
			PrevStateSetButtonBindings,
			NextStateSetButtonBindings,
			PrevStateSlotButtonBindings,
			NextStateSlotButtonBindings,
			SelectStateSlot1ButtonBindings,
			SelectStateSlot2ButtonBindings,
			SelectStateSlot3ButtonBindings,
			SelectStateSlot4ButtonBindings,
			SelectStateSlot5ButtonBindings,
			SelectStateSlot6ButtonBindings,
			SelectStateSlot7ButtonBindings,
			SelectStateSlot8ButtonBindings,
			SelectStateSlot9ButtonBindings,
			SelectStateSlot10ButtonBindings,
			SaveStateSlot1ButtonBindings,
			SaveStateSlot2ButtonBindings,
			SaveStateSlot3ButtonBindings,
			SaveStateSlot4ButtonBindings,
			SaveStateSlot5ButtonBindings,
			SaveStateSlot6ButtonBindings,
			SaveStateSlot7ButtonBindings,
			SaveStateSlot8ButtonBindings,
			SaveStateSlot9ButtonBindings,
			SaveStateSlot10ButtonBindings,
			LoadStateSlot1ButtonBindings,
			LoadStateSlot2ButtonBindings,
			LoadStateSlot3ButtonBindings,
			LoadStateSlot4ButtonBindings,
			LoadStateSlot5ButtonBindings,
			LoadStateSlot6ButtonBindings,
			LoadStateSlot7ButtonBindings,
			LoadStateSlot8ButtonBindings,
			LoadStateSlot9ButtonBindings,
			LoadStateSlot10ButtonBindings
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
