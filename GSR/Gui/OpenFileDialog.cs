using System;
using System.Collections.Generic;

#if GSR_WINDOWS
using Windows.Win32;
using Windows.Win32.UI.Controls.Dialogs;
#endif

#if GSR_OSX
using AppKit;
using Foundation;
using System.Linq;
#endif

#if GSR_LINUX
using Gtk;
#endif

namespace GSR.Gui;

/// <summary>
/// IMPORTANT! This can only be used on the GUI thread (on Windows this should be the only STA thread)
/// </summary>
internal static class OpenFileDialog
{
#if GSR_WINDOWS
	// TODO: Check if using the newer IFileOpenDialog has any worth
	public static unsafe string ShowDialog(string description, string baseDir, IEnumerable<string> fileTypes)
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
	public static string ShowDialog(string description, string baseDir, IEnumerable<string> fileTypes)
	{
		using var keyWindow = NSApplication.SharedApplication.KeyWindow;
		try
		{
			using var dialog = NSOpenPanel.OpenPanel;
			dialog.AllowsMultipleSelection = false;
			dialog.CanChooseDirectories = false;
			dialog.CanChooseFiles = true;
			dialog.AllowsOtherFileTypes = false;
			dialog.Title = $"Open {description}";
			dialog.DirectoryUrl = new Uri(baseDir ?? AppContext.BaseDirectory);
			// deprecated, but have to do this if we want to support macOS 10.15
			dialog.AllowedFileTypes = fileTypes.Select(ft => ft[1..]).ToArray();
			//dialog.AllowedContentTypes = fileTypes.Select(ft => UTType.CreateFromExtension(ft[1..])).ToArray();
			if ((NSModalResponse)dialog.RunModal() == NSModalResponse.OK)
			{
				Uri uri = panel.Url;
				return uri.LocalPath;
			}

			return null;
		}
		catch
		{
			return null;
		}
		finally
		{
			keyWindow?.MakeKeyAndOrderFront(null);
		}
	}
#endif

#if GSR_LINUX
	public static string ShowDialog(string description, string baseDir, IEnumerable<string> fileTypes)
	{
		try
		{
			using var dialog = new FileChooserNative($"Open {description}", null, FileChooserAction.Open, "_Open", "_Cancel");
			try
			{
				using var fileFilter = new FileFilter();
				fileFilter.Name = description;
				foreach (var fileType in fileTypes)
				{
					fileFilter.AddPattern($"*{fileType}");
				}
				dialog.AddFilter(fileFilter);
				dialog.SetCurrentFolder(baseDir ?? AppContext.BaseDirectory);
				return (ResponseType)dialog.Run() == ResponseType.Accept ? dialog.Filename : null;
			}
			finally
			{
				dialog.Destroy();
			}
		}
		catch
		{
			return null;
		}
	}
#endif
}
