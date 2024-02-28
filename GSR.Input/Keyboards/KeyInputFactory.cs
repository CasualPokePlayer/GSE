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
		// if we're using wayland windows, we must use wayland apis for key input
		// as wayland typically doesn't allow for x11 to do inputs unless xwayland or similar is used
		if (mainWindowWmInfo.subsystem == SDL_SYSWM_TYPE.SDL_SYSWM_WAYLAND)
		{
			if (WlImports.HasDisplay)
			{
				return new WlKeyInput(mainWindowWmInfo.info.wl.display);
			}
		}
		else
		{
			if (X11Imports.HasDisplay)
			{
				return new X11KeyInput();
			}
		}

		throw new NotSupportedException("Linux key input requires either X11 or Wayland");
#endif
	}
}
