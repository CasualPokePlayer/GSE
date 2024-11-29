// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
#if GSE_WINDOWS
using System.Runtime.InteropServices;
#endif

#if GSE_WINDOWS
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
#endif

#if GSE_OSX
using static GSE.Gui.CocoaHelper;
#endif

namespace GSE.Gui;

/// <summary>
/// IMPORTANT! This can only be used on the GUI thread (on Windows this should be the only STA thread)
/// </summary>
internal static class SelectFolderDialog
{
#if GSE_WINDOWS
	public static unsafe string ShowDialog(string description, string baseDir, ImGuiWindow mainWindow)
	{
		// the newer IFileDialog should be used, as the older APIs will not give newer Vista style dialogs
		if (PInvoke.CoCreateInstance<IFileOpenDialog>(
			    rclsid: typeof(FileOpenDialog).GUID,
			    pUnkOuter: null,
			    dwClsContext: CLSCTX.CLSCTX_ALL,
			    ppv: out var fileDialog).Failed)
		{
			// this call generally shouldn't fail
			// it might fail if the user is somehow on windows server core
			// but we don't really support windows server core, as it doesn't provide any audio services
			return null;
		}

		try
		{
			fileDialog->SetTitle($"Select {description} Folder");
			fileDialog->SetOptions(FILEOPENDIALOGOPTIONS.FOS_NOCHANGEDIR |
			                       FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS |
			                       FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM |
			                       FILEOPENDIALOGOPTIONS.FOS_PATHMUSTEXIST |
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
	public static string ShowDialog(string description, string baseDir, ImGuiWindow mainWindow)
	{
		var path = cocoa_helper_show_select_folder_dialog(
			mainWindow: mainWindow.SdlSysWMInfo.info.cocoa.window,
			title: $"Select {description} Folder",
			baseDir: baseDir ?? AppContext.BaseDirectory);
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
	public static string ShowDialog(string description, string baseDir, ImGuiWindow mainWindow)
	{
		// select folder dialogs are not available in portal until version 3
		if (PortalFileChooser.IsAvailable && PortalFileChooser.Version >= 3)
		{
			try
			{
				using var portal = new PortalFileChooser();
				using var openQuery = portal.CreateSelectFolderQuery(description, baseDir ?? AppContext.BaseDirectory, mainWindow);
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
			using var dialog = new GtkFileChooser($"Select {description} Folder", GtkFileChooser.FileChooserAction.SelectFolder);
			dialog.AddButton("_Cancel", GtkFileChooser.Response.Cancel);
			dialog.AddButton("_Select", GtkFileChooser.Response.Accept);
			dialog.SetCurrentFolder(baseDir ?? AppContext.BaseDirectory);
			return dialog.RunDialog() == GtkFileChooser.Response.Accept ? dialog.GetFilename() : null;
		}

		return null;
	}
#endif

#if GSE_ANDROID
	public static string ShowDialog(string description, string baseDir, ImGuiWindow mainWindow)
	{
		return null;
	}
#endif
}
