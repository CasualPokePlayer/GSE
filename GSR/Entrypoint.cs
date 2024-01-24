using System;
using System.IO;

using static SDL2.SDL;

namespace GSR;

internal static class Entrypoint
{
	private static GSR _gsr;

	[STAThread]
	private static int Main()
	{
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

			File.WriteAllText("gsr_crash.txt", exStr);
			return -1;
		}
		finally
		{
			_gsr?.Dispose();
		}
	}
}
