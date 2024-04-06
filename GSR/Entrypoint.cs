// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.IO;

using static SDL2.SDL;

namespace GSR;

internal static class Entrypoint
{
	private static GSR _gsr;

	[STAThread]
	public static int Main()
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
			var crashLogPath = PathResolver.GetCrashLogPath();
			_ = SDL_ShowSimpleMessageBox(
				flags: SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
				title: "Unhandled Exception",
				message: $"GSR has crashed :(\nException info will be written to {crashLogPath}",
				window: 0
			);

			File.WriteAllText(crashLogPath, exStr);
			return -1;
		}
		finally
		{
			_gsr?.Dispose();
		}
	}
}
