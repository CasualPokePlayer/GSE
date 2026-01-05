// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Immutable;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using ImGuiNET;

using static SDL3.SDL;

#if GSE_WINDOWS
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
#endif

using GSE.Audio;
using GSE.Emu;
using GSE.Emu.Controllers;
using GSE.Gui;
using GSE.Input;

#if GSE_ANDROID
using GSE.Android;
#endif

namespace GSE;

/// <summary>
/// Wraps all GSE logic together.
/// MUST BE MANAGED BY GUI THREAD
/// </summary>
internal sealed class GSE : IDisposable
{
	private static readonly ImmutableArray<SDL_EventType> _allowedEvents =
	[
		// ImGui events
		SDL_EventType.SDL_EVENT_QUIT,
		SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER, SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE,
		SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED, SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST,
		SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED,
		SDL_EventType.SDL_EVENT_KEY_DOWN, SDL_EventType.SDL_EVENT_KEY_UP,
		SDL_EventType.SDL_EVENT_TEXT_INPUT,
		SDL_EventType.SDL_EVENT_MOUSE_MOTION,
		SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN, SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP,
		SDL_EventType.SDL_EVENT_MOUSE_WHEEL,
		// SDL joystick handler events
		SDL_EventType.SDL_EVENT_JOYSTICK_ADDED, SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED,
		// Drop file event (for drag+drop savestate/ROM files)
		// drop begin/complete have to be allowed through, otherwise SDL will refuse to send any dropfile events
		SDL_EventType.SDL_EVENT_DROP_FILE, SDL_EventType.SDL_EVENT_DROP_BEGIN, SDL_EventType.SDL_EVENT_DROP_COMPLETE,
		// Audio hotplug events
		SDL_EventType.SDL_EVENT_AUDIO_DEVICE_ADDED, SDL_EventType.SDL_EVENT_AUDIO_DEVICE_REMOVED, SDL_EventType.SDL_EVENT_AUDIO_DEVICE_FORMAT_CHANGED,
		// Renderer specific events
		SDL_EventType.SDL_EVENT_RENDER_TARGETS_RESET, SDL_EventType.SDL_EVENT_RENDER_DEVICE_RESET,
	];

	[UnmanagedCallersOnly(CallConvs = [ typeof(CallConvCdecl) ])]
	private static unsafe SDLBool SDLEventFilter(nint userdata, SDL_Event* sdlEvent)
	{
		var eventType = (SDL_EventType)sdlEvent->type;
#if GSE_ANDROID
		var gse = (GSE)GCHandle.FromIntPtr(userdata).Target!;
		// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
		switch (eventType)
		{
			case SDL_EventType.SDL_EVENT_TERMINATING:
				gse.OnTermination();
				break;
			case SDL_EventType.SDL_EVENT_WILL_ENTER_BACKGROUND:
				gse.OnEnterBackground();
				break;
			case SDL_EventType.SDL_EVENT_DID_ENTER_FOREGROUND:
				gse.OnEnterForeground();
				break;
		}
#endif

		return _allowedEvents.Contains(eventType);
	}

	static GSE()
	{
		// We want to set SDL hints set before anything else
		SDL_SetHint(SDL_HINT_AUTO_UPDATE_JOYSTICKS, "0"); // We'll manually update joysticks (don't let the gui thread handle that!)

#if GSE_WINDOWS
		// If the user runs with elevated privileges, drag-n-drop will be broken on win7+
		// Do this to bypass the issue
		const uint WM_COPYGLOBALDATA = 0x0049; // Apparently this isn't documented anymore?
		PInvoke.ChangeWindowMessageFilter(PInvoke.WM_DROPFILES, CHANGE_WINDOW_MESSAGE_FILTER_FLAGS.MSGFLT_ADD);
		PInvoke.ChangeWindowMessageFilter(PInvoke.WM_COPYDATA, CHANGE_WINDOW_MESSAGE_FILTER_FLAGS.MSGFLT_ADD);
		PInvoke.ChangeWindowMessageFilter(WM_COPYGLOBALDATA, CHANGE_WINDOW_MESSAGE_FILTER_FLAGS.MSGFLT_ADD);
#endif
	}

	/// <summary>
	/// The config, storing any user settings
	/// </summary>
	private readonly Config _config;

	/// <summary>
	/// The main window, this is where emulation and most gui elements will be displayed
	/// </summary>
	private readonly ImGuiWindow _mainWindow;

	/// <summary>
	/// The input manager, this will handle polling host inputs and assist in input bindings
	/// This must be created after the main window is created!
	/// </summary>
	private readonly InputManager _inputManager;

	/// <summary>
	/// The audio manager, this will handle obtaining, resampling, and playing emulator audio
	/// This must be created after the main window is created!
	/// </summary>
	private readonly AudioManager _audioManager;

	/// <summary>
	/// The emu manager, this will handle running the underlying emulator
	/// </summary>
	private readonly EmuManager _emuManager;

	/// <summary>
	/// The post processor, handles scaling/letterboxing emu video output
	/// </summary>
	private readonly PostProcessor _postProcessor;

	/// <summary>
	/// OSD manager, may be a status bar or an overlay
	/// </summary>
	private readonly OSDManager _osdManager;

	/// <summary>
	/// The GB controller, used for GB games
	/// </summary>
	private readonly GBController _gbController;

	/// <summary>
	/// The GBA controller, used for GB games
	/// </summary>
	private readonly GBAController _gbaController;

	/// <summary>
	/// ROM loading logic
	/// </summary>
	private readonly RomLoader _romLoader;

	/// <summary>
	/// Savestate logic
	/// </summary>
	private readonly StateManager _stateManager;

	/// <summary>
	/// Hotkey logic
	/// </summary>
	private readonly HotkeyManager _hotkeyManager;

	/// <summary>
	/// ImGui popup modal logic, for the main window
	/// </summary>
	private readonly ImGuiModals _imGuiModals;

	/// <summary>
	/// ImGui menu bar logic, for the main window
	/// </summary>
	private readonly ImGuiMenuBar _imGuiMenuBar;


	private readonly SDL_Event[] _sdlEvents = new SDL_Event[10];
	private GCHandle _gseUserData;

	private InputGate InputGateCallback()
	{
		if (SDL_GetKeyboardFocus() != 0)
		{
			return new(true, true);
		}

		var keyInputAllowed = _config.AllowBackgroundInput && !_config.BackgroundInputForJoysticksOnly;
		var joystickInputAllowed = _config.AllowBackgroundInput;
		return new(keyInputAllowed, joystickInputAllowed);
	}

	private InputGate HotkeyInputGateCallback()
	{
		return _imGuiModals.ModalIsOpened
			? new(false, false) : InputGateCallback();
	}

	public GSE()
	{
		try
		{
			_gseUserData = GCHandle.Alloc(this, GCHandleType.Weak);
			unsafe
			{
				// Filter out events which we don't care for
				SDL_SetEventFilter(&SDLEventFilter, GCHandle.ToIntPtr(_gseUserData));
			}

			_config = Config.LoadConfig();
			_mainWindow = new("GSE", _config);
			_inputManager = new(_mainWindow.SdlWindowProperties, _config.EnableDirectInput);
			// Input manager is needed to fully load the config, as input bindings depend on user's keyboard layout
			// Default bindings will be set if this fails for some reason
			_config.DeserializeInputBindings(_inputManager, _mainWindow);
			_audioManager = new(_config.AudioDeviceName, _config.LatencyMs, _config.Volume);
			_emuManager = new(_audioManager, _config.PreferLowLatency);
			_postProcessor = new(_config, _emuManager, _mainWindow.SdlRenderer);
			_osdManager = new(_config, _emuManager, _mainWindow.SdlRenderer);
			_gbController = new(_inputManager, _config.EmuControllerBindings, InputGateCallback);
			_gbaController = new(_inputManager, _config.EmuControllerBindings, InputGateCallback);
			_romLoader = new(_config, _emuManager, _postProcessor, _osdManager, _gbController, _gbaController, _mainWindow);
			_stateManager = new(_config, _emuManager, _osdManager);
			_hotkeyManager = new(_config, _emuManager, _audioManager, _osdManager, _inputManager, _stateManager, _mainWindow, HotkeyInputGateCallback);
			_imGuiModals = new(_config, _emuManager, _inputManager, _audioManager, _hotkeyManager, _osdManager, _mainWindow);
			_imGuiMenuBar = new(_config, _emuManager, _romLoader, _stateManager, _osdManager, _mainWindow, _imGuiModals);
			_mainWindow.SetWindowPos(SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED);
			_mainWindow.SetVisible(true);
#if GSE_ANDROID
			AndroidInput.InputManager = _inputManager;
#endif
		}
		catch
		{
			// This just works around a potential Android bug when opening an audio device it doesn't like
			if (_inputManager != null && _audioManager == null)
			{
				_config.AudioDeviceName = AudioManager.DEFAULT_AUDIO_DEVICE;
			}

			Dispose();
			throw;
		}
	}

	public void Dispose()
	{
#if GSE_ANDROID
		AndroidInput.InputManager = null;
#endif
		_postProcessor?.Dispose();
		_osdManager?.Dispose();
		_emuManager?.Dispose();
		_audioManager?.Dispose();
		_inputManager?.Dispose();
		_mainWindow?.Dispose();
		_config?.SaveConfig(PathResolver.GetConfigPath());

		unsafe
		{
			SDL_SetEventFilter(null, 0);
		}

		if (_gseUserData.IsAllocated)
		{
			_gseUserData.Free();
		}
	}

#if GSE_ANDROID || GSE_WINDOWS
	private void OnTermination()
	{
		_emuManager?.FlushSave();
		_config?.SaveConfig(PathResolver.GetConfigPath());
	}

	private bool _wasPausedOnBackground;

	private void OnEnterBackground()
	{
		_wasPausedOnBackground = _wasPausedOnBackground || _emuManager.Pause();
		_mainWindow.SdlRenderer.PauseRenderer();
	}

	private bool _test;
	private ulong _enterForegroundCycleCount;

	private void OnEnterForeground()
	{
		_mainWindow.SdlRenderer.RestoreRenderer();
		if (_wasPausedOnBackground)
		{
			_emuManager.Unpause();
			_wasPausedOnBackground = false;
		}

		_enterForegroundCycleCount = _emuManager.GetCycleCount();
		_test = true;
	}
#endif

	private bool HandleEvents()
	{
		SDL_PumpEvents();

		if (_test)
		{
			_ = SDL_ShowSimpleMessageBox(
				flags: SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION,
				title: "Test",
				message: $"Enter foreground: {_enterForegroundCycleCount}",
				window: _mainWindow.SdlWindow
			);
			
			_test = false;
		}

		while (true)
		{
			var numEvents = SDL_PeepEvents(_sdlEvents, _sdlEvents.Length,
				SDL_EventAction.SDL_GETEVENT, (uint)SDL_EventType.SDL_EVENT_QUIT, (uint)SDL_EventType.SDL_EVENT_MOUSE_WHEEL);
			numEvents += SDL_PeepEvents(_sdlEvents.AsSpan(numEvents), _sdlEvents.Length - numEvents,
				SDL_EventAction.SDL_GETEVENT, (uint)SDL_EventType.SDL_EVENT_DROP_FILE, (uint)SDL_EventType.SDL_EVENT_AUDIO_DEVICE_FORMAT_CHANGED);
			if (numEvents == 0)
			{
				break;
			}

			for (var i = 0; i < numEvents; i++)
			{
				ref var e = ref _sdlEvents[i];
				var eventType = (SDL_EventType)e.type;

				// ReSharper disable once ConvertIfStatementToSwitchStatement
				if (eventType == SDL_EventType.SDL_EVENT_QUIT)
				{
					return false;
				}

				if (eventType == SDL_EventType.SDL_EVENT_DROP_FILE)
				{
					if (_imGuiModals.ModalIsOpened)
					{
						// Don't allow drag+drop while a modal is open
						continue;
					}

					string filePath;
					unsafe
					{
						filePath = Marshal.PtrToStringUTF8((nint)e.drop.data) ?? string.Empty;
					}

					var fileExt = Path.GetExtension(filePath);
					if (fileExt.Equals(".gqs", StringComparison.OrdinalIgnoreCase))
					{
						if (_emuManager.RomIsLoaded)
						{
							_emuManager.LoadState(filePath);
						}
					}
					else if (RomLoader.RomAndCompressionExtensions.Contains(fileExt, StringComparer.OrdinalIgnoreCase))
					{
						_romLoader.LoadRomFile(filePath);
					}

					continue;
				}

				if (eventType == SDL_EventType.SDL_EVENT_AUDIO_DEVICE_REMOVED)
				{
					_audioManager.RecoverLostAudioDeviceIfNeeded(e.adevice.which);
					_config.AudioDeviceName = _audioManager.AudioDeviceName;
				}

				// ReSharper disable once ConvertIfStatementToSwitchStatement
				if (eventType is SDL_EventType.SDL_EVENT_AUDIO_DEVICE_ADDED or SDL_EventType.SDL_EVENT_AUDIO_DEVICE_REMOVED)
				{
					_imGuiModals.AudioDeviceListChanged = true;
					continue;
				}

				if (eventType == SDL_EventType.SDL_EVENT_AUDIO_DEVICE_FORMAT_CHANGED)
				{
					_audioManager.ResetAudioDeviceIfNeeded(e.adevice.which);
					_config.AudioDeviceName = _audioManager.AudioDeviceName;
					continue;
				}

				var windowId = eventType switch
				{
					SDL_EventType.SDL_EVENT_MOUSE_MOTION or SDL_EventType.SDL_EVENT_MOUSE_WHEEL => e.motion.windowID,
					SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN or SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP => e.button.windowID,
					SDL_EventType.SDL_EVENT_TEXT_INPUT => e.text.windowID,
					SDL_EventType.SDL_EVENT_KEY_DOWN or SDL_EventType.SDL_EVENT_KEY_UP => e.key.windowID,
					>= SDL_EventType.SDL_EVENT_WINDOW_FIRST and <= SDL_EventType.SDL_EVENT_WINDOW_LAST => e.window.windowID,
					_ => 0u,
				};

				if (windowId == _mainWindow.WindowId)
				{
					// Suppress imgui keyboard inputs if the emulator is unpaused with a rom loaded
					if (eventType is SDL_EventType.SDL_EVENT_KEY_DOWN or SDL_EventType.SDL_EVENT_KEY_UP && _emuManager.EmuAcceptingInputs)
					{
						continue;
					}

					_mainWindow.ProcessEvent(in e);

					if (eventType == SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED)
					{
						return false;
					}
				}
			}
		}

		return true;
	}

	private void DrawNullEmu()
	{
		if (!_imGuiModals.ModalIsOpened)
		{
			// TODO: put some nice user message on the user needing to provide bios/roms
		}
	}

	private void DrawEmu()
	{
		if (!_emuManager.RomIsLoaded)
		{
			DrawNullEmu();
			return;
		}

		if (_emuManager.EmuAcceptingInputs && _config.PreferLowLatency)
		{
			_postProcessor.RenderEmuTexture(
				_emuManager.GetVideoBuffer(waitForUpdate: !_mainWindow.IsFullscreen));
		}

		var contentRegionAvail = ImGui.GetContentRegionAvail();
		var finalTex = _postProcessor.DoPostProcessing((int)contentRegionAvail.X, (int)contentRegionAvail.Y);
		ImGui.Image(finalTex.TextureId, contentRegionAvail);
	}

	public int MainLoop()
	{
		while (true)
		{
			if (!HandleEvents())
			{
				return 0;
			}

			if (_hotkeyManager.InputBindingsChanged && !_imGuiModals.ModalIsOpened)
			{
				_hotkeyManager.OnInputBindingsChange();
			}

			_hotkeyManager.ProcessHotkeys();

			_mainWindow.NewFrame();

			// Position of the emu window is below the menu bar
			var barHeight = ImGui.GetFrameHeight();
			var menuBarHeight = 0.0f;
			var statusBarHeight = 0.0f;

			var hideMenuBar = _config.HideMenuBarOnUnpause && _emuManager.EmuAcceptingInputs && _config.HotkeyBindings.PauseButtonBindings.Count != 0;
			if (!hideMenuBar)
			{
				_imGuiMenuBar.RunMenuBar();
				menuBarHeight = barHeight;
			}

			if (!_config.HideStatusBar)
			{
				_osdManager.RunStatusBar();
				statusBarHeight = barHeight;
			}

			var vp = ImGui.GetMainViewport();
			ImGui.SetNextWindowPos(vp.Pos + new Vector2(0, menuBarHeight));
			ImGui.SetNextWindowSize(vp.Size - new Vector2(0, menuBarHeight + statusBarHeight));

			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

			if (ImGui.Begin("GSE", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus))
			{
				DrawEmu();
				ImGui.PopStyleVar(3);
				_imGuiModals.RunModals();
			}
			else
			{
				ImGui.PopStyleVar(3);
			}

			ImGui.End();

			if (_config.HideStatusBar)
			{
				_osdManager.RunOverlay(_postProcessor.GetLastRenderPos());
			}

			if (!_config.HideStatePreviews)
			{
				_osdManager.RunStatePreviewOverlay();
			}

			if (_emuManager.EmuAcceptingInputs && _config.PreferLowLatency)
			{
				_mainWindow.Render(vsync: _mainWindow.IsFullscreen);
			}
			else
			{
				_mainWindow.Render(vsync: true);

				// Do this immediately after render, so it closely aligns to vsync
				if (_emuManager.RomIsLoaded)
				{
					_postProcessor.RenderEmuTexture(
						_emuManager.GetVideoBuffer(waitForUpdate: false));
				}
			}
		}
	}
}
