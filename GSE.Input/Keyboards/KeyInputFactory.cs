// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#if GSE_LINUX
using System;
#endif

#if GSE_LINUX
using static SDL3.SDL;
#endif

namespace GSE.Input.Keyboards;

internal static class KeyInputFactory
{
	public static IKeyInput CreateKeyInput(uint mainWindowProperties)
	{
#if GSE_WINDOWS
		return new RawKeyInput();
#endif

#if GSE_OSX
		return new QuartzKeyInput();
#endif

#if GSE_LINUX
		switch (SDL_GetCurrentVideoDriver())
		{
			// if we're using a wayland window, we must use wayland apis for key input
			// as wayland typically doesn't allow for x11 to do inputs unless xwayland or similar is used
			case "wayland" when WlImports.HasDisplay:
				return new WlKeyInput(SDL_GetPointerProperty(mainWindowProperties, SDL_PROP_WINDOW_WAYLAND_DISPLAY_POINTER, 0));
			// in this case, we're using XWayland, which would not allow background input with the X11 backend
			// we can still do background input however, if we have root access (and thus can use evdev directly)
			// we still of course want to grab a new wayland connection just to obtain a keymap
			case "x11" when WlImports.HasDisplay && EvDevImports.HasEvDev && LibcImports.HasRoot:
				return new WlKeyInput(0);
			case "x11" when X11Imports.HasDisplay:
				return new X11KeyInput();
			case "evdev" or "kmsdrm" or "rpi" or "vivante" when EvDevImports.HasEvDev:
				// these video drivers just use evdev directly for keyboard input, and shouldn't need root to use it
				return new EvDevKeyInput();
		}

		// assume that we need root to use evdev otherwise
		if (LibcImports.HasRoot && EvDevImports.HasEvDev)
		{
			return new EvDevKeyInput();
		}

		throw new NotSupportedException("Linux key input requires Wayland, X11, or rootless evdev");
#endif

#if GSE_ANDROID
		return new AndroidKeyInput();
#endif
	}
}
