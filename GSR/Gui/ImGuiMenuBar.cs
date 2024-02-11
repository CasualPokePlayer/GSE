using System.IO;

using GSR.Emu;

using ImGuiNET;

using static SDL2.SDL;

namespace GSR.Gui;

internal sealed class ImGuiMenuBar(Config config, EmuManager emuManager, RomLoader romLoader, HotkeyManager hotkeyManager, ImGuiWindow mainWindow, ImGuiModals imGuiModals)
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
						var filePath = OpenFileDialog.ShowDialog("GB/C/A ROM File", null, RomLoader.RomAndCompressionExtensions);
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
						if (ImGui.MenuItem(Path.GetFileName(config.RecentRoms[i])))
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

				ImGui.Separator();

				if (ImGui.MenuItem("Save State as...", emuManager.RomIsLoaded))
				{
					using (new EmuPause(emuManager))
					{
						var filePath = SaveFileDialog.ShowDialog("GSR Quick State", emuManager.CurrentRomDirectory, emuManager.CurrentRomName, ".gqs");
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
						var filePath = OpenFileDialog.ShowDialog("GSR Quick State", emuManager.CurrentRomDirectory, [".gqs"]);
						if (filePath != null)
						{
							// TODO: OSD message on success/fail
							_ = emuManager.LoadState(filePath);
						}
					}
				}

				ImGui.Separator();

				if (ImGui.MenuItem("Save State", emuManager.RomIsLoaded))
				{
					hotkeyManager.SaveStateCurSlot();
				}

				if (ImGui.MenuItem("Load State", emuManager.RomIsLoaded))
				{
					hotkeyManager.LoadStateCurSlot();
				}

				if (ImGui.BeginMenu("Select State Slot...", emuManager.RomIsLoaded))
				{
					if (ImGui.MenuItem("Previous Set"))
					{
						hotkeyManager.DecStateSet();
					}

					if (ImGui.MenuItem("Next Set"))
					{
						hotkeyManager.IncStateSet();
					}

					ImGui.Separator();

					if (ImGui.MenuItem("Previous"))
					{
						hotkeyManager.DecStateSlot();
					}

					if (ImGui.MenuItem("Next"))
					{
						hotkeyManager.IncStateSet();
					}

					ImGui.Separator();

					for (var i = 0; i < 10; i++)
					{
						var slot = config.SaveStateSet * 10 + i;
						if (ImGui.RadioButton($"Slot {slot}", i == config.SaveStateSlot))
						{
							hotkeyManager.SetStateSlot(i);
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

				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("Settings"))
			{
				if (ImGui.MenuItem("BIOS Paths..."))
				{
					imGuiModals.OpenBiosPathModal = true;
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

				if (ImGui.MenuItem("Misc..."))
				{
					imGuiModals.OpenMiscModal = true;
				}

				ImGui.Separator();

				if (ImGui.MenuItem("Toggle Fullscreen"))
				{
					mainWindow.ToggleFullscreen();
				}

				ImGui.EndMenu();
			}

			ImGui.EndMenuBar();
		}
	}
}
