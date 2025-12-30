// Copyright (c) 2024 CasualPokePlayer & Omar Cornut
// SPDX-License-Identifier: MPL-2.0 or MIT

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using ImGuiNET;

using static SDL3.SDL;

#if GSE_WINDOWS
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
#endif

using GSE.Emu;

namespace GSE.Gui;

/// <summary>
/// C# port of https://github.com/ocornut/imgui/blob/001f102/backends/imgui_impl_sdl2.cpp
/// and https://github.com/ocornut/imgui/blob/ab522dd/backends/imgui_impl_sdlrenderer2.cpp
/// with various improvements / original additions
/// </summary>
internal sealed class ImGuiWindow : IDisposable
{
	private static readonly nint[] _sdlCursors = new nint[(int)ImGuiMouseCursor.COUNT];

	// We use SDL keyboard here mainly as a convenience
	// As ImGui wants input events, not input state
	// Also, we only ever want focused inputs here, not unfocused inputs
	private static readonly FrozenDictionary<SDL_Keycode, ImGuiKey> _imGuiMap = new Dictionary<SDL_Keycode, ImGuiKey>
	{
		[SDL_Keycode.SDLK_TAB] = ImGuiKey.Tab,
		[SDL_Keycode.SDLK_LEFT] = ImGuiKey.LeftArrow,
		[SDL_Keycode.SDLK_RIGHT] = ImGuiKey.RightArrow,
		[SDL_Keycode.SDLK_UP] = ImGuiKey.UpArrow,
		[SDL_Keycode.SDLK_DOWN] = ImGuiKey.DownArrow,
		[SDL_Keycode.SDLK_PAGEUP] = ImGuiKey.PageUp,
		[SDL_Keycode.SDLK_PAGEDOWN] = ImGuiKey.PageDown,
		[SDL_Keycode.SDLK_HOME] = ImGuiKey.Home,
		[SDL_Keycode.SDLK_END] = ImGuiKey.End,
		[SDL_Keycode.SDLK_INSERT] = ImGuiKey.Insert,
		[SDL_Keycode.SDLK_DELETE] = ImGuiKey.Delete,
		[SDL_Keycode.SDLK_BACKSPACE] = ImGuiKey.Backspace,
		[SDL_Keycode.SDLK_SPACE] = ImGuiKey.Space,
		[SDL_Keycode.SDLK_RETURN] = ImGuiKey.Enter,
		[SDL_Keycode.SDLK_ESCAPE] = ImGuiKey.Escape,
		[SDL_Keycode.SDLK_APOSTROPHE] = ImGuiKey.Apostrophe,
		[SDL_Keycode.SDLK_COMMA] = ImGuiKey.Comma,
		[SDL_Keycode.SDLK_MINUS] = ImGuiKey.Minus,
		[SDL_Keycode.SDLK_PERIOD] = ImGuiKey.Period,
		[SDL_Keycode.SDLK_SLASH] = ImGuiKey.Slash,
		[SDL_Keycode.SDLK_SEMICOLON] = ImGuiKey.Semicolon,
		[SDL_Keycode.SDLK_EQUALS] = ImGuiKey.Equal,
		[SDL_Keycode.SDLK_LEFTBRACKET] = ImGuiKey.LeftBracket,
		[SDL_Keycode.SDLK_BACKSLASH] = ImGuiKey.Backslash,
		[SDL_Keycode.SDLK_RIGHTBRACKET] = ImGuiKey.RightBracket,
		[SDL_Keycode.SDLK_GRAVE] = ImGuiKey.GraveAccent,
		[SDL_Keycode.SDLK_CAPSLOCK] = ImGuiKey.CapsLock,
		[SDL_Keycode.SDLK_SCROLLLOCK] = ImGuiKey.ScrollLock,
		[SDL_Keycode.SDLK_NUMLOCKCLEAR] = ImGuiKey.NumLock,
		[SDL_Keycode.SDLK_PRINTSCREEN] = ImGuiKey.PrintScreen,
		[SDL_Keycode.SDLK_PAUSE] = ImGuiKey.Pause,
		[SDL_Keycode.SDLK_KP_0] = ImGuiKey.Keypad0,
		[SDL_Keycode.SDLK_KP_1] = ImGuiKey.Keypad1,
		[SDL_Keycode.SDLK_KP_2] = ImGuiKey.Keypad2,
		[SDL_Keycode.SDLK_KP_3] = ImGuiKey.Keypad3,
		[SDL_Keycode.SDLK_KP_4] = ImGuiKey.Keypad4,
		[SDL_Keycode.SDLK_KP_5] = ImGuiKey.Keypad5,
		[SDL_Keycode.SDLK_KP_6] = ImGuiKey.Keypad6,
		[SDL_Keycode.SDLK_KP_7] = ImGuiKey.Keypad7,
		[SDL_Keycode.SDLK_KP_8] = ImGuiKey.Keypad8,
		[SDL_Keycode.SDLK_KP_9] = ImGuiKey.Keypad9,
		[SDL_Keycode.SDLK_KP_PERIOD] = ImGuiKey.KeypadDecimal,
		[SDL_Keycode.SDLK_KP_DIVIDE] = ImGuiKey.KeypadDivide,
		[SDL_Keycode.SDLK_KP_MULTIPLY] = ImGuiKey.KeypadMultiply,
		[SDL_Keycode.SDLK_KP_MINUS] = ImGuiKey.KeypadSubtract,
		[SDL_Keycode.SDLK_KP_PLUS] = ImGuiKey.KeypadAdd,
		[SDL_Keycode.SDLK_KP_ENTER] = ImGuiKey.KeypadEnter,
		[SDL_Keycode.SDLK_KP_EQUALS] = ImGuiKey.KeypadEqual,
		[SDL_Keycode.SDLK_LCTRL] = ImGuiKey.LeftCtrl,
		[SDL_Keycode.SDLK_LSHIFT] = ImGuiKey.LeftShift,
		[SDL_Keycode.SDLK_LALT] = ImGuiKey.LeftAlt,
		[SDL_Keycode.SDLK_LGUI] = ImGuiKey.LeftSuper,
		[SDL_Keycode.SDLK_RCTRL] = ImGuiKey.RightCtrl,
		[SDL_Keycode.SDLK_RSHIFT] = ImGuiKey.RightShift,
		[SDL_Keycode.SDLK_RALT] = ImGuiKey.RightAlt,
		[SDL_Keycode.SDLK_RGUI] = ImGuiKey.RightSuper,
		[SDL_Keycode.SDLK_APPLICATION] = ImGuiKey.Menu,
		[SDL_Keycode.SDLK_0] = ImGuiKey._0,
		[SDL_Keycode.SDLK_1] = ImGuiKey._1,
		[SDL_Keycode.SDLK_2] = ImGuiKey._2,
		[SDL_Keycode.SDLK_3] = ImGuiKey._3,
		[SDL_Keycode.SDLK_4] = ImGuiKey._4,
		[SDL_Keycode.SDLK_5] = ImGuiKey._5,
		[SDL_Keycode.SDLK_6] = ImGuiKey._6,
		[SDL_Keycode.SDLK_7] = ImGuiKey._7,
		[SDL_Keycode.SDLK_8] = ImGuiKey._8,
		[SDL_Keycode.SDLK_9] = ImGuiKey._9,
		[SDL_Keycode.SDLK_A] = ImGuiKey.A,
		[SDL_Keycode.SDLK_B] = ImGuiKey.B,
		[SDL_Keycode.SDLK_C] = ImGuiKey.C,
		[SDL_Keycode.SDLK_D] = ImGuiKey.D,
		[SDL_Keycode.SDLK_E] = ImGuiKey.E,
		[SDL_Keycode.SDLK_F] = ImGuiKey.F,
		[SDL_Keycode.SDLK_G] = ImGuiKey.G,
		[SDL_Keycode.SDLK_H] = ImGuiKey.H,
		[SDL_Keycode.SDLK_I] = ImGuiKey.I,
		[SDL_Keycode.SDLK_J] = ImGuiKey.J,
		[SDL_Keycode.SDLK_K] = ImGuiKey.K,
		[SDL_Keycode.SDLK_L] = ImGuiKey.L,
		[SDL_Keycode.SDLK_M] = ImGuiKey.M,
		[SDL_Keycode.SDLK_N] = ImGuiKey.N,
		[SDL_Keycode.SDLK_O] = ImGuiKey.O,
		[SDL_Keycode.SDLK_P] = ImGuiKey.P,
		[SDL_Keycode.SDLK_Q] = ImGuiKey.Q,
		[SDL_Keycode.SDLK_R] = ImGuiKey.R,
		[SDL_Keycode.SDLK_S] = ImGuiKey.S,
		[SDL_Keycode.SDLK_T] = ImGuiKey.T,
		[SDL_Keycode.SDLK_U] = ImGuiKey.U,
		[SDL_Keycode.SDLK_V] = ImGuiKey.V,
		[SDL_Keycode.SDLK_W] = ImGuiKey.W,
		[SDL_Keycode.SDLK_X] = ImGuiKey.X,
		[SDL_Keycode.SDLK_Y] = ImGuiKey.Y,
		[SDL_Keycode.SDLK_Z] = ImGuiKey.Z,
		[SDL_Keycode.SDLK_F1] = ImGuiKey.F1,
		[SDL_Keycode.SDLK_F2] = ImGuiKey.F2,
		[SDL_Keycode.SDLK_F3] = ImGuiKey.F3,
		[SDL_Keycode.SDLK_F4] = ImGuiKey.F4,
		[SDL_Keycode.SDLK_F5] = ImGuiKey.F5,
		[SDL_Keycode.SDLK_F6] = ImGuiKey.F6,
		[SDL_Keycode.SDLK_F7] = ImGuiKey.F7,
		[SDL_Keycode.SDLK_F8] = ImGuiKey.F8,
		[SDL_Keycode.SDLK_F9] = ImGuiKey.F9,
		[SDL_Keycode.SDLK_F10] = ImGuiKey.F10,
		[SDL_Keycode.SDLK_F11] = ImGuiKey.F11,
		[SDL_Keycode.SDLK_F12] = ImGuiKey.F12,
		[SDL_Keycode.SDLK_F13] = ImGuiKey.F13,
		[SDL_Keycode.SDLK_F14] = ImGuiKey.F14,
		[SDL_Keycode.SDLK_F15] = ImGuiKey.F15,
		[SDL_Keycode.SDLK_F16] = ImGuiKey.F16,
		[SDL_Keycode.SDLK_F17] = ImGuiKey.F17,
		[SDL_Keycode.SDLK_F18] = ImGuiKey.F18,
		[SDL_Keycode.SDLK_F19] = ImGuiKey.F19,
		[SDL_Keycode.SDLK_F20] = ImGuiKey.F20,
		[SDL_Keycode.SDLK_F21] = ImGuiKey.F21,
		[SDL_Keycode.SDLK_F22] = ImGuiKey.F22,
		[SDL_Keycode.SDLK_F23] = ImGuiKey.F23,
		[SDL_Keycode.SDLK_F24] = ImGuiKey.F24,
		[SDL_Keycode.SDLK_AC_BACK] = ImGuiKey.AppBack,
		[SDL_Keycode.SDLK_AC_FORWARD] = ImGuiKey.AppForward,
	}.ToFrozenDictionary();

	private static readonly ReadOnlyMemory<byte> _natoSansMonoFont;

	static ImGuiWindow()
	{
		SDL_SetHint(SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
		SDL_SetHint(SDL_HINT_MOUSE_AUTO_CAPTURE, "0");
		SDL_SetHint(SDL_HINT_IME_IMPLEMENTED_UI, "0");
		SDL_SetHint(SDL_HINT_ORIENTATIONS, "LandscapeLeft LandscapeRight");
		SDL_SetHint(SDL_HINT_ENABLE_SCREEN_KEYBOARD, "0");
#if GSE_LINUX
		// Prefer x11, as wayland is more problematic 
		SDL_SetHint(SDL_HINT_VIDEO_DRIVER, "x11,wayland");
#endif

		_sdlCursors[(int)ImGuiMouseCursor.Arrow] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT);
		_sdlCursors[(int)ImGuiMouseCursor.TextInput] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_TEXT);
		_sdlCursors[(int)ImGuiMouseCursor.ResizeAll] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_MOVE);
		_sdlCursors[(int)ImGuiMouseCursor.ResizeNS] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_NS_RESIZE);
		_sdlCursors[(int)ImGuiMouseCursor.ResizeEW] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_EW_RESIZE);
		_sdlCursors[(int)ImGuiMouseCursor.ResizeNESW] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_NESW_RESIZE);
		_sdlCursors[(int)ImGuiMouseCursor.ResizeNWSE] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_NWSE_RESIZE);
		_sdlCursors[(int)ImGuiMouseCursor.Hand] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_POINTER);
		_sdlCursors[(int)ImGuiMouseCursor.NotAllowed] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_NOT_ALLOWED);

		using var notoSansMono = typeof(ImGuiWindow).Assembly
			.GetManifestResourceStream($"GSE.res.NotoSansMono-Medium.ttf")!;
		var font = new byte[notoSansMono.Length];
		notoSansMono.ReadExactly(font, 0, font.Length);
		_natoSansMonoFont = new(font);
	}

	private readonly nint _imGuiContext;
	private GCHandle _imGuiUserData;
	private readonly SDLTexture _fontSdlTexture;
	private readonly bool _isOverridingScale;
	private float _dpiScale;

	public readonly nint SdlWindow;
	public readonly uint WindowId;

	public readonly SDLRenderer SdlRenderer;
	private SDL_FColor[] _sdlColorBuffer = [];

	public readonly uint SdlWindowProperties;

	private int _lastWidth, _lastHeight, _lastScale, _lastBars;
	public bool IsFullscreen { get; private set; }

	private static readonly ulong _perfFreq = SDL_GetPerformanceFrequency();
	private ulong _lastTime;

	private readonly bool _mouseCanUseGlobalState;
	private uint _mouseButtonsDown;
	private int _mouseLeavePending;
	private nint _lastMouseCursor;

	private nint ClipboardText;

	[UnmanagedCallersOnly(CallConvs = [ typeof(CallConvCdecl) ])]
	private static nint GetClipboardText(nint userdata)
	{
		var window = (ImGuiWindow)GCHandle.FromIntPtr(userdata).Target!;
		SDL_free(window.ClipboardText);
		window.ClipboardText = SDL_GetClipboardText_RAW();
		return window.ClipboardText;
	}

	[UnmanagedCallersOnly(CallConvs = [ typeof(CallConvCdecl) ])]
	private static void SetClipboardText(nint userdata, nint text)
	{
		SDL_SetClipboardText(text);
	}

	[UnmanagedCallersOnly(CallConvs = [ typeof(CallConvCdecl) ])]
	private static unsafe void PlatformSetImeData(nint imGuiContext, ImGuiViewport* viewport, ImGuiPlatformImeData* data)
	{
		if (viewport->PlatformUserData == null)
		{
			return;
		}

		var window = (ImGuiWindow)GCHandle.FromIntPtr((nint)viewport->PlatformUserData).Target!;
		if (data->WantVisible == 0)
		{
			SDL_StopTextInput(window.SdlWindow);
		}
		else
		{
			SDL_Rect rect;
			rect.x = (int)data->InputPos.X;
			rect.y = (int)data->InputPos.Y;
			rect.w = 1;
			rect.h = (int)data->InputLineHeight;
			SDL_SetTextInputArea(window.SdlWindow, ref rect, 0);
			SDL_StartTextInput(window.SdlWindow);
		}
	}

	public const string DEFAULT_RENDER_DRIVER = "[Default Render Driver]";
	private const string SOFTWARE_RENDER_DRIVER = "software"; // hopefully SDL doesn't change this!

	public static readonly FrozenDictionary<string, string> RenderDriverFriendlyNameMap = new Dictionary<string, string>
	{
		["direct3d"] = "Direct3D 9",
		["direct3d11"] = "Direct3D 11",
		["direct3d12"] = "Direct3D 12",
		["metal"] = "Metal",
		["opengl"] = "OpenGL 2.1",
		["opengles2"] = "OpenGL ES 2.0",
		["gpu"] = "SDL3 GPU",
		["vulkan"] = "Vulkan",
		["software"] = "Software",
	}.ToFrozenDictionary();

	/// <summary>
	/// All render drivers available, should only be accessed from the GUI thread
	/// Index of the string will also match up with the index passed for SDL_CreateRenderer
	/// </summary>
	public static readonly Lazy<ImmutableArray<string>> RenderDrivers = new(() =>
	{
		// we don't really need to check renderer feature support here, as SDL is quite generous with fallbacks
		// in case vsync is not supported (e.g. software rendering), vsync will be "simulated"
		// if a texture format isn't natively supported, SDL will internally do conversions to the closest supported native format
		var renderDrivers = new string[SDL_GetNumRenderDrivers()];
		for (var i = 0; i < renderDrivers.Length; i++)
		{
			renderDrivers[i] = SDL_GetRenderDriver(i);
		}

		return [..renderDrivers];
	});

	private static nint CreateSdlRenderer(nint sdlWindow, Config config)
	{
		var friendlyRenderDriverName = RenderDriverFriendlyNameMap.GetValueOrDefault(config.RenderDriver, config.RenderDriver);
		if (config.RenderDriver != DEFAULT_RENDER_DRIVER)
		{
			var renderDrivers = RenderDrivers.Value;
			if (!renderDrivers.Contains(config.RenderDriver))
			{
				_ = SDL_ShowSimpleMessageBox(
					flags: SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING,
					title: "Warning",
					message: $"{friendlyRenderDriverName} was not in list of available render drivers, falling back on default render driver.",
					window: sdlWindow
				);

				config.RenderDriver = DEFAULT_RENDER_DRIVER;
			}
		}

		var defaultRenderDriverFailed = false;
		while (true)
		{
			var renderDriver = config.RenderDriver == DEFAULT_RENDER_DRIVER ? null : config.RenderDriver;
			var sdlRenderer = SDL_CreateRenderer(sdlWindow, renderDriver);
			if (sdlRenderer == 0)
			{
				if (renderDriver != null)
				{
					if (renderDriver == SOFTWARE_RENDER_DRIVER && defaultRenderDriverFailed)
					{
						// failed to create the default render driver and software renderer, somehow
						return 0;
					}

					_ = SDL_ShowSimpleMessageBox(
						flags: SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING,
						title: "Warning",
						message: $"{friendlyRenderDriverName} render driver could not be created, falling back on default render driver.",
						window: sdlWindow
					);

					config.RenderDriver = DEFAULT_RENDER_DRIVER;
				}
				else
				{
					_ = SDL_ShowSimpleMessageBox(
						flags: SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING,
						title: "Warning",
						message: "Default render driver could not be created, falling back on software render driver.",
						window: sdlWindow
					);

					config.RenderDriver = SOFTWARE_RENDER_DRIVER;
					defaultRenderDriverFailed = true;
				}

				continue;
			}

			return sdlRenderer;
		}
	}

	private float GetDpiScale()
	{
		var displayScale = SDL_GetWindowDisplayScale(SdlWindow);
		if (!float.IsNormal(displayScale) || float.IsNegative(displayScale))
		{
			return 1;
		}

		return displayScale;
	}

	private void SetFontTexture()
	{
		ImGui.SetCurrentContext(_imGuiContext);
		var io = ImGui.GetIO();
		io.Fonts.GetTexDataAsRGBA32(out nint pixels, out var width, out var height, out var bytesPerPixel);
		_fontSdlTexture.UpdateTexture(width, height, pixels, width * bytesPerPixel);
		io.Fonts.SetTexID(_fontSdlTexture.TextureId);
	}

	private void SetFont(float scaleFactor)
	{
		var io = ImGui.GetIO();
		ImFontConfigPtr fontConfig;
		unsafe
		{
			fontConfig = ImGuiNative.ImFontConfig_ImFontConfig();
			if (fontConfig.NativePtr == null)
			{
				throw new("Failed to allocate font config");
			}
		}

		fontConfig.OversampleH = fontConfig.OversampleV = 1;
		fontConfig.PixelSnapH = true;
		fontConfig.SizePixels = (float)Math.Round(16 * scaleFactor, MidpointRounding.AwayFromZero);
		fontConfig.FontDataOwnedByAtlas = false;

		io.Fonts.Clear();
		unsafe
		{
			fixed (byte* fontPtr = _natoSansMonoFont.Span)
			{
				io.Fonts.AddFontFromMemoryTTF((nint)fontPtr, _natoSansMonoFont.Length, 0, fontConfig);
			}
		}

		fontConfig.Destroy();
		SetFontTexture();
	}

	public ImGuiWindow(string windowName, Config config)
	{
		if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_EVENTS))
		{
			throw new($"Could not init SDL video! SDL error: {SDL_GetError()}");
		}

		try
		{
			const SDL_WindowFlags windowFlags = SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY | SDL_WindowFlags.SDL_WINDOW_HIDDEN;
			SdlWindow = SDL_CreateWindow(windowName, 64, 64, windowFlags);
			if (SdlWindow == 0)
			{
				throw new($"Could not create SDL window! SDL error: {SDL_GetError()}");
			}

			WindowId = SDL_GetWindowID(SdlWindow);

			var sdlRenderer = CreateSdlRenderer(SdlWindow, config);
			if (sdlRenderer == 0)
			{
				throw new($"Could not create SDL renderer! SDL error: {SDL_GetError()}");
			}

			SdlRenderer = new(sdlRenderer);
			_fontSdlTexture = new(SdlRenderer, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888,
				SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC, SDL_ScaleMode.SDL_SCALEMODE_LINEAR, SDL_BLENDMODE_BLEND, SetFontTexture);

			SdlWindowProperties = SDL_GetWindowProperties(SdlWindow);

#if GSE_WINDOWS
			// disable the window icon
			SetWindowIconEnabled(false);
			// disable windows 11 round corners, if the user wants that
			SetWin11CornerPreference(config.DisableWin11RoundCorners);
#endif

#if GSE_ANDROID
			// android is always fullscreen (don't let the user toggle this!)
			ToggleFullscreen(config);
#endif

			var videoDriver = SDL_GetCurrentVideoDriver();
			_mouseCanUseGlobalState = videoDriver is "windows" or "cocoa" or "x11" or "DIVE" or "VMAN";

			_imGuiContext = ImGui.CreateContext();
			if (_imGuiContext == 0)
			{
				throw new("Failed to create ImGui context!");
			}

			ImGui.SetCurrentContext(_imGuiContext);
			var io = ImGui.GetIO();
			io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

			unsafe
			{
				// disable imgui's ini
				// for whatever reason this isn't exposed in the safe wrapper :(
				io.NativePtr->IniFilename = null;
			}

			_imGuiUserData = GCHandle.Alloc(this, GCHandleType.Weak);
			io.BackendPlatformUserData = GCHandle.ToIntPtr(_imGuiUserData);
			io.BackendRendererUserData = io.BackendPlatformUserData;
			io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors | ImGuiBackendFlags.HasSetMousePos | ImGuiBackendFlags.RendererHasVtxOffset;

			var mainViewport = ImGui.GetMainViewport();
			mainViewport.PlatformUserData = io.BackendPlatformUserData;

			unsafe
			{
				io.ClipboardUserData = io.BackendPlatformUserData;
				io.GetClipboardTextFn = (nint)(delegate* unmanaged[Cdecl]<nint, nint>)&GetClipboardText;
				io.SetClipboardTextFn = (nint)(delegate* unmanaged[Cdecl]<nint, nint, void>)&SetClipboardText;
				io.PlatformSetImeDataFn = (nint)(delegate* unmanaged[Cdecl]<nint, ImGuiViewport*, ImGuiPlatformImeData*, void>)&PlatformSetImeData;
			}

			_dpiScale = GetDpiScale();

			var scaleOverride = Environment.GetEnvironmentVariable("GSE_SCALE");
			if (float.TryParse(scaleOverride, out var scaleFactor))
			{
				_isOverridingScale = true;
			}
			else
			{
				scaleFactor = _dpiScale;
			}

			SetTheme(config.DarkMode);

			var style = ImGui.GetStyle();
			style.ScaleAllSizes(scaleFactor);
			SetFont(scaleFactor);

			// make sure we have a frame border
			// this doesn't matter too much in dark mode
			// but without a border light mode is terrible
			style.FrameBorderSize = 1;

			// calling NewFrame isn't valid until this is set
			// just set it to 0 for now
			io.DisplaySize = new(0, 0);

			// this must be done in order to scaling to properly take effect for e.g. GetFrameHeight() calls
			ImGui.NewFrame();
			ImGui.EndFrame();
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	public void Dispose()
	{
		if (_imGuiContext != 0)
		{
			ImGui.SetCurrentContext(_imGuiContext);

			// ImGui wants userdata fields to be cleared before destroying the context
			var io = ImGui.GetIO();
			io.BackendPlatformUserData = 0;
			io.BackendRendererUserData = 0;
			io.ClipboardUserData = 0;

			var mainViewport = ImGui.GetMainViewport();
			mainViewport.PlatformUserData = 0;

			ImGui.DestroyContext(_imGuiContext);
		}

		if (_imGuiUserData.IsAllocated)
		{
			_imGuiUserData.Free();
		}

		SDL_free(ClipboardText);

		_fontSdlTexture?.Dispose();
		SdlRenderer?.Dispose();

#if GSE_WINDOWS
		if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763) &&
		    !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362))
		{
			var hwnd = SDL_GetPointerProperty(SdlWindowProperties, SDL_PROP_WINDOW_WIN32_HWND_POINTER, 0);
			if (hwnd != 0)
			{
				unsafe
				{
					fixed (char* useImmersiveDarkModeColors = "UseImmersiveDarkModeColors")
					{
						_ = PInvoke.RemoveProp(new(hwnd), useImmersiveDarkModeColors);
					}
				}
			}
		}
#endif

		SDL_DestroyWindow(SdlWindow);

		SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_EVENTS);
	}

	public void ToggleFullscreen(Config config)
	{
		// TODO: how should failure be handled?
		if (!SDL_SetWindowFullscreen(SdlWindow, !IsFullscreen))
		{
			return;
		}

		IsFullscreen = !IsFullscreen;

		if (!IsFullscreen)
		{
			SetWindowSize(_lastWidth, _lastHeight, _lastScale, _lastBars);
		}

#if GSE_WINDOWS
		// the hack used to disable the window icon will cause fullscreen to have a noticeable border
		// so re-enable the window icon if we're fullscreen (the user won't be seeing the icon anyways)
		SetWindowIconEnabled(IsFullscreen);
		if (!IsFullscreen)
		{
			// SDL mucks with how win11 corner preference when toggling fullscreen, ensure it's properly set
			SetWin11CornerPreference(config.DisableWin11RoundCorners);
		}
#endif
	}

	/// <summary>
	/// Only for main window usage
	/// </summary>
	public void UpdateMainWindowSize(EmuManager emuManager, Config config)
	{
		var (emuWidth, emuHeight) = emuManager.GetVideoDimensions(config.HideSgbBorder);
		var numBars = config.HideStatusBar ? 1 : 2;
		if (config.HideMenuBarOnUnpause &&
		    emuManager.EmuAcceptingInputs &&
		    config.HotkeyBindings.PauseButtonBindings.Count != 0)
		{
			numBars--;
		}

		SetWindowSize(emuWidth, emuHeight, config.WindowScale, numBars);
	}

	public void SetWindowSize(int w, int h, int scale, int bars)
	{
		_lastWidth = w;
		_lastHeight = h;
		_lastScale = scale;
		_lastBars = bars;
		if (!IsFullscreen)
		{
			w *= scale;
			h *= scale;
			h += (int)(ImGui.GetFrameHeight() * bars);
			// we want to adjust our width/height to match up against the renderer output
			// as imgui coords go against the dpi scaled size, not the window size
			SdlRenderer.GetCurrentRenderOutputSize(out var displayW, out var displayH);
			SDL_GetWindowSize(SdlWindow, out var lastWindowWidth, out var lastWindowHeight);
			w = w * lastWindowWidth / displayW;
			h = h * lastWindowHeight / displayH;
			SDL_SetWindowSize(SdlWindow, w, h);
		}
	}

	public void SetWindowPos(int x, int y)
	{
		SDL_SetWindowPosition(SdlWindow, x, y);
	}

	public void SetAlwaysOnTop(bool alwaysOnTop)
	{
		SDL_SetWindowAlwaysOnTop(SdlWindow, alwaysOnTop);
	}

	public void SetResizable(bool resizable)
	{
		SDL_SetWindowResizable(SdlWindow, resizable);
	}

	public void SetVisible(bool makeVisible)
	{
		if (makeVisible)
		{
			SDL_ShowWindow(SdlWindow);
		}
		else
		{
			SDL_HideWindow(SdlWindow);
		}
	}

#if GSE_WINDOWS
	public void SetWin11CornerPreference(bool doNotRound)
	{
		// windows 11 is windows 10 build 22000 and above
		if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
		{
			unsafe
			{
				var hwnd = SDL_GetPointerProperty(SdlWindowProperties, SDL_PROP_WINDOW_WIN32_HWND_POINTER, 0);
				var cornerPref = doNotRound ? DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DONOTROUND : DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DEFAULT;
				_ = PInvoke.DwmSetWindowAttribute(new(hwnd), DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, &cornerPref, sizeof(DWM_WINDOW_CORNER_PREFERENCE));
			}

			// don't know if this is needed, but if it's like the title bar, it probably is
			if (!IsFullscreen)
			{
				var windowFlags = SDL_GetWindowFlags(SdlWindow);
				if ((windowFlags & SDL_WindowFlags.SDL_WINDOW_HIDDEN) == 0 &&
				    (windowFlags & SDL_WindowFlags.SDL_WINDOW_MINIMIZED) == 0)
				{
					SDL_HideWindow(SdlWindow);
					SDL_ShowWindow(SdlWindow);
				}
			}
		}
	}

	private void SetWindowIconEnabled(bool enable)
	{
		var hwnd = SDL_GetPointerProperty(SdlWindowProperties, SDL_PROP_WINDOW_WIN32_HWND_POINTER, 0);
		var curStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLong(new(hwnd), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
		if (enable)
		{
			curStyle &= ~WINDOW_EX_STYLE.WS_EX_DLGMODALFRAME;
		}
		else
		{
			// kind of a hack to get rid of the window icon
			curStyle |= WINDOW_EX_STYLE.WS_EX_DLGMODALFRAME;
		}

		_ = PInvoke.SetWindowLong(new(hwnd), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)curStyle);
		_ = PInvoke.SetWindowPos(new(hwnd), HWND.HWND_TOP, 0, 0, 0, 0,
			SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);
		unsafe
		{
			_ = PInvoke.RedrawWindow(new(hwnd), null, HRGN.Null, REDRAW_WINDOW_FLAGS.RDW_INVALIDATE | REDRAW_WINDOW_FLAGS.RDW_FRAME);
		}
	}

	private void SetTitleBarTheme(bool dark)
	{
		// set dark title bar if the windows version is new enough
		// this mode technically isn't supported until windows 11
		// but unofficially it was available in windows 10
		if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
		{
			unsafe
			{
				var hwnd = SDL_GetPointerProperty(SdlWindowProperties, SDL_PROP_WINDOW_WIN32_HWND_POINTER, 0);
				BOOL darkTitleBar = dark;
				if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18985))
				{
					// windows 10 20H1 and above has the same documented dark mode api
					_ = PInvoke.DwmSetWindowAttribute(new(hwnd), DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, &darkTitleBar, (uint)sizeof(BOOL));
				}
				else
				{
					// before windows 10 1903, the UseImmersiveDarkModeColors window property needed to be set in order to use the unofficial dark mode api
					if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362))
					{
						fixed (char* useImmersiveDarkModeColors = "UseImmersiveDarkModeColors")
						{
							_ = PInvoke.SetProp(new(hwnd), useImmersiveDarkModeColors, new(darkTitleBar));
						}
					}

					// before windows 10 20H1 the DWMWA_USE_IMMERSIVE_DARK_MODE flag was -1 the final value
					_ = PInvoke.DwmSetWindowAttribute(new(hwnd), DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE - 1, &darkTitleBar, (uint)sizeof(BOOL));
				}

				if (!IsFullscreen)
				{
					var windowFlags = SDL_GetWindowFlags(SdlWindow);
					if ((windowFlags & SDL_WindowFlags.SDL_WINDOW_HIDDEN) == 0 &&
					    (windowFlags & SDL_WindowFlags.SDL_WINDOW_MINIMIZED) == 0)
					{
						// this seems to be the only way to programmatically force refresh the title bar
						SDL_HideWindow(SdlWindow);
						SDL_ShowWindow(SdlWindow);
					}
				}
			}
		}
	}
#endif

	public void SetTheme(bool dark)
	{
		ImGui.SetCurrentContext(_imGuiContext);
#if GSE_WINDOWS
		SetTitleBarTheme(dark);
#endif
		if (dark)
		{
			ImGui.StyleColorsDark();
		}
		else
		{
			ImGui.StyleColorsLight();
		}
	}

	/// <summary>
	/// HACK to prevent Escape closing the input binding popup
	/// </summary>
	public bool SuppressEscape { get; set; }

	public void ProcessEvent(in SDL_Event e)
	{
		ImGui.SetCurrentContext(_imGuiContext);
		var io = ImGui.GetIO();
		var xScale = io.DisplayFramebufferScale.X < 0.01 ? 1 : io.DisplayFramebufferScale.X;
		var yScale = io.DisplayFramebufferScale.Y < 0.01 ? 1 : io.DisplayFramebufferScale.Y;
		// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
		switch ((SDL_EventType)e.type)
		{
			case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
				io.AddMouseSourceEvent(e.motion.which == SDL_TOUCH_MOUSEID ? ImGuiMouseSource.TouchScreen : ImGuiMouseSource.Mouse);
				io.AddMousePosEvent(e.motion.x * xScale, e.motion.y * yScale);
				break;
			case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
				io.AddMouseSourceEvent(e.wheel.which == SDL_TOUCH_MOUSEID ? ImGuiMouseSource.TouchScreen : ImGuiMouseSource.Mouse);
				io.AddMouseWheelEvent(e.wheel.x, e.wheel.y);
				break;
			case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
			case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
			{
				var mouseButton = (uint)e.button.button switch
				{
					SDL_BUTTON_LEFT => 0,
					SDL_BUTTON_RIGHT => 1,
					SDL_BUTTON_MIDDLE => 2,
					SDL_BUTTON_X1 => 3,
					SDL_BUTTON_X2 => 4,
					_ => -1,
				};

				if (mouseButton == -1)
				{
					break;
				}

				io.AddMouseSourceEvent(e.button.which == SDL_TOUCH_MOUSEID ? ImGuiMouseSource.TouchScreen : ImGuiMouseSource.Mouse);
				if ((SDL_EventType)e.type == SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN)
				{
					io.AddMouseButtonEvent(mouseButton, true);
					_mouseButtonsDown |= 1u << mouseButton;
				}
				else
				{
					io.AddMouseButtonEvent(mouseButton, false);
					_mouseButtonsDown &= ~(1u << mouseButton);
				}

				break;
			}
			case SDL_EventType.SDL_EVENT_TEXT_INPUT:
				unsafe
				{
					ImGuiNative.ImGuiIO_AddInputCharactersUTF8(io.NativePtr, e.text.text);
					break;
				}
			case SDL_EventType.SDL_EVENT_KEY_DOWN:
			case SDL_EventType.SDL_EVENT_KEY_UP:
			{
				if (SuppressEscape && (SDL_Keycode)e.key.key == SDL_Keycode.SDLK_ESCAPE)
				{
					break;
				}

				var mods = e.key.mod;
				io.AddKeyEvent(ImGuiKey.ModCtrl, (mods & SDL_Keymod.SDL_KMOD_CTRL) != 0);
				io.AddKeyEvent(ImGuiKey.ModShift, (mods & SDL_Keymod.SDL_KMOD_SHIFT) != 0);
				io.AddKeyEvent(ImGuiKey.ModAlt, (mods & SDL_Keymod.SDL_KMOD_ALT) != 0);
				io.AddKeyEvent(ImGuiKey.ModSuper, (mods & SDL_Keymod.SDL_KMOD_GUI) != 0);

				var key = _imGuiMap.GetValueOrDefault((SDL_Keycode)e.key.key, ImGuiKey.None);
				io.AddKeyEvent(key, (SDL_EventType)e.type == SDL_EventType.SDL_EVENT_KEY_DOWN);
				io.SetKeyEventNativeData(key, (int)e.key.key, (int)e.key.scancode, (int)e.key.scancode);
				break;
			}
			case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER:
			case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
				_mouseLeavePending = (SDL_EventType)e.type == SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER ? 0 : 2;
				break;
			case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
			case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
				io.AddFocusEvent((SDL_EventType)e.type == SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED);
				break;
		}
	}

	public void NewFrame()
	{
		ImGui.SetCurrentContext(_imGuiContext);
		var io = ImGui.GetIO();

		if (!_isOverridingScale)
		{
			var dpiScale = GetDpiScale();
			if (Math.Abs(_dpiScale - dpiScale) > 0.01)
			{
				unsafe
				{
					var style = new ImGuiStylePtr(ImGuiNative.ImGuiStyle_ImGuiStyle());
					if (style.NativePtr == null)
					{
						throw new("Failed to allocate default style");
					}

					// copy default style to current style
					var curStyle = ImGui.GetStyle();
					// make sure to not override the colors
					new Span<Vector4>(curStyle.Colors.Data, curStyle.Colors.Count)
						.CopyTo(new(style.Colors.Data, style.Colors.Count));
					*curStyle.NativePtr = *style.NativePtr;
					style.Destroy();

					// scale up
					curStyle.ScaleAllSizes(dpiScale);
					// make sure we have the frame border
					curStyle.FrameBorderSize = 1;
				}

				SetFont(dpiScale);
				_dpiScale = dpiScale;

				// get ImGui.GetFrameHeight() up to date (important for SetWindowSize)
				ImGui.NewFrame();
				ImGui.EndFrame();

				SetWindowSize(_lastWidth, _lastHeight, _lastScale, _lastBars);
			}
		}

		SDL_GetWindowSize(SdlWindow, out var w, out var h);
		var windowFlags = SDL_GetWindowFlags(SdlWindow);
		if ((windowFlags & SDL_WindowFlags.SDL_WINDOW_MINIMIZED) != 0)
		{
			w = h = 0;
		}

		SdlRenderer.GetCurrentRenderOutputSize(out var displayW, out var displayH);
		io.DisplaySize = new(displayW, displayH);
		if (w > 0 && h > 0)
		{
			io.DisplayFramebufferScale = new((float)displayW / w, (float)displayH / h);
		}

		var time = SDL_GetPerformanceCounter();
		if (time <= _lastTime)
		{
			time = _lastTime + 1;
		}

		io.DeltaTime = time > 0 ? (float)((double)(time - _lastTime) / _perfFreq) : 1 / 60f;
		_lastTime = time;

		if (_mouseLeavePending != 0)
		{
			_mouseLeavePending--;
			if (_mouseLeavePending == 0)
			{
				io.AddMousePosEvent(float.MinValue, float.MinValue);
			}
		}

		var mouseCaptureSupported = SDL_CaptureMouse(_mouseButtonsDown != 0);
		var windowFocused = mouseCaptureSupported
			? SDL_GetKeyboardFocus() == SdlWindow
			: (windowFlags & SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS) != 0;
		if (windowFocused)
		{
			if (io.WantSetMousePos)
			{
				SDL_WarpMouseInWindow(SdlWindow, (int)io.MousePos.X, (int)io.MousePos.Y);
			}

			if (_mouseCanUseGlobalState && _mouseButtonsDown == 0)
			{
				_ = SDL_GetGlobalMouseState(out var mouseXGlobal, out var mouseYGlobal);
				SDL_GetWindowPosition(SdlWindow, out var windowX, out var windowY);
				var xScale = io.DisplayFramebufferScale.X < 0.01 ? 1 : io.DisplayFramebufferScale.X;
				var yScale = io.DisplayFramebufferScale.Y < 0.01 ? 1 : io.DisplayFramebufferScale.Y;
				io.AddMousePosEvent((mouseXGlobal - windowX) * xScale, (mouseYGlobal - windowY) * yScale);
			}
		}

		var imGuiMouseCursor = ImGui.GetMouseCursor();
		if (io.MouseDrawCursor || imGuiMouseCursor == ImGuiMouseCursor.None)
		{
			_ = SDL_HideCursor();
		}
		else
		{
			var sdlCursor = _sdlCursors[(int)imGuiMouseCursor];
			if (sdlCursor == 0)
			{
				sdlCursor = _sdlCursors[(int)ImGuiMouseCursor.Arrow];
			}

			if (sdlCursor != _lastMouseCursor)
			{
				SDL_SetCursor(sdlCursor);
				_lastMouseCursor = sdlCursor;
			}

			_ = SDL_ShowCursor();
		}

		ImGui.NewFrame();
	}

	/// <summary>
	/// Helper ref struct for saving render state in an RAII style
	/// </summary>
	private ref struct SDLSaveRenderStateWrapper
	{
		private readonly SDLRenderer _sdlRenderer;
		private readonly bool _clipEnabled;
		private SDL_Rect _viewport;
		private SDL_Rect _clipRect;

		public SDLSaveRenderStateWrapper(SDLRenderer sdlRenderer)
		{
			_sdlRenderer = sdlRenderer;
			_clipEnabled = _sdlRenderer.RenderClipEnabled();
			_sdlRenderer.GetRenderViewport(out _viewport);
			_sdlRenderer.GetRenderClipRect(out _clipRect);
		}

		public void Dispose()
		{
			_sdlRenderer.SetRenderViewport(ref _viewport);
			if (_clipEnabled)
			{
				_sdlRenderer.SetRenderClipRect(ref _clipRect);
			}
			else
			{
				_sdlRenderer.SetRenderClipRect(ref Unsafe.NullRef<SDL_Rect>());
			}
		}
	}

	// annoying conversion that needs to be done for SDL3's SDL_Renderer
	private ReadOnlySpan<SDL_FColor> ConvertColors(ReadOnlySpan<ImDrawVert> verts)
	{
		if (_sdlColorBuffer.Length < verts.Length)
		{
			_sdlColorBuffer = new SDL_FColor[verts.Length];
		}

		for (var i = 0; i < verts.Length; i++)
		{
			var packedColor = verts[i].col;
			SDL_FColor color;
			color.r = (packedColor & 0xFF) / 255.0f;
			color.g = ((packedColor >> 8) & 0xFF) / 255.0f;
			color.b = ((packedColor >> 16) & 0xFF) / 255.0f;
			color.a = ((packedColor >> 24) & 0xFF) / 255.0f;
			_sdlColorBuffer[i] = color;
		}

		return _sdlColorBuffer.AsSpan(0, verts.Length);
	}

	public void Render(bool vsync)
	{
		ImGui.SetCurrentContext(_imGuiContext);
		ImGui.Render();

		SdlRenderer.SetRenderDrawColor(0x80, 0x80, 0x80, 0xFF);
		SdlRenderer.RenderClear();

		var drawData = ImGui.GetDrawData();

		var fbWidth = (int)drawData.DisplaySize.X;
		var fbHeight = (int)drawData.DisplaySize.Y;
		if (fbWidth <= 0 || fbHeight <= 0)
		{
			Thread.Sleep(5);
			return;
		}

		using (new SDLSaveRenderStateWrapper(SdlRenderer))
		{
			SdlRenderer.SetRenderViewport(ref Unsafe.NullRef<SDL_Rect>());
			SdlRenderer.SetRenderClipRect(ref Unsafe.NullRef<SDL_Rect>());

			var clipOff = drawData.DisplayPos;
			for (var i = 0; i < drawData.CmdListsCount; i++)
			{
				var cmdList = drawData.CmdLists[i];
				var vtxBuffer = cmdList.VtxBuffer.Data;
				var idxBuffer = cmdList.IdxBuffer.Data;

				for (var j = 0; j < cmdList.CmdBuffer.Size; j++)
				{
					var cmd = cmdList.CmdBuffer[j];
					if (cmd.UserCallback != 0)
					{
						throw new NotSupportedException("User callbacks are not supported in this ImGui implementation");
					}

					var clipMin = new Vector2(Math.Max(cmd.ClipRect.X - clipOff.X, 0), Math.Max(cmd.ClipRect.Y - clipOff.Y, 0));
					var clipMax = new Vector2(Math.Min(cmd.ClipRect.Z - clipOff.X, fbWidth), Math.Min(cmd.ClipRect.W - clipOff.Y, fbHeight));
					if (clipMax.X <= clipMin.X || clipMax.Y <= clipMin.Y)
					{
						continue;
					}

					var r = new SDL_Rect
					{
						x = (int)clipMin.X,
						y = (int)clipMin.Y,
						w = (int)(clipMax.X - clipMin.X),
						h = (int)(clipMax.Y - clipMin.Y)
					};
					SdlRenderer.SetRenderClipRect(ref r);

					unsafe
					{
						var vtx = (ImDrawVert*)(vtxBuffer + cmd.VtxOffset * sizeof(ImDrawVert));
						var colors = ConvertColors(new(vtx, (int)(cmdList.VtxBuffer.Size - cmd.VtxOffset)));
						var texId = cmd.GetTexID();
						fixed (SDL_FColor* colorsPtr = colors)
						{
							SdlRenderer.RenderGeometryRaw(
								textureId: texId,
								xy: (nint)(&vtx->pos),
								xy_stride: sizeof(ImDrawVert),
								color: (nint)colorsPtr,
								color_stride: sizeof(SDL_FColor),
								uv: (nint)(&vtx->uv),
								uv_stride: sizeof(ImDrawVert),
								num_vertices: (int)(cmdList.VtxBuffer.Size - cmd.VtxOffset),
								indices: (nint)((nuint)idxBuffer + cmd.IdxOffset * sizeof(ushort)),
								num_indices: (int)cmd.ElemCount,
								size_indices: sizeof(ushort)
							);
						}
					}
				}
			}
		}

		SdlRenderer.SetRenderVSync(vsync);
		SdlRenderer.RenderPresent();
	}
}
