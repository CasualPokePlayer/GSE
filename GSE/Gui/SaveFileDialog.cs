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
using Windows.Win32.UI.Shell.Common;
#endif

#if GSE_OSX
using AppKit;
using UniformTypeIdentifiers;
#endif

namespace GSE.Gui;

/// <summary>
/// IMPORTANT! This can only be used on the GUI thread (on Windows this should be the only STA thread)
/// </summary>
internal static class SaveFileDialog
{
#if GSE_WINDOWS
	public static unsafe string ShowDialog(string description, string baseDir, string filename, string ext, ImGuiWindow mainWindow)
	{
		// the newer IFileDialog should be used, as the older APIs are limited to MAX_PATH (i.e. no long path support)
		if (PInvoke.CoCreateInstance<IFileSaveDialog>(
			    rclsid: typeof(FileSaveDialog).GUID,
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
			fileDialog->SetTitle($"Save {description}");
			fileDialog->SetOptions( FILEOPENDIALOGOPTIONS.FOS_OVERWRITEPROMPT |
			                        FILEOPENDIALOGOPTIONS.FOS_STRICTFILETYPES |
			                        FILEOPENDIALOGOPTIONS.FOS_NOCHANGEDIR |
			                        FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM |
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

			fileDialog->SetFileName(filename);
			fileDialog->SetDefaultExtension(ext);

			fixed (char* filterName = description, filterPattern = $"*{ext}")
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

#if GSE_LINUX
	public static string ShowDialog(string description, string baseDir, string filename, string ext, ImGuiWindow mainWindow)
	{
		try
		{
			using var portal = new PortalFileChooser();
			using var saveQuery = portal.CreateSaveFileQuery(description, baseDir ?? AppContext.BaseDirectory, filename, ext, mainWindow);
			// the path returned won't have an extension, so add one in
			var ret = portal.RunQuery(saveQuery);
			return ret != null ? ret + ext : null;
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

		if (GtkFileChooser.IsAvailable)
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

		return null;
	}
#endif

#if GSE_ANDROID
	public static string ShowDialog(string description, string baseDir, string filename, string ext, ImGuiWindow mainWindow)
	{
		return null;
	}
#endif
}
