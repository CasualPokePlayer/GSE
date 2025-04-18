// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System.IO;

using ImGuiNET;

using static SDL2.SDL;

using GSE.Emu;

namespace GSE.Gui;

internal sealed class ImGuiMenuBar(Config config, EmuManager emuManager, RomLoader romLoader, StateManager stateManager, OSDManager osdManager, ImGuiWindow mainWindow, ImGuiModals imGuiModals)
{
	public void RunMenuBar()
	{
		if (ImGui.BeginMainMenuBar())
		{
			if (ImGui.BeginMenu("File"))
			{
				if (ImGui.MenuItem("Open ROM..."))
				{
					using (new EmuPause(emuManager))
					{
						var filePath = OpenFileDialog.ShowDialog("GB/C/A ROM File", null, RomLoader.RomAndCompressionExtensions, mainWindow);
						if (filePath != null)
						{
							romLoader.LoadRomFile(filePath);
						}
					}
				}

				if (ImGui.BeginMenu("Open Recent...", config.RecentRoms.Count > 0))
				{
					// ReSharper disable once ForCanBeConvertedToForeach
					// can't use a foreach loop, as LoadRomFile may mutate _config.RecentRoms
					for (var i = 0; i < config.RecentRoms.Count; i++)
					{
						if (ImGui.MenuItem($"{Path.GetFileName(GSEFile.MakeFriendlyPath(config.RecentRoms[i]))}##{i}"))
						{
							romLoader.LoadRomFile(config.RecentRoms[i]);
						}
					}

					ImGui.EndMenu();
				}

				if (ImGui.MenuItem("Close ROM", emuManager.RomIsLoaded))
				{
					emuManager.UnloadRom();
					osdManager.OnRomUnloaded();
				}
#if !GSE_ANDROID
				ImGui.Separator();

				if (ImGui.MenuItem("Import Save...", emuManager.RomIsLoaded))
				{
					using (new EmuPause(emuManager))
					{
						var filePath = OpenFileDialog.ShowDialog("Save File", emuManager.CurrentSavePath, [".sav"], mainWindow);
						if (filePath != null)
						{
							// note that importing a save does an implicit reset, if successful
							// due to this, we only want to queue an OSD message on failure
							if (!emuManager.LoadSave(filePath))
							{
								osdManager.QueueMessage("Failed to import save!");
							}
						}
					}
				}

				ImGui.Separator();

				if (ImGui.MenuItem("Save State as...", emuManager.RomIsLoaded))
				{
					using (new EmuPause(emuManager))
					{
						var filePath = SaveFileDialog.ShowDialog("GSE Quick State", emuManager.CurrentStatePath, emuManager.CurrentRomName, ".gqs", mainWindow);
						if (filePath != null)
						{
							osdManager.QueueMessage(emuManager.SaveState(filePath)
								? "State saved to user selected path"
								: "Failed to save state!");
						}
					}
				}

				if (ImGui.MenuItem("Load State as...", emuManager.RomIsLoaded))
				{
					using (new EmuPause(emuManager))
					{
						var filePath = OpenFileDialog.ShowDialog("GSE Quick State", emuManager.CurrentStatePath, [".gqs"], mainWindow);
						if (filePath != null)
						{
							osdManager.QueueMessage(emuManager.LoadState(filePath)
								? "State loaded from user selected path"
								: "Failed to load state!");
						}
					}
				}
#else
				_ = osdManager;
#endif

				ImGui.Separator();

				if (ImGui.MenuItem("Save State", emuManager.RomIsLoaded))
				{
					stateManager.SaveStateCurSlot();
				}

				if (ImGui.MenuItem("Load State", emuManager.RomIsLoaded))
				{
					stateManager.LoadStateCurSlot();
				}

				if (ImGui.BeginMenu("Select State Slot...", emuManager.RomIsLoaded))
				{
					if (ImGui.MenuItem("Previous Set"))
					{
						stateManager.DecStateSet();
					}

					if (ImGui.MenuItem("Next Set"))
					{
						stateManager.IncStateSet();
					}

					ImGui.Separator();

					if (ImGui.MenuItem("Previous"))
					{
						stateManager.DecStateSlot();
					}

					if (ImGui.MenuItem("Next"))
					{
						stateManager.IncStateSet();
					}

					ImGui.Separator();

					for (var i = 0; i < 10; i++)
					{
						var slot = config.SaveStateSet * 10 + i + 1;
						if (ImGui.RadioButton($"Slot {slot}", i == config.SaveStateSlot))
						{
							stateManager.SetStateSlot(i);
						}
					}

					ImGui.EndMenu();
				}

				ImGui.Separator();

				if (ImGui.MenuItem("Quit"))
				{
					var e = default(SDL_Event);
					e.type = SDL_EventType.SDL_QUIT;
					SDL_PushEvent(ref e);
				}

				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("Play"))
			{
				if (ImGui.MenuItem("Toggle Pause"))
				{
					emuManager.TogglePause();

					if (config.HideMenuBarOnUnpause && !config.AllowManualResizing)
					{
						mainWindow.UpdateMainWindowSize(emuManager, config);
					}
				}

				if (ImGui.MenuItem("Frame Step"))
				{
					emuManager.DoFrameStep();

					if (config.HideMenuBarOnUnpause && !config.AllowManualResizing)
					{
						mainWindow.UpdateMainWindowSize(emuManager, config);
					}
				}

				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("Settings"))
			{
				if (ImGui.MenuItem("Paths..."))
				{
					imGuiModals.OpenPathModal = true;
				}

				if (ImGui.MenuItem("Input..."))
				{
					imGuiModals.OpenInputModal = true;
				}

				if (ImGui.MenuItem("Video..."))
				{
					imGuiModals.OpenVideoModal = true;
				}

				if (ImGui.MenuItem("Audio..."))
				{
					imGuiModals.OpenAudioModal = true;
				}

				if (ImGui.MenuItem("OSD..."))
				{
					imGuiModals.OpenOsdModal = true;
				}

				if (ImGui.MenuItem("Misc..."))
				{
					imGuiModals.OpenMiscModal = true;
				}
#if !GSE_ANDROID
				ImGui.Separator();

				if (ImGui.MenuItem("Toggle Fullscreen"))
				{
					mainWindow.ToggleFullscreen();
				}
#endif

				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("Help"))
			{
				if (ImGui.MenuItem("About GSE"))
				{
					imGuiModals.OpenAboutModal = true;
				}

				ImGui.EndMenu();
			}

			ImGui.EndMainMenuBar();
		}
	}
}
