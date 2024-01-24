using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using ImGuiNET;

using static SDL2.SDL;

using GSR.Audio;
using GSR.Emu;
using GSR.Emu.Controllers;
using GSR.Emu.Cores;
using GSR.Gui;
using GSR.Input;

namespace GSR;

/// <summary>
/// Wraps all GSR logic together.
/// MUST BE MANAGED BY GUI THREAD
/// </summary>
internal sealed class GSR : IDisposable
{
	private static readonly SDL_EventType[] _allowedEvents =
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
	];

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe int SDLEventFilter(IntPtr userdata, IntPtr sdlEvent)
	{
		var e = (SDL_Event*)sdlEvent;
		return Array.Exists(_allowedEvents, t => t == e->type) ? 1 : 0;
	}

	static GSR()
	{
		// we want a few SDL hints set before anything else
		SDL_SetHint(SDL_HINT_AUTO_UPDATE_JOYSTICKS, "0"); // we'll manually update joysticks (don't let the gui thread handle that!)
		unsafe
		{
			SDL_SetEventFilter(&SDLEventFilter, IntPtr.Zero); // filter out events which we don't care for
		}
	}

	/// <summary>
	/// The config, handling any user settings
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

	private readonly SDL_Event[] _sdlEvents = new SDL_Event[10];

	public GSR()
	{
		try
		{
			_mainWindow = new("GSR", true);
			_inputManager = new();
			// input manager is needed to load the config, as input bindings depend on user's keyboard layout
			_config = Config.LoadConfig(_inputManager, "gsr_config.json");
			_audioManager = new(100, null);
			_emuManager = new(_audioManager, _mainWindow.SdlRenderer);
			_emuManager.LoadRom(
				EmuCoreType.mGBA,
				new GBAController(_inputManager, _config.EmuControllerBindings),
				File.ReadAllBytes("gba.rom"),
				File.ReadAllBytes("gba.bios"));
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	public void Dispose()
	{
		_mainWindow?.Dispose();
        _emuManager?.Dispose();
        _audioManager?.Dispose();
		_inputManager?.Dispose();
		_config?.SaveConfig("gsr_config.json");
	}

	private bool HandleEvents()
	{
		SDL_PumpEvents();

		while (true)
		{
			var numEvents = SDL_PeepEvents(_sdlEvents, _sdlEvents.Length, SDL_eventaction.SDL_GETEVENT, SDL_EventType.SDL_QUIT, SDL_EventType.SDL_MOUSEWHEEL);
			if (numEvents == 0)
			{
				break;
			}

			for (var i = 0; i < numEvents; i++)
			{
				ref var e = ref _sdlEvents[i];
				if (e.type == SDL_EventType.SDL_QUIT)
				{
					return false;
				}

				var windowId = e.type switch
				{
					SDL_EventType.SDL_MOUSEMOTION or SDL_EventType.SDL_MOUSEWHEEL => e.motion.windowID,
					SDL_EventType.SDL_MOUSEBUTTONDOWN or SDL_EventType.SDL_MOUSEBUTTONUP => e.button.windowID,
					SDL_EventType.SDL_TEXTINPUT => e.text.windowID,
					SDL_EventType.SDL_KEYDOWN or SDL_EventType.SDL_KEYUP => e.key.windowID,
					SDL_EventType.SDL_WINDOWEVENT => e.window.windowID,
					_ => 0u,
				};

				if (windowId == _mainWindow.WindowId)
				{
					_mainWindow.ProcessEvent(in e);
					if (e.window.windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE)
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
		
	}

	private void DrawEmu()
	{
		var sdlVideoTexture = _emuManager.SdlVideoTexture;
		if (sdlVideoTexture == IntPtr.Zero)
		{
			DrawNullEmu();
			return;
		}

		ImGui.Image(sdlVideoTexture, ImGui.GetContentRegionAvail());
	}

	private void DrawMenu()
	{
		if (ImGui.BeginMenuBar())
		{
			if (ImGui.BeginMenu("Test"))
			{
				if (ImGui.MenuItem("Test Item", null))
				{
					SDL_ShowSimpleMessageBox(
						flags: SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION,
						title: "Test",
						message: "Testing",
						window: IntPtr.Zero
					);
				}

				ImGui.EndMenu();
			}

			ImGui.EndMenuBar();
		}
	}

	public int MainLoop()
	{
		while (true)
		{
			if (!HandleEvents())
			{
				return 0;
			}

			_mainWindow.NewFrame();

			var vp = ImGui.GetMainViewport();
			ImGui.SetNextWindowPos(vp.Pos);
			ImGui.SetNextWindowSize(vp.Size);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

			ImGui.Begin("Emu Blit", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.MenuBar);
			DrawEmu();
			ImGui.PopStyleVar(3);
			DrawMenu();
			ImGui.End();

			_mainWindow.Render();
			_emuManager.DrawLastFrameToTexture(); // do this immediately after render, so it closely aligns to vsync
		}
	}
}
