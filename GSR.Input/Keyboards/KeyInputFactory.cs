using System;

using static SDL2.SDL;

namespace GSR.Input.Keyboards;

internal static class KeyInputFactory
{
	public static IKeyInput CreateKeyInput(in SDL_SysWMinfo mainWindowWmInfo)
	{
#if GSR_WINDOWS
		if (!OperatingSystem.IsWindowsVersionAtLeast(6, 0, 6000))
		{
			throw new NotSupportedException("Windows key input requires at least Windows Vista");
		}

		return new RawKeyInput();
#endif

#if GSR_OSX
		return new QuartzKeyInput();
#endif

#if GSR_LINUX
		switch (mainWindowWmInfo.subsystem)
		{
			// if we're using a wayland window, we must use wayland apis for key input
			// as wayland typically doesn't allow for x11 to do inputs unless xwayland or similar is used
			case SDL_SYSWM_TYPE.SDL_SYSWM_WAYLAND when WlImports.HasDisplay:
				return new WlKeyInput(mainWindowWmInfo.info.wl.display);
			// in this case, we're using XWayland, which would not allow background input with the X11 backend
			// we can still do background input however, if we have root access (and thus can use evdev directly)
			// we still of course want to grab a new wayland connection just to obtain a keymap
			case SDL_SYSWM_TYPE.SDL_SYSWM_X11 when WlImports.HasDisplay && LibcImports.HasRoot:
				return new WlKeyInput(IntPtr.Zero);
			case SDL_SYSWM_TYPE.SDL_SYSWM_X11 when X11Imports.HasDisplay:
				return new X11KeyInput();
			case SDL_SYSWM_TYPE.SDL_SYSWM_KMSDRM or SDL_SYSWM_TYPE.SDL_SYSWM_VIVANTE:
				// these subsystems just use evdev directly for keyboard input, and don't need root to use it
				return new EvDevKeyInput(false);
		}

		// assume that we need root to use evdev otherwise
		if (LibcImports.HasRoot)
		{
			return new EvDevKeyInput(true);
		}

		throw new NotSupportedException("Linux key input requires Wayland, X11, or rootless evdev");
#endif
	}
}
