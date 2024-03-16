using System;

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
internal static class SaveFileDialog
{
#if GSR_WINDOWS
	// TODO: Check if using the newer IFileOpenDialog has any worth
	public static unsafe string ShowDialog(string description, string baseDir, string filename, string ext, ImGuiWindow mainWindow)
	{
		if (!OperatingSystem.IsWindowsVersionAtLeast(5))
		{
			return null;
		}

		try
		{
			var filter = $"{description}\0*{ext}\0\0";
			var fileBuffer = new char[PInvoke.MAX_PATH + 1];
			filename.CopyTo(fileBuffer);
			var title = $"Save {description}";
			fixed (char* filterPtr = filter, fileBufferPtr = fileBuffer, baseDirPtr = baseDir, titlePtr = title)
			{
				var ofn = default(OPENFILENAMEW);
				ofn.lStructSize = (uint)sizeof(OPENFILENAMEW);
				ofn.hwndOwner = new(mainWindow.SdlSysWMInfo.info.win.window);
				ofn.lpstrFilter = filterPtr;
				ofn.nFilterIndex = 1;
				ofn.lpstrFile = fileBufferPtr;
				ofn.nMaxFile = (uint)fileBuffer.Length;
				ofn.lpstrInitialDir = baseDirPtr;
				ofn.lpstrTitle = titlePtr;
				ofn.Flags = OPEN_FILENAME_FLAGS.OFN_NOCHANGEDIR | OPEN_FILENAME_FLAGS.OFN_OVERWRITEPROMPT | OPEN_FILENAME_FLAGS.OFN_NOREADONLYRETURN;
				if (PInvoke.GetSaveFileName(&ofn))
				{
					return new(fileBufferPtr);
				}

				return null;
			}
		}
		catch
		{
			return null;
		}
	}
#endif

#if GSR_OSX
	public static string ShowDialog(string description, string baseDir, string filename, string ext, ImGuiWindow mainWindow)
	{
		_ = mainWindow;
		using var keyWindow = NSApplication.SharedApplication.KeyWindow;
		try
		{
			using var dialog = NSSavePanel.SavePanel;
			dialog.AllowsOtherFileTypes = false;
			dialog.Title = $"Save {description}";
			dialog.DirectoryUrl = new(baseDir);
			dialog.NameFieldStringValue = filename;
			// the older API is deprecated on macOS 12
			// still need to support it however if we want to support macOS 10.15
			if (OperatingSystem.IsMacOSVersionAtLeast(11))
			{
				dialog.AllowedContentTypes = [ UTType.CreateFromExtension(ext[1..]) ];
			}
			else
			{
				dialog.AllowedFileTypes = [ ext[1..] ];
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
	private	static bool RunPortalFileChooser(string description, string baseDir, string filename, string ext, ImGuiWindow mainWindow, out string path)
	{
		try
		{
			using var portal = new PortalFileChooser();
			using var saveQuery = portal.CreateSaveFileQuery(description, baseDir ?? AppContext.BaseDirectory, filename, ext, mainWindow);
			// the path returned won't have an extension, so add one in
			var ret = portal.RunQuery(saveQuery, mainWindow);
			path = ret != null ? ret + ext : null;
			return true;
		}
		catch
		{
			PortalFileChooser.IsAvailable = false;
		}

		path = null;
		return false;
	}

	private	static bool RunGtkFileChooser(string description, string baseDir, string filename, string ext, ImGuiWindow mainWindow, out string path)
	{
		if (GtkFileChooser.IsAvailable)
		{
			try
			{
				using var dialog = new GtkFileChooser($"Save {description}", GtkFileChooser.FileChooserAction.Save);
				dialog.AddButton("_Cancel", GtkFileChooser.Response.Cancel);
				dialog.AddButton("_Save", GtkFileChooser.Response.Accept);
				dialog.AddFilter(description, [ $"*{ext}" ]);
				dialog.SetCurrentFolder(baseDir ?? AppContext.BaseDirectory);
				dialog.SetCurrentName($"{filename}{ext}");
				dialog.SetOverwriteConfirmation(true);
				path = dialog.RunDialog(mainWindow) == GtkFileChooser.Response.Accept ? dialog.GetFilename() : null;
				return true;
			}
			catch
			{
				GtkFileChooser.IsAvailable = false;
			}
		}

		path = null;
		return false;
	}

	public static string ShowDialog(string description, string baseDir, string filename, string ext, ImGuiWindow mainWindow)
	{
		if (PortalFileChooser.IsPreferred && RunPortalFileChooser(description, baseDir, filename, ext, mainWindow, out var path))
		{
			return path;
		}

		if (RunGtkFileChooser(description, baseDir, filename, ext, mainWindow, out path))
		{
			return path;
		}

		return RunPortalFileChooser(description, baseDir, filename, ext, mainWindow, out path) ? path : null;
	}
#endif
}
