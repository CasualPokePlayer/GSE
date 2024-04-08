// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
#if GSR_WINDOWS
using System.Runtime.InteropServices;
#endif
#if GSR_OSX || GSR_LINUX
using System.Linq;
#endif

#if GSR_WINDOWS
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
#endif

#if GSR_OSX
using AppKit;
using UniformTypeIdentifiers;
#endif

#if GSR_ANDROID
using GSR.Android;
#endif

namespace GSR.Gui;

/// <summary>
/// IMPORTANT! This can only be used on the GUI thread (on Windows this should be the only STA thread)
/// </summary>
internal static class OpenFileDialog
{
#if GSR_WINDOWS
	public static unsafe string ShowDialog(string description, string baseDir, IEnumerable<string> fileTypes, ImGuiWindow mainWindow)
	{
		// the newer IFileDialog should be used, as the older APIs are limited to MAX_PATH (i.e. no long path support)
		if (PInvoke.CoCreateInstance<IFileOpenDialog>(
			    rclsid: typeof(FileOpenDialog).GUID,
			    pUnkOuter: null,
			    dwClsContext: CLSCTX.CLSCTX_ALL,
			    ppv: out var fileDialog).Failed)
		{
			// this call generally shouldn't fail
			// it might fail if the user is somehow on windows server core
			// but we don't support windows server core, as it doesn't provide any audio services
			return null;
		}

		try
		{
			fileDialog->SetTitle($"Open {description}");
			fileDialog->SetOptions(FILEOPENDIALOGOPTIONS.FOS_STRICTFILETYPES |
			                       FILEOPENDIALOGOPTIONS.FOS_NOCHANGEDIR |
			                       FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM |
			                       FILEOPENDIALOGOPTIONS.FOS_FILEMUSTEXIST |
			                       FILEOPENDIALOGOPTIONS.FOS_NOREADONLYRETURN);

			if (PInvoke.SHCreateItemFromParsingName(baseDir ?? AppContext.BaseDirectory, null, in IShellItem.IID_Guid, out var ppv).Succeeded)
			{
				var folder = (IShellItem*)ppv;
				try
				{
					fileDialog->SetDefaultFolder(folder);
				}
				finally
				{
					folder->Release();
				}
			}

			fixed (char* filterName = description, filterPattern = $"*{string.Join(";*", fileTypes)}")
			{
				COMDLG_FILTERSPEC filter;
				filter.pszName = filterName;
				filter.pszSpec = filterPattern;
				fileDialog->SetFileTypes(1, &filter);
			}

			fileDialog->SetFileTypeIndex(1);

			fileDialog->Show(new(mainWindow.SdlSysWMInfo.info.win.window));

			IShellItem* result;
			fileDialog->GetResult(&result);
			try
			{
				result->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
				try
				{
					return new(path);
				}
				finally
				{
					Marshal.FreeCoTaskMem((nint)path.Value);
				}
			}
			finally
			{
				result->Release();
			}
		}
		catch
		{
			return null;
		}
		finally
		{
			fileDialog->Release();
		}
	}
#endif

#if GSR_OSX
	public static string ShowDialog(string description, string baseDir, IEnumerable<string> fileTypes, ImGuiWindow mainWindow)
	{
		_ = mainWindow;
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
	public static string ShowDialog(string description, string baseDir, IEnumerable<string> fileTypes, ImGuiWindow mainWindow)
	{
		var extensions = fileTypes.ToArray();

		if (PortalFileChooser.IsAvailable)
		{
			try
			{
				using var portal = new PortalFileChooser();
				using var openQuery = portal.CreateOpenFileQuery(description, baseDir ?? AppContext.BaseDirectory, extensions, mainWindow);
				return portal.RunQuery(openQuery);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
				// we'll only mark portal as "unavailable" if the gtk file chooser is available
				// just in case something oddly goes wrong with the portal and yet still be usable
				if (GtkFileChooser.IsAvailable)
				{
					Console.WriteLine("Portal file chooser assumed to be unavailable, falling back on GTK file chooser");
					PortalFileChooser.IsAvailable = false;
				}
			}
		}

		if (GtkFileChooser.IsAvailable)
		{
			using var dialog = new GtkFileChooser($"Open {description}", GtkFileChooser.FileChooserAction.Open);
			dialog.AddButton("_Cancel", GtkFileChooser.Response.Cancel);
			dialog.AddButton("_Open", GtkFileChooser.Response.Accept);
			dialog.AddFilter(description, extensions.Select(ft => $"*{ft}"));
			dialog.SetCurrentFolder(baseDir ?? AppContext.BaseDirectory);
			return dialog.RunDialog() == GtkFileChooser.Response.Accept ? dialog.GetFilename() : null;
		}

		return null;
	}
#endif

#if GSR_ANDROID
	public static string ShowDialog(string description, string baseDir, IEnumerable<string> fileTypes, ImGuiWindow mainWindow)
	{
		_ = description;
		_ = baseDir;
		_ = fileTypes;
		_ = mainWindow;
		return AndroidFile.RequestDocument();
	}
#endif
}
