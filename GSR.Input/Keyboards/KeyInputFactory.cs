using System;

namespace GSR.Input.Keyboards;

internal static class KeyInputFactory
{
	public static IKeyInput CreateKeyInput()
	{
		if (OperatingSystem.IsWindowsVersionAtLeast(6, 0, 6000))
		{
			return new RawKeyInput();
		}

		if (OperatingSystem.IsLinux())
		{
			//return new X11KeyInput();
		}

		if (OperatingSystem.IsMacOS())
		{
			//return new QuartzKeyInput();
		}

		throw new NotSupportedException("Key input is not supported on this platform");
	}
}
