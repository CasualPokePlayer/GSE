// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
#if !GSE_ANDROID
using System.IO;
#endif
using System.Linq;

using ImGuiNET;

using static SDL2.SDL;

#if GSE_ANDROID
using GSE.Android;
#endif
using GSE.Audio;
using GSE.Emu;
using GSE.Input;

namespace GSE.Gui;

internal sealed class ImGuiModals
{
	private const string PATH_SETTINGS = "Path Settings";
	private const string INPUT_SETTINGS = "Input Settings";
	private const string VIDEO_SETTINGS = "Video Settings";
	private const string AUDIO_SETTINGS = "Audio Settings";
	private const string OSD_SETTINGS = "OSD Settings";
	private const string MISC_SETTINGS = "Misc Settings";
	private const string ABOUT = "About";

	private readonly Config _config;
	private readonly EmuManager _emuManager;
	private readonly InputManager _inputManager;
	private readonly AudioManager _audioManager;
	private readonly HotkeyManager _hotkeyManager;
	private readonly OSDManager _osdManager;
	private readonly ImGuiWindow _mainWindow;

	private static readonly string[] _pathLocationOptions =
	[
		"Same as ROM file",
		"Pref Path",
		"Custom Path" // may be overwritten
	];

	private readonly record struct InputConfig(string InputName, List<InputBinding> InputBindings);

	private readonly InputConfig[] _gameInputConfigs;
	private readonly InputConfig[] _playInputConfigs;
	private readonly InputConfig[] _stateInputConfigs;

	private List<InputBinding> _currentInputBindingList;
	private InputBinding _currentInputBinding = new(null, null, null);
	private bool _startingInputBinding;

	private static readonly Lazy<string[]> _lazyRenderDriverOptions = new(() =>
	{
		var renderDrivers = ImGuiWindow.RenderDrivers.Value;
		var ret = new string[renderDrivers.Length + 1];
		ret[0] = ImGuiWindow.DEFAULT_RENDER_DRIVER;
		for (var i = 0; i < renderDrivers.Length; i++)
		{
			var driverName = renderDrivers[i];
			ret[i + 1] = ImGuiWindow.RenderDriverFriendlyNameMap.GetValueOrDefault(driverName, driverName);
		}

		return ret;
	});

	private static readonly Lazy<ImmutableArray<string>> _lazyRenderDriverConfigStrings = new(() =>
	{
		var renderDrivers = ImGuiWindow.RenderDrivers.Value;
		var ret = new string[renderDrivers.Length + 1];
		ret[0] = ImGuiWindow.DEFAULT_RENDER_DRIVER;
		for (var i = 0; i < renderDrivers.Length; i++)
		{
			ret[i + 1] = renderDrivers[i];
		}

		return [.. ret];
	});

	private static readonly string[] _filterOptions = [ "Nearest Neighbor", "Bilinear", "Sharp Bilinear" ];
	private static readonly string[] _windowScalingOptions = [ "1x", "2x", "3x", "4x", "5x", "6x", "7x", "8x", "9x", "10x", "11x", "12x", "13x", "14x", "15x" ];

	private readonly string[] _renderDriverOptions;
	private readonly ImmutableArray<string> _renderDriverConfigStrings;
	private int _renderDriverIndex;

	private string[] _audioDevices;
	private int _audioDeviceIndex;
	public bool AudioDeviceListChanged;

	private static readonly string[] _gbPlatformOptions = [ "Game Boy", "Game Boy Color", "Game Boy Advance", "Game Boy Player", "Super Game Boy 2" ];

	public bool ModalIsOpened => _pathModalOpened || _inputModalOpened || _videoModalOpened || _audioModalOpened || _osdModalOpened || _miscModalOpened || _aboutModalOpened;
	private bool _pathModalOpened;
	private bool _inputModalOpened;
	private bool _videoModalOpened;
	private bool _audioModalOpened;
	private bool _osdModalOpened;
	private bool _miscModalOpened;
	private bool _aboutModalOpened;

	public bool OpenPathModal;
	public bool OpenInputModal;
	public bool OpenVideoModal;
	public bool OpenAudioModal;
	public bool OpenOsdModal;
	public bool OpenMiscModal;
	public bool OpenAboutModal;

	private bool _didPause;

	public ImGuiModals(Config config, EmuManager emuManager, InputManager inputManager, AudioManager audioManager, HotkeyManager hotkeyManager, OSDManager osdManager, ImGuiWindow mainWindow)
	{
		_config = config;
		_emuManager = emuManager;
		_inputManager = inputManager;
		_audioManager = audioManager;
		_hotkeyManager = hotkeyManager;
		_osdManager = osdManager;
		_mainWindow = mainWindow;

		_gameInputConfigs =
		[
			new("A", _config.EmuControllerBindings.AButtonBindings),
			new("B", _config.EmuControllerBindings.BButtonBindings),
			new("Select", _config.EmuControllerBindings.SelectButtonBindings),
			new("Start", _config.EmuControllerBindings.StartButtonBindings),
			new("Right", _config.EmuControllerBindings.RightButtonBindings),
			new("Left", _config.EmuControllerBindings.LeftButtonBindings),
			new("Up", _config.EmuControllerBindings.UpButtonBindings),
			new("Down", _config.EmuControllerBindings.DownButtonBindings),
			new("R", _config.EmuControllerBindings.RButtonBindings),
			new("L", _config.EmuControllerBindings.LButtonBindings),
			new("Hard Reset", _config.EmuControllerBindings.HardResetButtonBindings),
		];

		_playInputConfigs =
		[
			new("Pause", _config.HotkeyBindings.PauseButtonBindings),
			new("Frame Step", _config.HotkeyBindings.FrameStepButtonBindings),
			new("Fast Forward", _config.HotkeyBindings.FastForwardButtonBindings),
			new("Fullscreen", _config.HotkeyBindings.FullScreenButtonBindings),
			new("Volume Up", _config.HotkeyBindings.VolumeUpButtonBindings),
			new("Volume Down", _config.HotkeyBindings.VolumeDownButtonBindings),
			new("Volume Up by 10", _config.HotkeyBindings.VolumeUp10ButtonBindings),
			new("Volume Down by 10", _config.HotkeyBindings.VolumeDown10ButtonBindings),
		];

		_stateInputConfigs =
		[
			new("Save State", _config.HotkeyBindings.SaveStateButtonBindings),
			new("Load State", _config.HotkeyBindings.LoadStateButtonBindings),
			new("Prev State Set", _config.HotkeyBindings.PrevStateSetButtonBindings),
			new("Next State Set", _config.HotkeyBindings.NextStateSetButtonBindings),
			new("Prev State Slot", _config.HotkeyBindings.PrevStateSlotButtonBindings),
			new("Next State Slot", _config.HotkeyBindings.NextStateSlotButtonBindings),
			new("Select State Slot 1", _config.HotkeyBindings.SelectStateSlot1ButtonBindings),
			new("Select State Slot 2", _config.HotkeyBindings.SelectStateSlot2ButtonBindings),
			new("Select State Slot 3", _config.HotkeyBindings.SelectStateSlot3ButtonBindings),
			new("Select State Slot 4", _config.HotkeyBindings.SelectStateSlot4ButtonBindings),
			new("Select State Slot 5", _config.HotkeyBindings.SelectStateSlot5ButtonBindings),
			new("Select State Slot 6", _config.HotkeyBindings.SelectStateSlot6ButtonBindings),
			new("Select State Slot 7", _config.HotkeyBindings.SelectStateSlot7ButtonBindings),
			new("Select State Slot 8", _config.HotkeyBindings.SelectStateSlot8ButtonBindings),
			new("Select State Slot 9", _config.HotkeyBindings.SelectStateSlot9ButtonBindings),
			new("Select State Slot 10", _config.HotkeyBindings.SelectStateSlot10ButtonBindings),
			new("Save State Slot 1", _config.HotkeyBindings.SaveStateSlot1ButtonBindings),
			new("Save State Slot 2", _config.HotkeyBindings.SaveStateSlot2ButtonBindings),
			new("Save State Slot 3", _config.HotkeyBindings.SaveStateSlot3ButtonBindings),
			new("Save State Slot 4", _config.HotkeyBindings.SaveStateSlot4ButtonBindings),
			new("Save State Slot 5", _config.HotkeyBindings.SaveStateSlot5ButtonBindings),
			new("Save State Slot 6", _config.HotkeyBindings.SaveStateSlot6ButtonBindings),
			new("Save State Slot 7", _config.HotkeyBindings.SaveStateSlot7ButtonBindings),
			new("Save State Slot 8", _config.HotkeyBindings.SaveStateSlot8ButtonBindings),
			new("Save State Slot 9", _config.HotkeyBindings.SaveStateSlot9ButtonBindings),
			new("Save State Slot 10", _config.HotkeyBindings.SaveStateSlot10ButtonBindings),
			new("Load State Slot 1", _config.HotkeyBindings.LoadStateSlot1ButtonBindings),
			new("Load State Slot 2", _config.HotkeyBindings.LoadStateSlot2ButtonBindings),
			new("Load State Slot 3", _config.HotkeyBindings.LoadStateSlot3ButtonBindings),
			new("Load State Slot 4", _config.HotkeyBindings.LoadStateSlot4ButtonBindings),
			new("Load State Slot 5", _config.HotkeyBindings.LoadStateSlot5ButtonBindings),
			new("Load State Slot 6", _config.HotkeyBindings.LoadStateSlot6ButtonBindings),
			new("Load State Slot 7", _config.HotkeyBindings.LoadStateSlot7ButtonBindings),
			new("Load State Slot 8", _config.HotkeyBindings.LoadStateSlot8ButtonBindings),
			new("Load State Slot 9", _config.HotkeyBindings.LoadStateSlot9ButtonBindings),
			new("Load State Slot 10", _config.HotkeyBindings.LoadStateSlot10ButtonBindings),
		];

		foreach (var inputConfig in _gameInputConfigs.Concat(_playInputConfigs).Concat(_stateInputConfigs))
		{
			// remove all overlapping inputs, leaving the latest inputs in their place
			for (var i = 0; i < inputConfig.InputBindings.Count; i++)
			{
				for (var j = i + 1; j < inputConfig.InputBindings.Count; j++)
				{
					if (InputsOverlap(inputConfig.InputBindings[^(i + 1)], inputConfig.InputBindings[^(j + 1)]))
					{
						inputConfig.InputBindings.RemoveAt(inputConfig.InputBindings.Count - (j + 1));
						j--;
					}
				}
			}

			// we limit bindings to only 4 max
			if (inputConfig.InputBindings.Count > 4)
			{
				inputConfig.InputBindings.RemoveRange(0, inputConfig.InputBindings.Count - 4);
			}
		}

		foreach (var gameInputConfig in _gameInputConfigs)
		{
			RemoveMatchingGameInputs(gameInputConfig.InputBindings);
		}

		_renderDriverOptions = _lazyRenderDriverOptions.Value;
		_renderDriverConfigStrings = _lazyRenderDriverConfigStrings.Value;

		for (var i = 0; i < _renderDriverConfigStrings.Length; i++)
		{
			if (_renderDriverConfigStrings[i] == _config.RenderDriver)
			{
				_renderDriverIndex = i;
			}
		}

		_mainWindow.SetAlwaysOnTop(_config.AlwaysOnTop);
		_mainWindow.SetResizable(_config.AllowManualResizing);
		_mainWindow.UpdateMainWindowSize(_emuManager, _config);

		EnumerateAudioDevices();
	}

	private void CheckModalNeedsOpen(string id, ref bool needOpen, ref bool opened)
	{
		if (needOpen)
		{
			ImGui.OpenPopup(id);
			needOpen = false;
			opened = true;
			_didPause = _emuManager.Pause();
			if (_didPause && _config.HideMenuBarOnUnpause && !_config.AllowManualResizing)
			{
				_mainWindow.UpdateMainWindowSize(_emuManager, _config);
			}
		}
	}

	private void CheckModalWasClosed(bool open, ref bool wasOpened)
	{
		if (!open && wasOpened)
		{
			wasOpened = false;
			if (_didPause)
			{
				_emuManager.Unpause();
				if (_config.HideMenuBarOnUnpause && !_config.AllowManualResizing)
				{
					_mainWindow.UpdateMainWindowSize(_emuManager, _config);
				}
			}
		}
	}

	// ReSharper disable once SuggestBaseTypeForParameter
	private void DoInputTab(InputConfig[] inputConfigs, float textSpacing)
	{
		// create input labels for all input configs
		// we need to do this now, as we need to know the minimum size for a button
		// there's probably a better way to do this, but this works fine
		var inputLabels = new string[inputConfigs.Length];
		var buttonWidth = ImGui.CalcTextSize("Clear").X + ImGui.GetStyle().FramePadding.X * 2;
		for (var i = 0; i < inputLabels.Length; i++)
		{
			inputLabels[i] = string.Join(',',
				inputConfigs[i].InputBindings.Select(b => b.ModifierLabel != null ? $"{b.ModifierLabel}+{b.MainInputLabel}" : b.MainInputLabel));
			var labelSize = ImGui.CalcTextSize(inputLabels[i]).X + ImGui.GetStyle().FramePadding.X * 2;
			buttonWidth = Math.Max(buttonWidth, labelSize);
		}

		for (var i = 0; i < inputConfigs.Length; i++)
		{
			ImGui.AlignTextToFramePadding();
			ImGui.TextUnformatted(inputConfigs[i].InputName);
			ImGui.SameLine(ImGui.GetFontSize() * textSpacing);

			if (ImGui.Button($"{inputLabels[i]}##{inputConfigs[i].InputName}", new(buttonWidth, 0)))
			{
				_currentInputBindingList = inputConfigs[i].InputBindings;
				_inputManager.BeginInputBinding();
				_startingInputBinding = true;
				_mainWindow.SuppressEscape = true;
			}

			ImGui.SameLine();
			if (ImGui.Button($"Clear##{inputConfigs[i].InputName}"))
			{
				inputConfigs[i].InputBindings.Clear();
			}
		}
	}

	private void StopInputBinding()
	{
		_inputManager.EndInputBinding();
		_currentInputBindingList = null;
		_currentInputBinding = new(null, null, null);
		_mainWindow.SuppressEscape = false;
	}

	private static bool InputsOverlap(InputBinding b1, InputBinding b2)
	{
		return b1.MainInputLabel == b2.MainInputLabel
		       || b1.MainInputLabel == b2.ModifierLabel
		       || b1.ModifierLabel == b2.MainInputLabel
		       || (b1.ModifierLabel is not null && b1.ModifierLabel == b2.ModifierLabel);
	}

	public void RemoveMatchingGameInputs(List<InputBinding> inputBindings)
	{
		foreach (var inputBinding in inputBindings)
		{
			foreach (var gameInputConfig in _gameInputConfigs)
			{
				if (gameInputConfig.InputBindings != inputBindings)
				{
					gameInputConfig.InputBindings.RemoveAll(b => InputsOverlap(b, inputBinding));
				}
			}
		}
	}

	public void EnumerateAudioDevices()
	{
		_config.AudioDeviceName = _audioManager.AudioDeviceName;
		_audioDevices = AudioManager.EnumerateAudioDevices();
		_audioDeviceIndex = 0;
		for (var i = 0; i < _audioDevices.Length; i++)
		{
			if (_audioDevices[i] == _config.AudioDeviceName)
			{
				_audioDeviceIndex = i;
			}
		}
	}

	public void RunModals()
	{
		CheckModalNeedsOpen(PATH_SETTINGS, ref OpenPathModal, ref _pathModalOpened);
		CheckModalNeedsOpen(INPUT_SETTINGS, ref OpenInputModal, ref _inputModalOpened);
		CheckModalNeedsOpen(VIDEO_SETTINGS, ref OpenVideoModal, ref _videoModalOpened);
		CheckModalNeedsOpen(AUDIO_SETTINGS, ref OpenAudioModal, ref _audioModalOpened);
		CheckModalNeedsOpen(OSD_SETTINGS, ref OpenOsdModal, ref _osdModalOpened);
		CheckModalNeedsOpen(MISC_SETTINGS, ref OpenMiscModal, ref _miscModalOpened);
		CheckModalNeedsOpen(ABOUT, ref OpenAboutModal, ref _aboutModalOpened);

		var center = ImGui.GetMainViewport().GetCenter();
		ImGui.SetNextWindowPos(center, ImGuiCond.Always, new(.5f, .5f));

		var pathOpen = true;
		if (ImGui.BeginPopupModal(PATH_SETTINGS, ref pathOpen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
		{
			static string AddBiosPathButton(string system, string biosPathConfig, ImGuiWindow mainWindow)
			{
				ImGui.AlignTextToFramePadding();
				ImGui.TextUnformatted($"{system} BIOS:");

				ImGui.SameLine(ImGui.GetFontSize() * 5.5f);
				if (ImGui.Button($"{GSEFile.MakeFriendlyPath(biosPathConfig) ?? "Path not set..."}##{system}"))
				{
					var biosPath = OpenFileDialog.ShowDialog($"{system} BIOS File", null, RomLoader.BiosAndCompressionExtensions, mainWindow);
					if (biosPath != null)
					{
						biosPathConfig = biosPath;
					}
				}

				return biosPathConfig;
			}

			_config.GbBiosPath = AddBiosPathButton("GB", _config.GbBiosPath, _mainWindow);
			_config.GbcBiosPath = AddBiosPathButton("GBC", _config.GbcBiosPath, _mainWindow);
			_config.Sgb2BiosPath = AddBiosPathButton("SGB2", _config.Sgb2BiosPath, _mainWindow);
			_config.GbaBiosPath = AddBiosPathButton("GBA", _config.GbaBiosPath, _mainWindow);

#if !GSE_ANDROID
			ImGui.Separator();

			static (PathResolver.PathType, string) AddPathLocationButton(string label, PathResolver.PathType pathType, string customPath, ImGuiWindow mainWindow)
			{
				if (pathType == PathResolver.PathType.Custom)
				{
					_pathLocationOptions[(int)PathResolver.PathType.Custom] = customPath;
				}
				else
				{
					_pathLocationOptions[(int)PathResolver.PathType.Custom] = "Custom Path";
				}

				_pathLocationOptions[(int)PathResolver.PathType.PrefPath] =
					Path.Combine(PathResolver.GetPath(PathResolver.PathType.PrefPath, null, null, null), label);

				var pathTypeIndex = (int)pathType;
				if (ImGui.Combo($"{label} Path", ref pathTypeIndex, _pathLocationOptions, _pathLocationOptions.Length))
				{
					if ((PathResolver.PathType)pathTypeIndex == PathResolver.PathType.Custom)
					{
						customPath = SelectFolderDialog.ShowDialog(label, null, mainWindow);
						// revert back to previous selection if the dialog was cancelled,
						if (customPath == null)
						{
							pathTypeIndex = (int)pathType;
						}
					}
					else
					{
						customPath = null;
					}

					pathType = (PathResolver.PathType)pathTypeIndex;
				}

				return (pathType, customPath);
			}

			(_config.SavePathLocation, _config.SavePathCustom) = AddPathLocationButton("Save", _config.SavePathLocation, _config.SavePathCustom, _mainWindow);
			(_config.StatePathLocation, _config.StatePathCustom) = AddPathLocationButton("State", _config.StatePathLocation, _config.StatePathCustom, _mainWindow);
#endif
			ImGui.Separator();
			ImGui.AlignTextToFramePadding();

			if (ImGui.Button("Open User Folder"))
			{
#if GSE_ANDROID
				AndroidFile.OpenFileManager();
#else
				var prefPath = PathResolver.GetPath(PathResolver.PathType.PrefPath, null, null, null);
#if GSE_OSX
				_ = SDL_OpenURL(new Uri(prefPath).AbsoluteUri);
#else
				try
				{
					Process.Start(new ProcessStartInfo(prefPath) { UseShellExecute = true });
				}
				catch
				{
					// ignored
				}
#endif
#endif
			}

			ImGui.EndPopup();
		}

		ImGui.SetNextWindowPos(center, ImGuiCond.Always, new(.5f, .5f));

		var inputOpen = true;
		if (ImGui.BeginPopupModal(INPUT_SETTINGS, ref inputOpen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
		{
			if (ImGui.BeginTabBar("Input Settings Tabs"))
			{
				if (ImGui.BeginTabItem("Game"))
				{
					DoInputTab(_gameInputConfigs, 5.5f);
					ImGui.EndTabItem();
				}

				if (ImGui.BeginTabItem("Play"))
				{
					DoInputTab(_playInputConfigs, 9.5f);
					ImGui.EndTabItem();
				}

				if (ImGui.BeginTabItem("State"))
				{
					DoInputTab(_stateInputConfigs, 11.5f);
					ImGui.EndTabItem();
				}

#if !GSE_ANDROID
				if (ImGui.BeginTabItem("Misc"))
				{
					var bkgInput = _config.AllowBackgroundInput;
					if (ImGui.Checkbox("Allow Background Input", ref bkgInput))
					{
						_config.AllowBackgroundInput = bkgInput;
					}

					// we only allow changing background input for joysticks only if allow background input is set
					if (!_config.AllowBackgroundInput)
					{
						ImGui.BeginDisabled();
					}

					var bkgInputForJs = _config.BackgroundInputForJoysticksOnly;
					if (ImGui.Checkbox("For Joysticks Only", ref bkgInputForJs))
					{
						_config.BackgroundInputForJoysticksOnly = bkgInputForJs;
					}

					if (!_config.AllowBackgroundInput)
					{
						ImGui.EndDisabled();
					}

#if GSE_WINDOWS
					ImGui.Separator();

					var enableDirectInput = _config.EnableDirectInput;
					if (ImGui.Checkbox("Enable DirectInput", ref enableDirectInput))
					{
						_config.EnableDirectInput = enableDirectInput;
						_inputManager.SetDirectInputEnable(enableDirectInput);
					}
#endif

					ImGui.EndTabItem();
				}
#endif

				ImGui.EndTabBar();
			}

			if (_currentInputBindingList != null)
			{
				if (_startingInputBinding)
				{
					ImGui.OpenPopup("Input Binding");
					_startingInputBinding = false;
				}

				if (ImGui.BeginPopup("Input Binding", ImGuiWindowFlags.NoNavInputs))
				{
					ImGui.TextUnformatted("Press a keyboard or joystick input to bind this input.");
					ImGui.TextUnformatted("Hold an input then press another to do a modifier input pair.");

					// otherwise, we can update our input binding
					if (_inputManager.UpdateInputBinding(ref _currentInputBinding))
					{
						_currentInputBindingList.RemoveAll(b => InputsOverlap(b, _currentInputBinding));
						_currentInputBindingList.Add(_currentInputBinding);

						// we limit bindings to only 4 max
						if (_currentInputBindingList.Count > 4)
						{
							_currentInputBindingList.RemoveRange(0, _currentInputBindingList.Count - 4);
						}

						// we don't allow game inputs to match against other game inputs
						if (_gameInputConfigs.Any(gi => gi.InputBindings == _currentInputBindingList))
						{
							RemoveMatchingGameInputs(_currentInputBindingList);
						}

						StopInputBinding();
						ImGui.CloseCurrentPopup();

						_hotkeyManager.InputBindingsChanged = true;
					}

					ImGui.EndPopup();
				}
				else
				{
					StopInputBinding();
				}
			}

			ImGui.EndPopup();
		}

		ImGui.SetNextWindowPos(center, ImGuiCond.Always, new(.5f, .5f));

		var videoOpen = true;
		if (ImGui.BeginPopupModal(VIDEO_SETTINGS, ref videoOpen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
		{
			if (ImGui.Combo("Render Driver", ref _renderDriverIndex, _renderDriverOptions, _renderDriverOptions.Length))
			{
				_config.RenderDriver = _renderDriverConfigStrings[_renderDriverIndex];
			}

			ImGui.Separator();

#if !GSE_ANDROID
			var windowsAlwaysOnTop = _config.AlwaysOnTop;
			if (ImGui.Checkbox("Window Always On Top", ref windowsAlwaysOnTop))
			{
				_config.AlwaysOnTop = windowsAlwaysOnTop;
				_mainWindow.SetAlwaysOnTop(windowsAlwaysOnTop);
			}

			var allowManualResizing = _config.AllowManualResizing;
			if (ImGui.Checkbox("Allow Manual Resizing", ref allowManualResizing))
			{
				_config.AllowManualResizing = allowManualResizing;
				_mainWindow.SetResizable(allowManualResizing);
				if (!allowManualResizing)
				{
					_mainWindow.UpdateMainWindowSize(_emuManager, _config);
				}
			}

			if (SDL_GetCurrentDisplayMode(0, out var displayMode) != 0)
			{
				throw new($"Failed to get display mode for window, SDL error: {SDL_GetError()}");
			}

			var (emuWidth, emuHeight) = _emuManager.GetVideoDimensions(_config.HideSgbBorder);
			var maxWindowScale = Math.Min(displayMode.w / emuWidth, displayMode.h / emuHeight);
			maxWindowScale = Math.Clamp(maxWindowScale, 1, _windowScalingOptions.Length);

			if (!_config.AllowManualResizing && maxWindowScale < _config.WindowScale)
			{
				_config.WindowScale = maxWindowScale;
				_mainWindow.UpdateMainWindowSize(_emuManager, _config);
			}

			// note that Combo items are 0 indexed, while window scale is 1 indexed
			var windowScale = _config.WindowScale - 1;
			if (ImGui.Combo("Window Scale", ref windowScale, _windowScalingOptions, maxWindowScale))
			{
				_config.WindowScale = windowScale + 1;
				_mainWindow.UpdateMainWindowSize(_emuManager, _config);
			}
#endif

			var keepAspectRatio = _config.KeepAspectRatio;
			if (ImGui.Checkbox("Keep Aspect Ratio", ref keepAspectRatio))
			{
				_config.KeepAspectRatio = keepAspectRatio;
			}

			var outputFilterIndex = (int)_config.OutputFilter;
			if (ImGui.Combo("Output Filter", ref outputFilterIndex, _filterOptions, _filterOptions.Length))
			{
				_config.OutputFilter = (ScalingFilter)outputFilterIndex;
			}

			ImGui.EndPopup();
		}

		ImGui.SetNextWindowPos(center, ImGuiCond.Always, new(.5f, .5f));

		if (AudioDeviceListChanged)
		{
			EnumerateAudioDevices();
		}

		var audioOpen = true;
		if (ImGui.BeginPopupModal(AUDIO_SETTINGS, ref audioOpen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
		{
			if (ImGui.Combo("Audio Device", ref _audioDeviceIndex, _audioDevices, _audioDevices.Length))
			{
				_config.AudioDeviceName = _audioDevices[_audioDeviceIndex];
				try
				{
					_audioManager.SetAudioDevice(_config.AudioDeviceName);
				}
				catch
				{
					// ChangeConfig generally should not throw
					// However, Android audio seems to be buggy and will be completely wreaked if opening a device id it doesn't like
					// So much to the point that proceeding to fallback on the default audio device will fail
					// Make sure to force the config back to the default audio device to avoid opening the app causing an instant crash
					if (_audioManager.AudioDeviceName == AudioManager.DEFAULT_AUDIO_DEVICE)
					{
						_config.AudioDeviceName = AudioManager.DEFAULT_AUDIO_DEVICE;
					}

					throw;
				}

				// check if we ended up falling back to the default audio device (config change failed?), and adjust the config accordingly
				// TODO: probably want to popup a message box in this case?
				if (_audioDeviceIndex > 0 && _audioManager.AudioDeviceName == AudioManager.DEFAULT_AUDIO_DEVICE)
				{
					_config.AudioDeviceName = AudioManager.DEFAULT_AUDIO_DEVICE;
					_audioDeviceIndex = 0;
				}
			}

			ImGui.Separator();

			var latencyMs = _config.LatencyMs;
			if (ImGui.SliderInt("Latency Ms", ref latencyMs, AudioManager.MINIMUM_LATENCY_MS, AudioManager.MAXIMUM_LATENCY_MS))
			{
				_config.LatencyMs = latencyMs;
				_audioManager.SetLatency(latencyMs);
			}

			var volume = _config.Volume;
			if (ImGui.SliderInt("Volume", ref volume, 0, 100))
			{
				_config.Volume = volume;
				_audioManager.SetVolume(volume);
			}

			ImGui.EndPopup();
		}

		ImGui.SetNextWindowPos(center, ImGuiCond.Always, new(.5f, .5f));

		var osdOpen = true;
		if (ImGui.BeginPopupModal(OSD_SETTINGS, ref osdOpen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
		{
			var hideStatusBar = _config.HideStatusBar;
			if (ImGui.Checkbox("Hide Status Bar", ref hideStatusBar))
			{
				_config.HideStatusBar = hideStatusBar;
				if (!_config.AllowManualResizing)
				{
					_mainWindow.UpdateMainWindowSize(_emuManager, _config);
				}
			}

			var hideMenuBarOnUnpause = _config.HideMenuBarOnUnpause;
			if (ImGui.Checkbox("Hide Menu Bar On Unpause", ref hideMenuBarOnUnpause))
			{
				_config.HideMenuBarOnUnpause = hideMenuBarOnUnpause;
				if (!_config.AllowManualResizing)
				{
					_mainWindow.UpdateMainWindowSize(_emuManager, _config);
				}
			}

			var restrictOsdOverlayToGameArea = _config.RestrictOsdOverlayToGameArea;
			if (ImGui.Checkbox("Restrict OSD Overlay To Game Area", ref restrictOsdOverlayToGameArea))
			{
				_config.RestrictOsdOverlayToGameArea = restrictOsdOverlayToGameArea;
			}

			ImGui.Separator();

			var hideStatePreviews = _config.HideStatePreviews;
			if (ImGui.Checkbox("Hide State Previews", ref hideStatePreviews))
			{
				_config.HideStatePreviews = hideStatePreviews;
				if (_config.HideStatePreviews)
				{
					_osdManager.ClearStatePreview();
				}
			}

			var statePreviewOpacity = _config.StatePreviewOpacity;
			if (ImGui.SliderInt("State Preview Opacity", ref statePreviewOpacity, 25, 100))
			{
				_config.StatePreviewOpacity = statePreviewOpacity;
			}

			var statePreviewScale = _config.StatePreviewScale;
			if (ImGui.SliderInt("State Preview Scale", ref statePreviewScale, 10, 50))
			{
				_config.StatePreviewScale = statePreviewScale;
			}

			ImGui.EndPopup();
		}

		ImGui.SetNextWindowPos(center, ImGuiCond.Always, new(.5f, .5f));

		var miscOpen = true;
		if (ImGui.BeginPopupModal(MISC_SETTINGS, ref miscOpen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
		{
			var gbPlatform = (int)_config.GbPlatform;
			if (ImGui.Combo("GB Platform", ref gbPlatform, _gbPlatformOptions, _gbPlatformOptions.Length))
			{
				_config.GbPlatform = (GBPlatform)gbPlatform;
			}

			var fastForwardSpeed = _config.FastForwardSpeed;
			if (ImGui.SliderInt("Fast Forward Speed", ref fastForwardSpeed, 2, 64))
			{
				_config.FastForwardSpeed = fastForwardSpeed;
			}

			var applyColorCorrection = _config.ApplyColorCorrection;
			if (ImGui.Checkbox("Apply Color Correction", ref applyColorCorrection))
			{
				_config.ApplyColorCorrection = applyColorCorrection;
				_emuManager.SetColorCorrectionEnable(applyColorCorrection);
			}

			var disableGbaRtc = _config.DisableGbaRtc;
			if (ImGui.Checkbox("Disable GBA RTC", ref disableGbaRtc))
			{
				_config.DisableGbaRtc = disableGbaRtc;
			}

			var hideSgbBorder = _config.HideSgbBorder;
			if (ImGui.Checkbox("Hide SGB Border", ref hideSgbBorder))
			{
				_config.HideSgbBorder = hideSgbBorder;
				if (!_config.AllowManualResizing)
				{
					_mainWindow.UpdateMainWindowSize(_emuManager, _config);
				}
			}

			var darkMode = _config.DarkMode;
			if (ImGui.Checkbox("Dark Mode", ref darkMode))
			{
				_config.DarkMode = darkMode;
				_mainWindow.SetTheme(darkMode);
			}

#if GSE_WINDOWS
			if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
			{
				var disableWin11RoundCorners = _config.DisableWin11RoundCorners;
				if (ImGui.Checkbox("Disable Windows 11 Round Corners", ref disableWin11RoundCorners))
				{
					_config.DisableWin11RoundCorners = disableWin11RoundCorners;
					_mainWindow.SetWin11CornerPreference(disableWin11RoundCorners);
				}
			}
#endif

#if !GSE_ANDROID
			var enableDiscordRichPresence = _config.EnableDiscordRichPresence;
			if (ImGui.Checkbox("Enable Discord Rich Presence", ref enableDiscordRichPresence))
			{
				_config.EnableDiscordRichPresence = enableDiscordRichPresence;
				_osdManager.ResetDiscordRichPresence();
			}
#endif

			ImGui.EndPopup();
		}

		ImGui.SetNextWindowPos(center, ImGuiCond.Always, new(.5f, .5f));

		var aboutOpen = true;
		if (ImGui.BeginPopupModal(ABOUT, ref aboutOpen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
		{
			ImGui.TextUnformatted($"GSE v{GSEVersion.FullSemVer}");
			ImGui.Separator();

			ImGui.TextWrapped("GSE comprises of original work and many third-party libraries, each with their own respective license. In aggregate, GSE is licensable under the terms of the GPL-2.0 license.");
			ImGui.Separator();

			foreach (var copyrightInfo in Licensing.CopyrightInfos)
			{
				ImGui.AlignTextToFramePadding();
				ImGui.TextUnformatted(copyrightInfo.Product);
				ImGui.SameLine();
				if (ImGui.Button(copyrightInfo.ProductUrl))
				{
#if GSE_OSX || GSE_ANDROID
					// we prefer SDL's OpenURL on some platforms
					// (mainly as some platforms just don't support Process.Start's shell execute)
					_ = SDL_OpenURL(copyrightInfo.ProductUrl);
#else
					try
					{
						Process.Start(new ProcessStartInfo(copyrightInfo.ProductUrl) { UseShellExecute = true });
					}
					catch
					{
						// ignored
					}
#endif
				}

				ImGui.TextUnformatted($"Copyright (c) {copyrightInfo.CopyrightHolder}");
				ImGui.TextUnformatted($"Under {copyrightInfo.LicenseId} License");

				ImGui.Separator();
			}

			if (ImGui.BeginTabBar("License Tabs"))
			{
				// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
				foreach (var license in Licensing.Licenses)
				{
					if (ImGui.BeginTabItem(license.Key))
					{
						ImGui.TextUnformatted(license.Value);
						ImGui.EndTabItem();
					}
				}

				ImGui.EndTabBar();
			}

			ImGui.EndPopup();
		}

		CheckModalWasClosed(pathOpen, ref _pathModalOpened);
		CheckModalWasClosed(inputOpen, ref _inputModalOpened);
		CheckModalWasClosed(videoOpen, ref _videoModalOpened);
		CheckModalWasClosed(audioOpen, ref _audioModalOpened);
		CheckModalWasClosed(osdOpen, ref _osdModalOpened);
		CheckModalWasClosed(miscOpen, ref _miscModalOpened);
		CheckModalWasClosed(aboutOpen, ref _aboutModalOpened);
	}
}
