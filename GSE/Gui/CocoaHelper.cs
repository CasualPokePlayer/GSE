// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#if GSE_OSX

using System.Runtime.InteropServices;

namespace GSE.Gui;

internal static partial class CocoaHelper
{
	[LibraryImport("native_helper", StringMarshalling = StringMarshalling.Utf8)]
	public static partial nint cocoa_helper_show_open_file_dialog(nint mainWindow, string title, string baseDir, [In] string[] fileTypes, int numFileTypes);

	[LibraryImport("native_helper", StringMarshalling = StringMarshalling.Utf8)]
	public static partial nint cocoa_helper_show_save_file_dialog(nint mainWindow, string title, string baseDir, string filename, string ext);

	[LibraryImport("native_helper", StringMarshalling = StringMarshalling.Utf8)]
	public static partial nint cocoa_helper_show_select_folder_dialog(nint mainWindow, string title, string baseDir);

	[LibraryImport("native_helper")]
	public static partial void cocoa_helper_free_path(nint path);
}

#endif
