using System;
using System.Collections.Generic;
#if GSR_OSX || GSR_LINUX
using System.Linq;
#endif

using static SDL2.SDL;

#if GSR_WINDOWS
using Windows.Win32;
using Windows.Win32.UI.Controls.Dialogs;
#endif

#if GSR_OSX
using AppKit;
using UniformTypeIdentifiers;
#endif

namespace GSR.Gui;

/// <summary>
/// IMPORTANT! This can only be used on the GUI thread (on Windows this should be the only STA thread)
/// </summary>
internal static class OpenFileDialog
{
#if GSR_WINDOWS
	// TODO: Check if using the newer IFileOpenDialog has any worth
	public static unsafe string ShowDialog(string description, string baseDir, IEnumerable<string> fileTypes, in SDL_SysWMinfo sdlSysWmInfo)
	{
		if (!OperatingSystem.IsWindowsVersionAtLeast(5))
		{
			return null;
		}

		var filter = $"{description}\0*{string.Join(";*", fileTypes)}\0\0";
		var fileBuffer = new char[PInvoke.MAX_PATH + 1];
		var initDir = baseDir ?? AppContext.BaseDirectory;
		var title = $"Open {description}";
		fixed (char* filterPtr = filter, fileBufferPtr = fileBuffer, initDirPtr = initDir, titlePtr = title)
		{
			var ofn = default(OPENFILENAMEW);
			ofn.lStructSize = (uint)sizeof(OPENFILENAMEW);
			ofn.hwndOwner = new(sdlSysWmInfo.info.win.window);
			ofn.lpstrFilter = filterPtr;
			ofn.nFilterIndex = 1;
			ofn.lpstrFile = fileBufferPtr;
			ofn.nMaxFile = (uint)fileBuffer.Length;
			ofn.lpstrInitialDir = initDirPtr;
			ofn.lpstrTitle = titlePtr;
			ofn.Flags = OPEN_FILENAME_FLAGS.OFN_NOCHANGEDIR | OPEN_FILENAME_FLAGS.OFN_PATHMUSTEXIST | OPEN_FILENAME_FLAGS.OFN_FILEMUSTEXIST | OPEN_FILENAME_FLAGS.OFN_NOREADONLYRETURN;
			if (PInvoke.GetOpenFileName(&ofn))
			{
				return new(fileBufferPtr);
			}
		}

		return null;
	}
#endif

#if GSR_OSX
	public static string ShowDialog(string description, string baseDir, IEnumerable<string> fileTypes, in SDL_SysWMinfo sdlSysWmInfo)
	{
		_ = sdlSysWmInfo;
		using var keyWindow = NSApplication.SharedApplication.KeyWindow;
		try
		{
			using var dialog = NSOpenPanel.OpenPanel;
			dialog.AllowsMultipleSelection = false;
			dialog.CanChooseDirectories = false;
			dialog.CanChooseFiles = true;
			dialog.AllowsOtherFileTypes = false;
			dialog.Title = $"Open {description}";
			dialog.DirectoryUrl = new(baseDir ?? AppContext.BaseDirectory);
			// the older API is deprecated on macOS 12
			// still need to support it however if we want to support macOS 10.15
			if (OperatingSystem.IsMacOSVersionAtLeast(11))
			{
				dialog.AllowedContentTypes = fileTypes.Select(ft => UTType.CreateFromExtension(ft[1..])).ToArray();
			}
			else
			{
				dialog.AllowedFileTypes = fileTypes.Select(ft => ft[1..]).ToArray();
			}
			return (NSModalResponse)dialog.RunModal() == NSModalResponse.OK ? dialog.Url.Path : null;
		}
		catch
		{
			return null;
		}
		finally
		{
			keyWindow.MakeKeyAndOrderFront(null);
		}
	}
#endif

#if GSR_LINUX
	public static string ShowDialog(string description, string baseDir, IEnumerable<string> fileTypes, in SDL_SysWMinfo sdlSysWmInfo)
	{
		var extensions = fileTypes.ToArray();

		if (PortalFileChooser.IsAvailable && false)
		{
			try
			{
				using var portal = new PortalFileChooser();
				using var openQuery = portal.CreateOpenFileQuery(description, extensions, baseDir ?? AppContext.BaseDirectory, in sdlSysWmInfo);
				return portal.RunQuery(openQuery);
			}
			catch
			{
				PortalFileChooser.IsAvailable = false;
			}
		}

		if (GtkFileChooser.IsAvailable)
		{
			try
			{
				using var dialog = new GtkFileChooser($"Open {description}", GtkFileChooser.FileChooserAction.Open);
				dialog.AddButton("_Cancel", GtkFileChooser.Response.Cancel);
				dialog.AddButton("_Open", GtkFileChooser.Response.Accept);
				dialog.AddFilter(description, extensions.Select(ft => $"*{ft}"));
				dialog.SetCurrentFolder(baseDir ?? AppContext.BaseDirectory);
				return dialog.RunDialog() == GtkFileChooser.Response.Accept ? dialog.GetFilename() : null;
			}
			catch
			{
				GtkFileChooser.IsAvailable = false;
			}
		}

		return null;
	}
#endif
}
