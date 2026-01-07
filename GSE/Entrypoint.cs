// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.IO;
#if GSE_WINDOWS
using System.Threading;
#endif

using static SDL3.SDL;

namespace GSE;

internal static class Entrypoint
{
	private static GSE _gse;

	[STAThread]
	public static int Main()
	{
		try
		{
#if GSE_WINDOWS
			SynchronizationContext.SetSynchronizationContext(Win32BlockingWaitSyncContext.Singleton);
#endif
			_gse = new();
			return _gse.MainLoop();
		}
		catch (Exception ex)
		{
			var exStr = ex.ToString();
			Console.WriteLine(exStr);
			var crashLogPath = PathResolver.GetCrashLogPath();
			_ = SDL_ShowSimpleMessageBox(
				flags: SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
				title: "Unhandled Exception",
				message: $"GSE has crashed :(\nException info will be written to {crashLogPath}",
				window: 0
			);

			File.WriteAllText(crashLogPath, exStr);
			return -1;
		}
		finally
		{
			_gse?.Dispose();
			SDL_Quit();
		}
	}
}
