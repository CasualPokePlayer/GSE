using System;

#if GSR_WINDOWS
using Windows.Win32;
using Windows.Win32.UI.Controls.Dialogs;
#endif

#if GSR_OSX
using AppKit;
#endif

namespace GSR.Gui;

/// <summary>
/// IMPORTANT! This can only be used on the GUI thread (on Windows this should be the only STA thread)
/// </summary>
internal static class SaveFileDialog
{
#if GSR_WINDOWS
	// TODO: Check if using the newer IFileOpenDialog has any worth
	public static unsafe string ShowDialog(string description, string baseDir, string filename, string ext)
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
	public static string ShowDialog(string description, string baseDir, string filename, string ext)
	{
		using var keyWindow = NSApplication.SharedApplication.KeyWindow;
		try
		{
			using var dialog = NSSavePanel.SavePanel;
			dialog.AllowsOtherFileTypes = false;
			dialog.Title = $"Save {description}";
			dialog.DirectoryUrl = new(baseDir);
			dialog.NameFieldStringValue = filename;
			// deprecated on macOS 12, but have to do this if we want to support macOS 10.15
			dialog.AllowedFileTypes = [ ext[1..] ];
			//dialog.AllowedContentTypes = [ UniformTypeIdentifiers.UTType.CreateFromExtension(ext[1..]) ];
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
	public static string ShowDialog(string description, string baseDir, string filename, string ext)
	{
		if (!GtkFileChooser.IsAvailable)
		{
			return null;
		}

		try
		{
			using var dialog = new GtkFileChooser($"Save {description}", GtkFileChooser.FileChooserAction.Save);
			dialog.AddButton("_Cancel", GtkFileChooser.Response.Cancel);
			dialog.AddButton("_Save", GtkFileChooser.Response.Accept);
			dialog.AddFilter(description, [ $"*{ext}" ]);
			dialog.SetCurrentFolder(baseDir ?? AppContext.BaseDirectory);
			dialog.SetCurrentName($"{filename}{ext}");
			dialog.SetOverwriteConfirmation(true);
			return dialog.RunDialog() == GtkFileChooser.Response.Accept ? dialog.GetFilename() : null;
		}
		catch
		{
			return null;
		}
	}
#endif
}
