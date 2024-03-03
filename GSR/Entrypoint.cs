using System;
using System.IO;

using static SDL2.SDL;

namespace GSR;

internal static class Entrypoint
{
	private static GSR _gsr;

	private static string CrashLogDirectory()
	{
#if GSR_OSX
		return Path.Combine(SDL_GetPrefPath("", "GSR"), "gsr_crash.txt");
#else
		return Path.Combine(AppContext.BaseDirectory, "gsr_crash.txt");
#endif
	}

	[STAThread]
	private static int Main()
	{
#if GSR_LINUX
		RootHelper.DropRoot();
#endif

		try
		{
			_gsr = new();
			return _gsr.MainLoop();
		}
		catch (Exception ex)
		{
			var exStr = ex.ToString();
			Console.WriteLine(exStr);
			_ = SDL_ShowSimpleMessageBox(
				flags: SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
				title: "Unhandled Exception",
				message: "GSR has crashed :(\nException info will be written to gsr_crash.txt",
				window: IntPtr.Zero
			);

			File.WriteAllText(CrashLogDirectory(), exStr);
			return -1;
		}
		finally
		{
			_gsr?.Dispose();
		}
	}
}
