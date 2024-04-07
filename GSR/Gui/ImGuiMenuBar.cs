// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System.IO;

using ImGuiNET;

using static SDL2.SDL;

using GSR.Emu;

namespace GSR.Gui;

internal sealed class ImGuiMenuBar(Config config, EmuManager emuManager, RomLoader romLoader, StateManager stateManager, ImGuiWindow mainWindow, ImGuiModals imGuiModals)
{
	public void RunMenuBar()
	{
		if (ImGui.BeginMenuBar())
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
						if (ImGui.MenuItem($"{Path.GetFileName(GSRFile.MakeFriendlyPath(config.RecentRoms[i]))}##{i}"))
						{
							romLoader.LoadRomFile(config.RecentRoms[i]);
						}
					}

					ImGui.EndMenu();
				}

				if (ImGui.MenuItem("Close ROM", emuManager.RomIsLoaded))
				{
					emuManager.UnloadRom();
				}
#if !GSR_ANDROID
				ImGui.Separator();

				if (ImGui.MenuItem("Save State as...", emuManager.RomIsLoaded))
				{
					using (new EmuPause(emuManager))
					{
						var filePath = SaveFileDialog.ShowDialog("GSR Quick State", emuManager.CurrentStatePath, emuManager.CurrentRomName, ".gqs", mainWindow);
						if (filePath != null)
						{
							// TODO: OSD message on success/fail
							_ = emuManager.SaveState(filePath);
						}
					}
				}

				if (ImGui.MenuItem("Load State as...", emuManager.RomIsLoaded))
				{
					using (new EmuPause(emuManager))
					{
						var filePath = OpenFileDialog.ShowDialog("GSR Quick State", emuManager.CurrentStatePath, [".gqs"], mainWindow);
						if (filePath != null)
						{
							// TODO: OSD message on success/fail
							_ = emuManager.LoadState(filePath);
						}
					}
				}
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
				}

				if (ImGui.MenuItem("Frame Step"))
				{
					emuManager.DoFrameStep();
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
#if !GSR_ANDROID
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
				if (ImGui.MenuItem("About GSR"))
				{
					imGuiModals.OpenAboutModal = true;
				}

				ImGui.EndMenu();
			}

			ImGui.EndMenuBar();
		}
	}
}
