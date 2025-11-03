// Copyright (c) 2024 CasualPokePlayer & Lior Halphon
// SPDX-License-Identifier: MPL-2.0 or MIT

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using static SDL3.SDL;

namespace GSE.Gui;

/// <summary>
/// Based on https://github.com/LIJI32/SameBoy/blob/cbb7e01/OpenDialog/gtk.c
/// </summary>
internal sealed partial class GtkFileChooser : IDisposable
{
	private const string LIBGTK = "libgtk";

	// most of the file chooser apis used only need at least gtk 2.4
	private static readonly ImmutableArray<string> _gtkLibraryNames = 
	[
		"libgtk-3.so.0",
		"libgtk-3.so",
		"libgtk-x11-2.0.so.0",
		"libgtk-x11-2.0.so",
	];

	private static nint GtkImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
	{
		if (libraryName != LIBGTK)
		{
			return NativeLibrary.TryLoad(libraryName, assembly, searchPath, out var handle) ? handle : 0;
		}

		foreach (var gtkLibraryName in _gtkLibraryNames)
		{
			if (NativeLibrary.TryLoad(gtkLibraryName, assembly, searchPath, out var gtkHandle))
			{
				// check if the file_chooser api is available
				if (NativeLibrary.TryGetExport(gtkHandle, "gtk_file_chooser_dialog_new", out _))
				{
					return gtkHandle;
				}

				NativeLibrary.Free(gtkHandle);
			}
		}

		return 0;
	}

	[UnmanagedCallersOnly]
	private static void StubGtkLogger(nint log_domain, int log_level, nint message, nint user_data)
	{
	}

	static GtkFileChooser()
	{
		// currently this assembly (GSE) only needs this special logic for Gtk
		// only 1 import resolver may be assigned per assembly
		// so this might need to be moved to another class if another library needs special logic
		NativeLibrary.SetDllImportResolver(typeof(GtkFileChooser).Assembly, GtkImportResolver);

		try
		{
			IsAvailable = gtk_init_check(0, 0);
			// prevent gtk log spam
			unsafe
			{
				_ = g_log_set_default_handler(&StubGtkLogger, 0);
			}
		}
		catch
		{
			// g_log_set_default_handler might not be available (requires at least glib 2.6, which was released slightly after gtk 2.4)
			// but most file chooser apis might still be available
		}
	}

	public static readonly bool IsAvailable;

	public enum FileChooserAction
	{
		Open = 0,
		Save = 1,
		SelectFolder = 2,
	}

	public enum Response
	{
		Accept = -3,
		Cancel = -6,
	}

	private readonly nint _chooser;

	public GtkFileChooser(string title, FileChooserAction action)
	{
		_chooser = gtk_file_chooser_dialog_new(title, 0, action, 0);
		if (_chooser == 0)
		{
			throw new("Failed to create Gtk file chooser!");
		}
	}

	public void AddButton(string buttonText, Response responseId)
	{
		_ = gtk_dialog_add_button(_chooser, buttonText, responseId);
	}

	public void AddFilter(string name, IEnumerable<string> patterns)
	{
		var filter = gtk_file_filter_new();
		if (filter == 0)
		{
			throw new("Failed to create file filter");
		}

		gtk_file_filter_set_name(filter, name);
		foreach (var pattern in patterns)
		{
			gtk_file_filter_add_pattern(filter, pattern);
		}

		gtk_file_chooser_add_filter(_chooser, filter);
	}

	public void SetCurrentFolder(string folder)
	{
		_ = gtk_file_chooser_set_current_folder(_chooser, folder);
	}

	public void SetCurrentName(string name)
	{
		gtk_file_chooser_set_current_name(_chooser, name);
	}

	public void SetOverwriteConfirmation(bool doOverwriteConfirmation)
	{
		try
		{
			gtk_file_chooser_set_do_overwrite_confirmation(_chooser, doOverwriteConfirmation);
		}
		catch
		{
			// might not be available (requires at least gtk 2.8), not critical for usage however
		}
	}

	private record DialogThreadParam(nint Chooser)
	{
		public Response Response;
	}

	public Response RunDialog()
	{
		static void DialogThreadProc(object param)
		{
			var dialogThreadParam = (DialogThreadParam)param;
			dialogThreadParam.Response = gtk_dialog_run(dialogThreadParam.Chooser);
		}

		var dialogThread = new Thread(DialogThreadProc) { IsBackground = true };
		var dialogThreadParam = new DialogThreadParam(_chooser);
		dialogThread.Start(dialogThreadParam);
		while (dialogThread.IsAlive)
		{
			// keep events pumping while we wait (don't want annoying "not responding" messages)
			SDL_PumpEvents();
			Thread.Sleep(50);
		}

		return dialogThreadParam.Response;
	}

	public string GetFilename()
	{
		var filename = gtk_file_chooser_get_filename(_chooser);
		try
		{
			return Marshal.PtrToStringUTF8(filename);
		}
		finally
		{
			g_free(filename);
		}
	}

	public void Dispose()
	{
		while (gtk_events_pending())
		{
			gtk_main_iteration();
		}

		gtk_widget_destroy(_chooser);

		while (gtk_events_pending())
		{
			gtk_main_iteration();
		}
	}

	[LibraryImport(LIBGTK)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool gtk_init_check(nint argc, nint argv);

	[LibraryImport(LIBGTK)]
	private static unsafe partial nint g_log_set_default_handler(delegate* unmanaged<nint, int, nint, nint, void> log_func, nint user_data);

	[LibraryImport(LIBGTK, StringMarshalling = StringMarshalling.Utf8)]
	private static partial nint gtk_file_chooser_dialog_new(string title, nint parent, FileChooserAction action, nint first_button_text);

	[LibraryImport(LIBGTK, StringMarshalling = StringMarshalling.Utf8)]
	private static partial nint gtk_dialog_add_button(nint dialog, string button_text, Response response_id);

	[LibraryImport(LIBGTK)]
	private static partial nint gtk_file_filter_new();

	[LibraryImport(LIBGTK, StringMarshalling = StringMarshalling.Utf8)]
	private static partial void gtk_file_filter_set_name(nint filter, string name);

	[LibraryImport(LIBGTK, StringMarshalling = StringMarshalling.Utf8)]
	private static partial void gtk_file_filter_add_pattern(nint filter, string pattern);

	[LibraryImport(LIBGTK, StringMarshalling = StringMarshalling.Utf8)]
	private static partial void gtk_file_chooser_add_filter(nint chooser, nint filter);

	[LibraryImport(LIBGTK, StringMarshalling = StringMarshalling.Utf8)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool gtk_file_chooser_set_current_folder(nint chooser, string filename);

	[LibraryImport(LIBGTK, StringMarshalling = StringMarshalling.Utf8)]
	private static partial void gtk_file_chooser_set_current_name(nint chooser, string name);

	[LibraryImport(LIBGTK)]
	private static partial void gtk_file_chooser_set_do_overwrite_confirmation(nint chooser, [MarshalAs(UnmanagedType.Bool)] bool do_overwrite_confirmation);

	[LibraryImport(LIBGTK)]
	private static partial Response gtk_dialog_run(nint dialog);

	[LibraryImport(LIBGTK)]
	private static partial nint gtk_file_chooser_get_filename(nint chooser);

	[LibraryImport(LIBGTK)]
	private static partial void g_free(nint mem);

	[LibraryImport(LIBGTK)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool gtk_events_pending();

	[LibraryImport(LIBGTK)]
	private static partial void gtk_main_iteration();

	[LibraryImport(LIBGTK)]
	private static partial void gtk_widget_destroy(nint widget);
}
