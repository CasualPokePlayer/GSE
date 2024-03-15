using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using ImGuiNET;

using static SDL2.SDL;

using GSR.Audio;
using GSR.Emu;
using GSR.Input;

namespace GSR.Gui;

internal sealed class ImGuiModals
{
	private const string BIOS_PATH_SETTINGS = "BIOS Path Settings";
	private const string INPUT_SETTINGS = "Input Settings";
	private const string VIDEO_SETTINGS = "Video Settings";
	private const string AUDIO_SETTINGS = "Audio Settings";
	private const string MISC_SETTINGS = "Misc Settings";

	private readonly Config _config;
	private readonly EmuManager _emuManager;
	private readonly InputManager _inputManager;
	private readonly AudioManager _audioManager;
	private readonly HotkeyManager _hotkeyManager;
	private readonly ImGuiWindow _mainWindow;

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

	public bool ModalIsOpened => _biosPathModalOpened || _inputModalOpened || _videoModalOpened || _audioModalOpened || _miscModalOpened;
	private bool _biosPathModalOpened;
	private bool _inputModalOpened;
	private bool _videoModalOpened;
	private bool _audioModalOpened;
	private bool _miscModalOpened;

	public bool OpenBiosPathModal;
	public bool OpenInputModal;
	public bool OpenVideoModal;
	public bool OpenAudioModal;
	public bool OpenMiscModal;

	private bool _didPause;

	public ImGuiModals(Config config, EmuManager emuManager, InputManager inputManager, AudioManager audioManager, HotkeyManager hotkeyManager, ImGuiWindow mainWindow)
	{
		_config = config;
		_emuManager = emuManager;
		_inputManager = inputManager;
		_audioManager = audioManager;
		_hotkeyManager = hotkeyManager;
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
					if (InputsOverlap(inputConfig.InputBindings[^i], inputConfig.InputBindings[^j]))
					{
						inputConfig.InputBindings.RemoveAt(inputConfig.InputBindings.Count - j);
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
			if (_renderDriverConfigStrings[i] == config.RenderDriver)
			{
				_renderDriverIndex = i;
			}
		}

		_mainWindow.SetResizable(_config.AllowManualResizing);
		UpdateWindowScale();

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
			}
		}
	}

	private void DoInputTab(IEnumerable<InputConfig> inputConfigs, float textSpacing)
	{
		foreach (var inputConfig in inputConfigs)
		{
			ImGui.AlignTextToFramePadding();
			ImGui.TextUnformatted(inputConfig.InputName);
			ImGui.SameLine(ImGui.GetFontSize() * textSpacing);

			var inputLabel = string.Join(',', inputConfig.InputBindings.Select(b => b.ModifierLabel != null ? $"{b.ModifierLabel}+{b.MainInputLabel}" : b.MainInputLabel));
			var labelSize = ImGui.CalcTextSize(inputLabel).X + ImGui.GetStyle().FramePadding.X * 2;
			var maxLabelSize = ImGui.GetWindowContentRegionMax().X - ImGui.GetFontSize() * (textSpacing + 4);
			var buttonWidth = maxLabelSize > labelSize ? maxLabelSize : 0;
			if (ImGui.Button($"{inputLabel}##{inputConfig.InputName}", new(buttonWidth, 0)))
			{
				_currentInputBindingList = inputConfig.InputBindings;
				_inputManager.BeginInputBinding();
				_startingInputBinding = true;
				_mainWindow.SuppressEscape = true;
			}

			ImGui.SameLine();
			if (ImGui.Button($"Clear##{inputConfig.InputName}"))
			{
				inputConfig.InputBindings.Clear();
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

	private void UpdateWindowScale()
	{
		var (emuWidth, emuHeight) = _emuManager.GetVideoDimensions(_config.HideSgbBorder);
		_mainWindow.SetWindowSize(emuWidth, emuHeight, _config.WindowScale, _config.HideStatusBar ? 1 : 2);
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
		CheckModalNeedsOpen(BIOS_PATH_SETTINGS, ref OpenBiosPathModal, ref _biosPathModalOpened);
		CheckModalNeedsOpen(INPUT_SETTINGS, ref OpenInputModal, ref _inputModalOpened);
		CheckModalNeedsOpen(VIDEO_SETTINGS, ref OpenVideoModal, ref _videoModalOpened);
		CheckModalNeedsOpen(AUDIO_SETTINGS, ref OpenAudioModal, ref _audioModalOpened);
		CheckModalNeedsOpen(MISC_SETTINGS, ref OpenMiscModal, ref _miscModalOpened);

		var center = ImGui.GetMainViewport().GetCenter();
		ImGui.SetNextWindowPos(center, ImGuiCond.Always, new(.5f, .5f));

		var biosPathOpen = true;
		if (ImGui.BeginPopupModal(BIOS_PATH_SETTINGS, ref biosPathOpen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
		{
			static string AddBiosPathButton(string system, string biosPathConfig)
			{
				ImGui.AlignTextToFramePadding();
				ImGui.TextUnformatted($"{system} BIOS:");

				ImGui.SameLine(ImGui.GetFontSize() * 6.5f);

				if (ImGui.Button($"{biosPathConfig ?? "Path not set..."}##{system}"))
				{
					var biosPath = OpenFileDialog.ShowDialog($"{system} BIOS File", null, RomLoader.BiosAndCompressionExtensions);
					if (biosPath != null)
					{
						biosPathConfig = biosPath;
					}
				}

				return biosPathConfig;
			}

			_config.GbBiosPath = AddBiosPathButton("GB", _config.GbBiosPath);
			_config.GbcBiosPath = AddBiosPathButton("GBC", _config.GbcBiosPath);
			_config.Sgb2BiosPath = AddBiosPathButton("SGB2", _config.Sgb2BiosPath);
			_config.GbaBiosPath = AddBiosPathButton("GBA", _config.GbaBiosPath);

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
					DoInputTab(_gameInputConfigs, 6.5f);
					ImGui.EndTabItem();
				}

				if (ImGui.BeginTabItem("Play"))
				{
					DoInputTab(_playInputConfigs, 10.5f);
					ImGui.EndTabItem();
				}

				if (ImGui.BeginTabItem("State"))
				{
					DoInputTab(_stateInputConfigs, 12.5f);
					ImGui.EndTabItem();
				}

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

					ImGui.EndTabItem();
				}

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

			var allowManualResizing = _config.AllowManualResizing;
			if (ImGui.Checkbox("Allow Manual Resizing", ref allowManualResizing))
			{
				_config.AllowManualResizing = allowManualResizing;
				_mainWindow.SetResizable(allowManualResizing);
				if (!allowManualResizing)
				{
					UpdateWindowScale();
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
				UpdateWindowScale();
			}

			// note that Combo items are 0 indexed, while window scale is 1 indexed
			var windowScale = _config.WindowScale - 1;
			if (ImGui.Combo("Window Scale", ref windowScale, _windowScalingOptions, maxWindowScale))
			{
				_config.WindowScale = windowScale + 1;
				UpdateWindowScale();
			}

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
				_audioManager.ChangeConfig(_config.AudioDeviceName, _config.LatencyMs, _config.Volume);
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
				_audioManager.ChangeConfig(_config.AudioDeviceName, _config.LatencyMs, _config.Volume);
			}

			var volume = _config.Volume;
			if (ImGui.SliderInt("Volume", ref volume, 0, 100))
			{
				_config.Volume = volume;
				_audioManager.ChangeConfig(_config.AudioDeviceName, _config.LatencyMs, _config.Volume);
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
					UpdateWindowScale();
				}
			}

			var hideStatusBar = _config.HideStatusBar;
			if (ImGui.Checkbox("Hide Status Bar", ref hideStatusBar))
			{
				_config.HideStatusBar = hideStatusBar;
				if (!_config.AllowManualResizing)
				{
					UpdateWindowScale();
				}
			}

			if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
			{
				var disableWin11RoundCorners = _config.DisableWin11RoundCorners;
				if (ImGui.Checkbox("Disable Win11 Round Corners", ref disableWin11RoundCorners))
				{
					_config.DisableWin11RoundCorners = disableWin11RoundCorners;
				}
			}

			ImGui.EndPopup();
		}

		CheckModalWasClosed(biosPathOpen, ref _biosPathModalOpened);
		CheckModalWasClosed(inputOpen, ref _inputModalOpened);
		CheckModalWasClosed(videoOpen, ref _videoModalOpened);
		CheckModalWasClosed(audioOpen, ref _audioModalOpened);
		CheckModalWasClosed(miscOpen, ref _miscModalOpened);
	}
}
