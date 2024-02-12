using System;
using System.Runtime.Versioning;

using Windows.Win32;
using Windows.Win32.UI.Controls.Dialogs;

using Gtk;

#if MACOS
using AppKit;
using Foundation;
#endif

namespace GSR.Gui;

/// <summary>
/// IMPORTANT! This can only be used on the GUI thread (on Windows this should be the only STA thread)
/// </summary>
internal static class SaveFileDialog
{
	// TODO: Check if using the newer IFileOpenDialog has any worth
	[SupportedOSPlatform("windows5.0")]
	private static unsafe string ShowWindowsDialog(string description, string baseDir, string filename, string ext)
	{
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

	private static string ShowGtkDialog(string description, string baseDir, string filename, string ext)
	{
		try
		{
			using var dialog = new FileChooserNative($"Save {description}", null, FileChooserAction.Save, "_Save", "_Cancel");
			try
			{
				dialog.DoOverwriteConfirmation = true;
				using var fileFilter = new FileFilter();
				fileFilter.Name = description;
				fileFilter.AddPattern($"*{ext}");
				dialog.AddFilter(fileFilter);
				dialog.SetCurrentFolder(baseDir);
				dialog.SetFilename(filename);
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

#if MACOS
	private static string ShowAppKitDialog(string description, string baseDir, string filename, string ext)
	{
		using var keyWindow = NSApplication.SharedApplication.KeyWindow;
		try
		{
			using var dialog = NSSavePanel.SavePanel;
			dialog.AllowsMultipleSelection = false;
			dialog.CanChooseDirectories = false;
			dialog.CanChooseFiles = true;
			dialog.AllowsOtherFileTypes = false;
			dialog.Title = $"Save {description}";
			dialog.DirectoryUrl = new Uri(baseDir);
			dialog.NameFieldStringValue = filename;
			// deprecated, but have to do this if we want to support macOS 10.15
			dialog.AllowedFileTypes = [ ext[1..] ];
			//dialog.AllowedContentTypes = [ UTType.CreateFromExtension(ext[1..]) ];
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

	public static string ShowDialog(string description, string baseDir, string filename, string ext)
	{
		// ReSharper disable ConvertIfStatementToReturnStatement

		if (OperatingSystem.IsWindowsVersionAtLeast(5))
		{
			return ShowWindowsDialog(description, baseDir, filename, ext);
		}

		if (OperatingSystem.IsLinux())
		{
			return ShowGtkDialog(description, baseDir, filename, ext);
		}

#if MACOS
		if (OperatingSystem.IsMacOS())
		{
			return ShowAppKitDialog(description, baseDir, filename, ext);
		}
#endif

		return null;
	}
}
