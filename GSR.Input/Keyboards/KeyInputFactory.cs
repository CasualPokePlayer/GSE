using System;

namespace GSR.Input.Keyboards;

internal static class KeyInputFactory
{
	public static IKeyInput CreateKeyInput()
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
		if (WlImports.HasDisplay && WlImports.Preferred)
		{
			return new WlKeyInput();
		}

		if (X11Imports.HasDisplay)
		{
			return new X11KeyInput();
		}

		if (WlImports.HasDisplay)
		{
			return new WlKeyInput();
		}

		throw new NotSupportedException("Linux key input requires either X11 or Wayland");
#endif
	}
}
