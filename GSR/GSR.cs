// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Immutable;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using ImGuiNET;

using static SDL2.SDL;

#if GSR_WINDOWS
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
#endif

using GSR.Audio;
using GSR.Emu;
using GSR.Emu.Controllers;
using GSR.Gui;
using GSR.Input;

namespace GSR;

/// <summary>
/// Wraps all GSR logic together.
/// MUST BE MANAGED BY GUI THREAD
/// </summary>
internal sealed class GSR : IDisposable
{
	private static readonly ImmutableArray<SDL_EventType> _allowedEvents =
	[
		// ImGui events
		SDL_EventType.SDL_QUIT,
		SDL_EventType.SDL_WINDOWEVENT,
		SDL_EventType.SDL_KEYDOWN, SDL_EventType.SDL_KEYUP,
		SDL_EventType.SDL_TEXTINPUT,
		SDL_EventType.SDL_MOUSEMOTION,
		SDL_EventType.SDL_MOUSEBUTTONDOWN, SDL_EventType.SDL_MOUSEBUTTONUP,
		SDL_EventType.SDL_MOUSEWHEEL,
		// SDL joystick handler events
		SDL_EventType.SDL_JOYDEVICEADDED, SDL_EventType.SDL_JOYDEVICEREMOVED,
		// Drop file event (for drag+drop savestate/ROM files)
		// drop begin/complete have to be allowed through, otherwise SDL will refuse to send any dropfile events
		SDL_EventType.SDL_DROPFILE, SDL_EventType.SDL_DROPBEGIN, SDL_EventType.SDL_DROPCOMPLETE,
		// Audio hotplug events
		SDL_EventType.SDL_AUDIODEVICEADDED, SDL_EventType.SDL_AUDIODEVICEREMOVED,
	];

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe int SDLEventFilter(IntPtr userdata, IntPtr sdlEvent)
	{
		var e = (SDL_Event*)sdlEvent;
		return _allowedEvents.Contains(e->type) ? 1 : 0;
	}

	static GSR()
	{
		// we want a few SDL hints set before anything else
		SDL_SetHint(SDL_HINT_AUTO_UPDATE_JOYSTICKS, "0"); // we'll manually update joysticks (don't let the gui thread handle that!)
		unsafe
		{
			SDL_SetEventFilter(&SDLEventFilter, IntPtr.Zero); // filter out events which we don't care for
		}

#if GSR_WINDOWS
		// if the user runs with elevated privileges, drag-n-drop will be broken on win7+
		// do this to bypass the issue
		const uint WM_COPYGLOBALDATA = 0x0049; // apparently this isn't documented anymore?
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

	private InputGate InputGateCallback()
	{
		if (SDL_GetKeyboardFocus() != IntPtr.Zero)
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

	public GSR()
	{
		try
		{
			_config = Config.LoadConfig(PathResolver.GetConfigPath());
			_mainWindow = new("GSR", _config, true);
			_inputManager = new(in _mainWindow.SdlSysWMInfo);
			// input manager is needed to fully load the config, as input bindings depend on user's keyboard layout
			// default bindings will be set if this fails for some reason
			_config.DeserializeInputBindings(_inputManager, _mainWindow.SdlWindow);
			_audioManager = new(_config.AudioDeviceName, _config.LatencyMs, _config.Volume);
			_emuManager = new(_audioManager);
			_postProcessor = new(_config, _emuManager, _mainWindow.SdlRenderer);
			_osdManager = new(_emuManager);
			_gbController = new(_inputManager, _config.EmuControllerBindings, InputGateCallback);
			_gbaController = new(_inputManager, _config.EmuControllerBindings, InputGateCallback);
			_romLoader = new(_config, _emuManager, _postProcessor, _osdManager, _gbController, _gbaController, _mainWindow);
			_stateManager = new(_config, _emuManager, _osdManager);
			_hotkeyManager = new(_config, _emuManager, _audioManager, _osdManager, _inputManager, _stateManager, _mainWindow, HotkeyInputGateCallback);
			_imGuiModals = new(_config, _emuManager, _inputManager, _audioManager, _hotkeyManager, _mainWindow);
			_imGuiMenuBar = new(_config, _emuManager, _romLoader, _stateManager, _mainWindow, _imGuiModals);
			_mainWindow.SetWindowPos(SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED);
			_mainWindow.SetVisible(true);
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	public void Dispose()
	{
		_postProcessor?.Dispose();
		_emuManager?.Dispose();
		_audioManager?.Dispose();
		_inputManager?.Dispose();
		_mainWindow?.Dispose();
		_config?.SaveConfig(PathResolver.GetConfigPath());
	}

	private unsafe bool HandleEvents()
	{
		SDL_PumpEvents();

		fixed (SDL_Event* sdlEvents = _sdlEvents)
		{
			while (true)
			{
				var numEvents = SDL_PeepEvents(sdlEvents, _sdlEvents.Length, SDL_eventaction.SDL_GETEVENT, SDL_EventType.SDL_QUIT, SDL_EventType.SDL_MOUSEWHEEL);
				numEvents += SDL_PeepEvents(sdlEvents + numEvents, _sdlEvents.Length - numEvents, SDL_eventaction.SDL_GETEVENT, SDL_EventType.SDL_DROPFILE, SDL_EventType.SDL_AUDIODEVICEREMOVED);
				if (numEvents == 0)
				{
					break;
				}

				for (var i = 0; i < numEvents; i++)
				{
					var e = &sdlEvents[i];
					// ReSharper disable once ConvertIfStatementToSwitchStatement
					if (e->type == SDL_EventType.SDL_QUIT)
					{
						return false;
					}

					if (e->type == SDL_EventType.SDL_DROPFILE)
					{
						var filePath = UTF8_ToManaged(e->drop.file, true);
						if (_imGuiModals.ModalIsOpened)
						{
							// don't allow drag+drop while a modal is open
							continue;
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

					if (e->type is SDL_EventType.SDL_AUDIODEVICEADDED or SDL_EventType.SDL_AUDIODEVICEREMOVED)
					{
						_imGuiModals.AudioDeviceListChanged = true;
						continue;
					}

					var windowId = e->type switch
					{
						SDL_EventType.SDL_MOUSEMOTION or SDL_EventType.SDL_MOUSEWHEEL => e->motion.windowID,
						SDL_EventType.SDL_MOUSEBUTTONDOWN or SDL_EventType.SDL_MOUSEBUTTONUP => e->button.windowID,
						SDL_EventType.SDL_TEXTINPUT => e->text.windowID,
						SDL_EventType.SDL_KEYDOWN or SDL_EventType.SDL_KEYUP => e->key.windowID,
						SDL_EventType.SDL_WINDOWEVENT => e->window.windowID,
						_ => 0u,
					};

					if (windowId == _mainWindow.WindowId)
					{
						// suppress imgui keyboard inputs if the emulator is unpaused with a rom loaded
						if (e->type == SDL_EventType.SDL_KEYDOWN && _emuManager.EmuAcceptingInputs)
						{
							continue;
						}

						_mainWindow.ProcessEvent(in *e);

						if (e->type == SDL_EventType.SDL_WINDOWEVENT && e->window.windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE)
						{
							return false;
						}
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

		var contentRegionAvail = ImGui.GetContentRegionAvail();
		var finalTex = _postProcessor.DoPostProcessing((int)contentRegionAvail.X, (int)contentRegionAvail.Y);
		ImGui.Image(finalTex.Texture, contentRegionAvail);
	}

	public int MainLoop()
	{
		while (true)
		{
			if (!HandleEvents())
			{
				return 0;
			}

			// this needs to happen periodically
			if (_audioManager.RecoverLostAudioDeviceIfNeeded())
			{
				_config.AudioDeviceName = _audioManager.AudioDeviceName;
			}

			if (_hotkeyManager.InputBindingsChanged && !_imGuiModals.ModalIsOpened)
			{
				_hotkeyManager.OnInputBindingsChange();
			}

			_hotkeyManager.ProcessHotkeys();

			_mainWindow.NewFrame();

			var statusBarHeight = 0f;
			if (!_config.HideStatusBar)
			{
				_osdManager.RunStatusBar();
				statusBarHeight = ImGui.GetFrameHeight();
			}

			var vp = ImGui.GetMainViewport();
			ImGui.SetNextWindowPos(vp.Pos);
			ImGui.SetNextWindowSize(vp.Size - new Vector2(0, statusBarHeight));

			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

			if (ImGui.Begin("GSR", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoBringToFrontOnFocus))
			{
				DrawEmu();
				ImGui.PopStyleVar(3);
				_imGuiMenuBar.RunMenuBar();
				_imGuiModals.RunModals();
			}
			else
			{
				ImGui.PopStyleVar(3);
			}

			ImGui.End();

			if (_config.HideStatusBar)
			{
				_osdManager.RunOverlay();
			}

			_mainWindow.Render();

			// do this immediately after render, so it closely aligns to vsync
			if (_emuManager.RomIsLoaded)
			{
				_postProcessor.RenderEmuTexture(_emuManager.GetVideoBuffer());
			}
		}
	}
}
