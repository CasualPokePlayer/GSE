// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System.IO;

using GSE.Emu;
using GSE.Gui;

namespace GSE;

internal sealed class StateManager(Config config, EmuManager emuManager, OSDManager osdManager)
{
	public void SaveStateCurSlot()
	{
		SaveStateSlot(config.SaveStateSlot);
	}

	public void LoadStateCurSlot()
	{
		LoadStateSlot(config.SaveStateSlot);
	}

	public void DecStateSet()
	{
		SetStateSet(config.SaveStateSet == 0 ? 9 : config.SaveStateSet - 1);
	}

	public void IncStateSet()
	{
		SetStateSet(config.SaveStateSet == 9 ? 0 : config.SaveStateSet + 1);
	}

	public void DecStateSlot()
	{
		SetStateSlot(config.SaveStateSlot == 0 ? 9 : config.SaveStateSlot - 1);
	}

	public void IncStateSlot()
	{
		SetStateSlot(config.SaveStateSlot == 9 ? 0 : config.SaveStateSlot + 1);
	}

	private int GetStateSlot(int slot)
	{
		return config.SaveStateSet * 10 + slot + 1;
	}

	private void LoadStatePreview()
	{
		var statePreview = emuManager.LoadStatePreview(CreateStatePath(config.SaveStateSlot));
		if (!statePreview.VideoBuffer.IsEmpty)
		{
			osdManager.SetStatePreview(statePreview, config.SaveStateSlot);
		}
		else
		{
			osdManager.ClearStatePreview();
		}
	}

	private void OnStateSlotChanged()
	{
		osdManager.QueueMessage($"Current state slot set to {GetStateSlot(config.SaveStateSlot)}");
		if (!config.HideStatePreviews && !emuManager.RomIsLoaded)
		{
			LoadStatePreview();
		}
	}

	private void SetStateSet(int set)
	{
		config.SaveStateSet = set;
		OnStateSlotChanged();
	}

	public void SetStateSlot(int slot)
	{
		config.SaveStateSlot = slot;
		OnStateSlotChanged();
	}

	private string CreateStatePath(int slot)
	{
		return $"{Path.Combine(emuManager.CurrentStatePath, emuManager.CurrentRomName)}_{GetStateSlot(slot)}.gqs";
	}

	public void SaveStateSlot(int slot)
	{
		if (!emuManager.RomIsLoaded)
		{
			return;
		}

		var statePath = CreateStatePath(slot);
		osdManager.QueueMessage(emuManager.SaveState(statePath)
			? $"State {GetStateSlot(slot)} saved"
			: "Failed to save state!");

		if (slot == config.SaveStateSlot)
		{
			// if we had a state preview active, we'll want to update it with the new savestate
			// this will also reset the preview timer, but that's fine (only at most ~3 extra seconds)
			if (osdManager.StatePreviewActive)
			{
				LoadStatePreview();
			}
		}
	}

	public void LoadStateSlot(int slot)
	{
		if (!emuManager.RomIsLoaded)
		{
			return;
		}

		var statePath = CreateStatePath(slot);
		osdManager.QueueMessage(emuManager.LoadState(statePath)
			? $"State {GetStateSlot(slot)} loaded"
			: "Failed to load state!");
	}
}
