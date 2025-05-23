// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
#if GSE_WINDOWS || GSE_OSX
using System.Runtime.InteropServices;
#endif
#if GSE_OSX || GSE_LINUX
using System.Linq;
#endif

#if GSE_WINDOWS
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
#endif

#if GSE_ANDROID
using GSE.Android;
#endif

#if GSE_OSX
using static GSE.Gui.CocoaHelper;
#endif

namespace GSE.Gui;

/// <summary>
/// IMPORTANT! This can only be used on the GUI thread (on Windows this should be the only STA thread)
/// </summary>
internal static class OpenFileDialog
{
#if GSE_WINDOWS
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
			                       FILEOPENDIALOGOPTIONS.FOS_FILEMUSTEXIST);

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

#if GSE_OSX
	public static string ShowDialog(string description, string baseDir, IEnumerable<string> fileTypes, ImGuiWindow mainWindow)
	{
		var fileTypesArray = fileTypes.Select(ft => ft[1..]).ToArray();
		var path = cocoa_helper_show_open_file_dialog(
			mainWindow: mainWindow.SdlSysWMInfo.info.cocoa.window,
			title: $"Open {description}",
			baseDir: baseDir ?? AppContext.BaseDirectory,
			fileTypes: fileTypesArray,
			numFileTypes: fileTypesArray.Length);
		try
		{
			return Marshal.PtrToStringUTF8(path);
		}
		finally
		{
			cocoa_helper_free_path(path);
		}
	}
#endif

#if GSE_LINUX
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

#if GSE_ANDROID
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
