using System.IO;

using GSR.Emu;
using GSR.Gui;

namespace GSR;

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

	private void SetStateSet(int set)
	{
		config.SaveStateSet = set;
		osdManager.QueueMessage($"Current state  set to {GetStateSlot(config.SaveStateSlot)}");
	}

	public void SetStateSlot(int slot)
	{
		config.SaveStateSlot = slot;
		osdManager.QueueMessage($"Current state slot set to {GetStateSlot(config.SaveStateSlot)}");
	}

	private string CreateStatePath(int slot)
	{
		var stateSlot = config.SaveStateSet * 10 + slot + 1;
		return $"{Path.Combine(emuManager.CurrentRomDirectory, emuManager.CurrentRomName)}_{stateSlot}.gqs";
	}

	public void SaveStateSlot(int slot)
	{
		var statePath = CreateStatePath(slot);
		osdManager.QueueMessage(emuManager.SaveState(statePath)
			? $"State {GetStateSlot(slot)} saved"
			: "Failed to save state!");
	}

	public void LoadStateSlot(int slot)
	{
		var statePath = CreateStatePath(slot);
		osdManager.QueueMessage(emuManager.LoadState(statePath)
			? $"State {GetStateSlot(slot)} loaded"
			: "Failed to load state!");
	}
}
