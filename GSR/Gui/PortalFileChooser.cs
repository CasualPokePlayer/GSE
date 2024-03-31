// Copyright (c) 2024 CasualPokePlayer & Bernard Teo
// SPDX-License-Identifier: MPL-2.0 or Zlib

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using static SDL2.SDL;

namespace GSR.Gui;

/// <summary>
/// Based on https://github.com/btzy/nativefiledialog-extended/blob/6dc1272/src/nfd_portal.cpp
/// </summary>
internal sealed partial class PortalFileChooser : IDisposable
{
	static PortalFileChooser()
	{
		try
		{
			// check if we can get a connection
			using var dbusError = new DBusErrorWrapper();
			var conn = dbus_bus_get(DBusBusType.DBUS_BUS_SESSION, ref dbusError.Native);
			if (conn != 0)
			{
				try
				{
					IsAvailable = true;
					Version = GetVersion(conn);
				}
				finally
				{
					dbus_connection_unref(conn);
				}
			}
		}
		catch
		{
			IsAvailable = false;
			Version = 0;
		}

		if (IsAvailable)
		{
			// check if the user wants to prefer the GTK file chooser
			// usually portal is better as it's more "native" and generally less buggy
			// but it has a minor flaw in that the "current folder" does not have to be respected
			var env = Environment.GetEnvironmentVariable("GSR_PREFER_GTK_FILE_CHOOSER");
			var preferred = int.TryParse(env, out var ret) && ret != 0;
			if (preferred)
			{
				// make sure the gtk file chooser is actually available!
				if (GtkFileChooser.IsAvailable)
				{
					IsAvailable = false;
				}
			}
		}
	}

	private static uint GetVersion(nint conn)
	{
		var query = dbus_message_new_method_call("org.freedesktop.portal.Desktop",
			"/org/freedesktop/portal/desktop", "org.freedesktop.DBus.Properties", "Get");
		if (query == 0)
		{
			throw new("Failed to create Get query");
		}

		using (new DBusMessageWrapper(query))
		{
			dbus_message_iter_init_append(query, out var iter);
			EnsureSuccess(dbus_message_iter_append_basic_string(ref iter, DBusType.DBUS_TYPE_STRING, "org.freedesktop.portal.FileChooser"));
			EnsureSuccess(dbus_message_iter_append_basic_string(ref iter, DBusType.DBUS_TYPE_STRING, "version"));

			using var dbusError = new DBusErrorWrapper();
			var reply = dbus_connection_send_with_reply_and_block(conn, query, DBUS_TIMEOUT_INFINITE, ref dbusError.Native);
			if (reply == 0)
			{
				throw new($"Failed to call query, D-Bus error: {dbusError.Message}");
			}

			using (new DBusMessageWrapper(reply))
			{
				if (!dbus_message_iter_init(reply, out iter))
				{
					throw new("D-Bus reply was missing one or more arguments");
				}

				if (dbus_message_iter_get_arg_type(ref iter) != DBusType.DBUS_TYPE_VARIANT)
				{
					throw new("D-Bus reply argument was not DBUS_TYPE_VARIANT");
				}

				dbus_message_iter_recurse(ref iter, out var variantIter);
				if (dbus_message_iter_get_arg_type(ref variantIter) != DBusType.DBUS_TYPE_UINT32)
				{
					throw new("D-Bus reply argument was not DBUS_TYPE_UINT32");
				}

				dbus_message_iter_get_basic(ref variantIter, out uint version);
				return version;
			}
		}
	}

	// ReSharper disable once FieldCanBeMadeReadOnly.Global
	public static bool IsAvailable;
	// ReSharper disable once NotAccessedField.Global
	public static readonly uint Version;

	private readonly nint _conn;
	private readonly string _busUniqueName;
	private readonly string _uniqueToken;
	private readonly string _uniqueObjectPath;
	private string _currentMatchRule;

	public PortalFileChooser()
	{
		using var dbusError = new DBusErrorWrapper();
		_conn = dbus_bus_get(DBusBusType.DBUS_BUS_SESSION, ref dbusError.Native);
		if (_conn == 0)
		{
			throw new($"Failed to obtain D-Bus connection, D-Bus error: {dbusError.Message}");
		}

		try
		{
			_busUniqueName = Marshal.PtrToStringUTF8(dbus_bus_get_unique_name(_conn)) ?? throw new("Failed to obtain D-Bus unique name");
			var sender = (_busUniqueName.StartsWith(':') ? _busUniqueName[1..] : _busUniqueName).Replace('.', '_');
			Span<char> token = stackalloc char[64];
			Random.Shared.GetItems("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", token);
			_uniqueToken = new(token);
			_uniqueObjectPath = $"/org/freedesktop/portal/desktop/request/{sender}/{_uniqueToken}";
			SetMatchRule(_uniqueObjectPath);
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	public void Dispose()
	{
		UnsetMatchRule();
		dbus_connection_unref(_conn);
	}

	private void SetMatchRule(string rule)
	{
		UnsetMatchRule();
		_currentMatchRule = $"type='signal',sender='org.freedesktop.portal.Desktop',path='{rule}',interface='org.freedesktop.portal.Request',member='Response',destination='{_busUniqueName}'";
		using var dbusError = new DBusErrorWrapper();
		dbus_bus_add_match(_conn, _currentMatchRule, ref dbusError.Native);
		if (dbus_error_is_set(in dbusError.Native))
		{
			throw new($"Failed to set match rule, D-Bus error: {dbusError.Message}");
		}
	}

	private void UnsetMatchRule()
	{
		if (_currentMatchRule == null)
		{
			return;
		}

		using var dbusError = new DBusErrorWrapper();
		dbus_bus_remove_match(_conn, _currentMatchRule, ref dbusError.Native);
		_currentMatchRule = null;
		// don't throw an exception if something went wrong here, since this is part of disposing
		if (dbus_error_is_set(in dbusError.Native))
		{
			Console.Error.Write($"Error occurred when unsetting match rule, D-Bus error: {dbusError.Message}");
		}
	}

	public readonly ref struct DBusMessageWrapper(nint message)
	{
		public readonly nint Message = message;

		public void Dispose()
		{
			dbus_message_unref(Message);
		}
	}

	// this is for calls which only fail due to running out of memory
	// (on Linux, this typically never occurs)
	private static void EnsureSuccess(bool res)
	{
		if (!res)
		{
			throw new("D-Bus call failed to allocate memory");
		}
	}

	private static void SetStringOption(ref DBusMessageIter iter, string key, string option)
	{
		EnsureSuccess(dbus_message_iter_open_container(ref iter, DBusType.DBUS_TYPE_DICT_ENTRY, null, out var subIter));
		EnsureSuccess(dbus_message_iter_append_basic_string(ref subIter, DBusType.DBUS_TYPE_STRING, in key));
		EnsureSuccess(dbus_message_iter_open_container(ref subIter, DBusType.DBUS_TYPE_VARIANT, "s", out var variantIter));
		EnsureSuccess(dbus_message_iter_append_basic_string(ref variantIter, DBusType.DBUS_TYPE_STRING, in option));
		EnsureSuccess(dbus_message_iter_close_container(ref subIter, ref variantIter));
		EnsureSuccess(dbus_message_iter_close_container(ref iter, ref subIter));
	}

	private static void SetBoolOption(ref DBusMessageIter iter, string key, bool option)
	{
		EnsureSuccess(dbus_message_iter_open_container(ref iter, DBusType.DBUS_TYPE_DICT_ENTRY, null, out var subIter));
		EnsureSuccess(dbus_message_iter_append_basic_string(ref subIter, DBusType.DBUS_TYPE_STRING, in key));
		EnsureSuccess(dbus_message_iter_open_container(ref subIter, DBusType.DBUS_TYPE_VARIANT, "b", out var variantIter));
		EnsureSuccess(dbus_message_iter_append_basic_bool(ref variantIter, DBusType.DBUS_TYPE_BOOLEAN, in option));
		EnsureSuccess(dbus_message_iter_close_container(ref subIter, ref variantIter));
		EnsureSuccess(dbus_message_iter_close_container(ref iter, ref subIter));
	}

	private static void SetArrayOption(ref DBusMessageIter iter, string key, string option)
	{
		EnsureSuccess(dbus_message_iter_open_container(ref iter, DBusType.DBUS_TYPE_DICT_ENTRY, null, out var subIter));
		EnsureSuccess(dbus_message_iter_append_basic_string(ref subIter, DBusType.DBUS_TYPE_STRING, in key));
		EnsureSuccess(dbus_message_iter_open_container(ref subIter, DBusType.DBUS_TYPE_VARIANT, "ay", out var variantIter));
		EnsureSuccess(dbus_message_iter_open_container(ref variantIter, DBusType.DBUS_TYPE_ARRAY, "y", out var arrayIter));

		var bytes = Encoding.UTF8.GetBytes(option);
		foreach (var b in bytes)
		{
			EnsureSuccess(dbus_message_iter_append_basic_byte(ref arrayIter, DBusType.DBUS_TYPE_BYTE, in b));
		}
		EnsureSuccess(dbus_message_iter_append_basic_byte(ref arrayIter, DBusType.DBUS_TYPE_BYTE, 0));

		EnsureSuccess(dbus_message_iter_close_container(ref variantIter, ref arrayIter));
		EnsureSuccess(dbus_message_iter_close_container(ref subIter, ref variantIter));
		EnsureSuccess(dbus_message_iter_close_container(ref iter, ref subIter));
	}

	private static void AddFilter(ref DBusMessageIter iter, string description, IEnumerable<string> extensions)
	{
		EnsureSuccess(dbus_message_iter_open_container(ref iter, DBusType.DBUS_TYPE_STRUCT, null, out var filterListStructIter));
		// friendly name users see
		EnsureSuccess(dbus_message_iter_append_basic_string(ref filterListStructIter, DBusType.DBUS_TYPE_STRING, in description));
		// patterns used for delimiting
		EnsureSuccess(dbus_message_iter_open_container(ref filterListStructIter, DBusType.DBUS_TYPE_ARRAY, "(us)", out var filterSublistIter));
		foreach (var ext in extensions)
		{
			EnsureSuccess(dbus_message_iter_open_container(ref filterSublistIter, DBusType.DBUS_TYPE_STRUCT, null, out var filterSublistStructIter));
			// 0 indicates glob style
			EnsureSuccess(dbus_message_iter_append_basic_uint(ref filterSublistStructIter, DBusType.DBUS_TYPE_UINT32, 0));
			EnsureSuccess(dbus_message_iter_append_basic_string(ref filterSublistStructIter, DBusType.DBUS_TYPE_STRING, $"*{ext}"));
			EnsureSuccess(dbus_message_iter_close_container(ref filterSublistIter, ref filterSublistStructIter));
		}

		EnsureSuccess(dbus_message_iter_close_container(ref filterListStructIter, ref filterSublistIter));
		EnsureSuccess(dbus_message_iter_close_container(ref iter, ref filterListStructIter));
	}

	private static void SetFilters(ref DBusMessageIter iter, string description, IEnumerable<string> extensions)
	{
		EnsureSuccess(dbus_message_iter_open_container(ref iter, DBusType.DBUS_TYPE_DICT_ENTRY, null, out var subIter));
		EnsureSuccess(dbus_message_iter_append_basic_string(ref subIter, DBusType.DBUS_TYPE_STRING, "filters"));
		EnsureSuccess(dbus_message_iter_open_container(ref subIter, DBusType.DBUS_TYPE_VARIANT, "a(sa(us))", out var variantIter));
		EnsureSuccess(dbus_message_iter_open_container(ref variantIter, DBusType.DBUS_TYPE_ARRAY, "(sa(us))", out var filterListIter));
		AddFilter(ref filterListIter, description, extensions);
		EnsureSuccess(dbus_message_iter_close_container(ref variantIter, ref filterListIter));
		EnsureSuccess(dbus_message_iter_close_container(ref subIter, ref variantIter));
		EnsureSuccess(dbus_message_iter_close_container(ref iter, ref subIter));
	}

	private static void SetCurrentFilter(ref DBusMessageIter iter, string description, IEnumerable<string> extensions)
	{
		EnsureSuccess(dbus_message_iter_open_container(ref iter, DBusType.DBUS_TYPE_DICT_ENTRY, null, out var subIter));
		EnsureSuccess(dbus_message_iter_append_basic_string(ref subIter, DBusType.DBUS_TYPE_STRING, "current_filter"));
		EnsureSuccess(dbus_message_iter_open_container(ref subIter, DBusType.DBUS_TYPE_VARIANT, "(sa(us))", out var variantIter));
		AddFilter(ref variantIter, description, extensions);
		EnsureSuccess(dbus_message_iter_close_container(ref subIter, ref variantIter));
		EnsureSuccess(dbus_message_iter_close_container(ref iter, ref subIter));
	}

	public DBusMessageWrapper CreateOpenFileQuery(string description, string initialPath, string[] extensions, ImGuiWindow parentWindow)
	{
		var query = dbus_message_new_method_call("org.freedesktop.portal.Desktop",
			"/org/freedesktop/portal/desktop", "org.freedesktop.portal.FileChooser", "OpenFile");
		if (query == 0)
		{
			throw new("Failed to create OpenFile D-Bus query");
		}

		try
		{
			dbus_message_iter_init_append(query, out var iter);

			// set "parent window"
			var parentWindowStr = parentWindow.SdlSysWMInfo.subsystem switch
			{
				SDL_SYSWM_TYPE.SDL_SYSWM_X11 => $"x11:{parentWindow.SdlSysWMInfo.info.x11.window:X}",
				// wayland requires an "exported surface handle", something only implemented in SDL3, not SDL2
				// SDL3 also has file dialogs, so upgrading to SDL3 would just mean throwing out this code anyways
				_ => string.Empty,
			};
			EnsureSuccess(dbus_message_iter_append_basic_string(ref iter, DBusType.DBUS_TYPE_STRING, in parentWindowStr));

			// set title
			EnsureSuccess(dbus_message_iter_append_basic_string(ref iter, DBusType.DBUS_TYPE_STRING, $"Open {description}"));

			// set options
			EnsureSuccess(dbus_message_iter_open_container(ref iter, DBusType.DBUS_TYPE_ARRAY, "{sv}", out var optionsIter));
			SetStringOption(ref optionsIter, "handle_token", _uniqueToken);
			SetBoolOption(ref optionsIter, "multiple", false);
			SetBoolOption(ref optionsIter, "directory", false);
			SetStringOption(ref optionsIter, "accept_label", "_Open");
			SetStringOption(ref optionsIter, "cancel_label", "_Cancel");
			SetBoolOption(ref optionsIter, "modal", parentWindowStr != string.Empty);
			SetFilters(ref optionsIter, description, extensions);
			SetCurrentFilter(ref optionsIter, description, extensions);
			SetArrayOption(ref optionsIter, "current_folder", initialPath.TrimEnd('/'));
			EnsureSuccess(dbus_message_iter_close_container(ref iter, ref optionsIter));

			return new(query);
		}
		catch
		{
			dbus_message_unref(query);
			throw;
		}
	}

	public DBusMessageWrapper CreateSaveFileQuery(string description, string initialPath, string filename, string ext, ImGuiWindow parentWindow)
	{
		var query = dbus_message_new_method_call("org.freedesktop.portal.Desktop",
			"/org/freedesktop/portal/desktop", "org.freedesktop.portal.FileChooser", "SaveFile");
		if (query == 0)
		{
			throw new("Failed to create SaveFile D-Bus query");
		}

		try
		{
			dbus_message_iter_init_append(query, out var iter);

			// set "parent window"
			var parentWindowStr = parentWindow.SdlSysWMInfo.subsystem switch
			{
				SDL_SYSWM_TYPE.SDL_SYSWM_X11 => $"x11:{parentWindow.SdlSysWMInfo.info.x11.window:X}",
				// wayland requires an "exported surface handle", something only implemented in SDL3, not SDL2
				// SDL3 also has file dialogs, so upgrading to SDL3 would just mean throwing out this code anyways
				_ => string.Empty,
			};
			EnsureSuccess(dbus_message_iter_append_basic_string(ref iter, DBusType.DBUS_TYPE_STRING, in parentWindowStr));

			// set title
			EnsureSuccess(dbus_message_iter_append_basic_string(ref iter, DBusType.DBUS_TYPE_STRING, $"Save {description}"));

			// set options
			EnsureSuccess(dbus_message_iter_open_container(ref iter, DBusType.DBUS_TYPE_ARRAY, "{sv}", out var optionsIter));
			SetStringOption(ref optionsIter, "handle_token", _uniqueToken);
			SetStringOption(ref optionsIter, "accept_label", "_Save");
			SetStringOption(ref optionsIter, "cancel_label", "_Cancel");
			SetBoolOption(ref optionsIter, "modal", parentWindowStr != string.Empty);
			SetFilters(ref optionsIter, description, [ext]);
			SetCurrentFilter(ref optionsIter, description, [ext]);
			SetStringOption(ref optionsIter, "current_name", filename);
			SetArrayOption(ref optionsIter, "current_folder", initialPath.TrimEnd('/'));
			var targetFile = Path.Combine(initialPath, filename, ext);
			if (File.Exists(targetFile))
			{
				SetArrayOption(ref optionsIter, "current_file", targetFile);
			}
			EnsureSuccess(dbus_message_iter_close_container(ref iter, ref optionsIter));

			return new(query);
		}
		catch
		{
			dbus_message_unref(query);
			throw;
		}
	}

	public DBusMessageWrapper CreateSelectFolderQuery(string description, string initialPath, ImGuiWindow parentWindow)
	{
		var query = dbus_message_new_method_call("org.freedesktop.portal.Desktop",
			"/org/freedesktop/portal/desktop", "org.freedesktop.portal.FileChooser", "OpenFile");
		if (query == 0)
		{
			throw new("Failed to create OpenFile D-Bus query");
		}

		try
		{
			dbus_message_iter_init_append(query, out var iter);

			// set "parent window"
			var parentWindowStr = parentWindow.SdlSysWMInfo.subsystem switch
			{
				SDL_SYSWM_TYPE.SDL_SYSWM_X11 => $"x11:{parentWindow.SdlSysWMInfo.info.x11.window:X}",
				// wayland requires an "exported surface handle", something only implemented in SDL3, not SDL2
				// SDL3 also has file dialogs, so upgrading to SDL3 would just mean throwing out this code anyways
				_ => string.Empty,
			};
			EnsureSuccess(dbus_message_iter_append_basic_string(ref iter, DBusType.DBUS_TYPE_STRING, in parentWindowStr));

			// set title
			EnsureSuccess(dbus_message_iter_append_basic_string(ref iter, DBusType.DBUS_TYPE_STRING, $"Select {description} Folder"));

			// set options
			EnsureSuccess(dbus_message_iter_open_container(ref iter, DBusType.DBUS_TYPE_ARRAY, "{sv}", out var optionsIter));
			SetStringOption(ref optionsIter, "handle_token", _uniqueToken);
			SetBoolOption(ref optionsIter, "multiple", false);
			SetBoolOption(ref optionsIter, "directory", true);
			SetStringOption(ref optionsIter, "accept_label", "_Select");
			SetStringOption(ref optionsIter, "cancel_label", "_Cancel");
			SetBoolOption(ref optionsIter, "modal", parentWindowStr != string.Empty);
			SetArrayOption(ref optionsIter, "current_folder", initialPath.TrimEnd('/'));
			EnsureSuccess(dbus_message_iter_close_container(ref iter, ref optionsIter));

			return new(query);
		}
		catch
		{
			dbus_message_unref(query);
			throw;
		}
	}

	private record QueryThreadParam(nint Conn)
	{
		public nint Response;
	}

	public string RunQuery(DBusMessageWrapper query)
	{
		using var dbusError = new DBusErrorWrapper();
		var reply = dbus_connection_send_with_reply_and_block(_conn, query.Message, DBUS_TIMEOUT_INFINITE, ref dbusError.Native);
		if (reply == 0)
		{
			throw new($"Failed to call query, D-Bus error: {dbusError.Message}");
		}

		using (new DBusMessageWrapper(reply))
		{
			if (!dbus_message_iter_init(reply, out var iter))
			{
				throw new("Query reply was missing an argument");
			}

			if (dbus_message_iter_get_arg_type(ref iter) != DBusType.DBUS_TYPE_OBJECT_PATH)
			{
				throw new("Query reply was not an object path");
			}

			dbus_message_iter_get_basic(ref iter, out nint path);
			var pathStr = Marshal.PtrToStringUTF8(path);
			if (pathStr != _uniqueObjectPath)
			{
				SetMatchRule(pathStr);
			}
		}

		static void QueryThreadProc(object param)
		{
			var queryThreadParam = (QueryThreadParam)param;
			do
			{
				nint message;
				while ((message = dbus_connection_pop_message(queryThreadParam.Conn)) != 0)
				{
					try
					{
						if (dbus_message_is_signal(message, "org.freedesktop.portal.Request", "Response"))
						{
							queryThreadParam.Response = message;
							message = 0;
							break;
						}
					}
					finally
					{
						if (message != 0)
						{
							dbus_message_unref(message);
						}
					}
				}
			} while (queryThreadParam.Response == 0 && dbus_connection_read_write(queryThreadParam.Conn, -1));
		}

		var queryThread = new Thread(QueryThreadProc) { IsBackground = true };
		var queryThreadParam = new QueryThreadParam(_conn);
		queryThread.Start(queryThreadParam);
		while (queryThread.IsAlive)
		{
			// keep events pumping while we wait (don't want annoying "not responding" messages)
			SDL_PumpEvents();
			Thread.Sleep(50);
		}

		var response = queryThreadParam.Response;
		if (response == 0)
		{
			throw new("Failed to obtain response from D-Bus portal");
		}

		using (new DBusMessageWrapper(response))
		{
			if (!dbus_message_iter_init(response, out var iter))
			{
				throw new("D-Bus response was missing one or more arguments");
			}

			// read out the response code
			if (dbus_message_iter_get_arg_type(ref iter) != DBusType.DBUS_TYPE_UINT32)
			{
				throw new("D-Bus response argument was not DBUS_TYPE_UINT32");
			}

			dbus_message_iter_get_basic(ref iter, out uint responseCode);
			if (responseCode != 0)
			{
				if (responseCode is 1 or 2)
				{
					// cancel or the X button was pressed
					return null;
				}

				throw new($"D-Bus response errored with unknown response code {responseCode}");
			}

			if (!dbus_message_iter_next(ref iter))
			{
				throw new("D-Bus response was missing one or more arguments");
			}

			if (dbus_message_iter_get_arg_type(ref iter) != DBusType.DBUS_TYPE_ARRAY)
			{
				throw new("D-Bus response argument was not DBUS_TYPE_ARRAY");
			}

			dbus_message_iter_recurse(ref iter, out var subIter);
			while (dbus_message_iter_get_arg_type(ref subIter) == DBusType.DBUS_TYPE_DICT_ENTRY)
			{
				dbus_message_iter_recurse(ref subIter, out var entryIter);
				if (dbus_message_iter_get_arg_type(ref entryIter) != DBusType.DBUS_TYPE_STRING)
				{
					throw new("D-Bus response dict entry did not start with a string");
				}

				dbus_message_iter_get_basic(ref entryIter, out nint key);
				if (!dbus_message_iter_next(ref entryIter))
				{
					throw new("D-Bus response dict entry was missing one or more arguments");
				}

				if (dbus_message_iter_get_arg_type(ref entryIter) != DBusType.DBUS_TYPE_VARIANT)
				{
					throw new("D-Bus response dict entry was not DBUS_TYPE_VARIANT");
				}

				dbus_message_iter_recurse(ref entryIter, out var variantIter);
				var keyStr = Marshal.PtrToStringUTF8(key);
				if (keyStr == "uris")
				{
					if (dbus_message_iter_get_arg_type(ref variantIter) != DBusType.DBUS_TYPE_ARRAY)
					{
						throw new("D-Bus response URI variant was not DBUS_TYPE_ARRAY");
					}

					dbus_message_iter_recurse(ref variantIter, out var uriIter);
					if (dbus_message_iter_get_arg_type(ref uriIter) != DBusType.DBUS_TYPE_STRING)
					{
						throw new("D-Bus response URI field was not DBUS_TYPE_STRING");
					}

					dbus_message_iter_get_basic(ref uriIter, out nint uri);
					var uriStr = Marshal.PtrToStringUTF8(uri) ?? throw new("Got null string for URI");
					return new Uri(uriStr).LocalPath;
				}

				if (!dbus_message_iter_next(ref subIter))
				{
					break;
				}
			}

			throw new("D-Bus response had no URI field");
		}
	}

	private class DBusErrorWrapper : IDisposable
	{
		public DBusError Native;

		public DBusErrorWrapper()
		{
			dbus_error_init(out Native);
		}

		public string Message => Marshal.PtrToStringUTF8(Native.message) ?? "No error message set";

		public void Dispose()
		{
			dbus_error_free(ref Native);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct DBusError
	{
		public nint name;
		public nint message;
		public uint dummy;
		public nint padding;
	}

	[LibraryImport("libdbus-1.so.3")]
	private static partial void dbus_error_init(out DBusError error);

	[LibraryImport("libdbus-1.so.3")]
	private static partial void dbus_error_free(ref DBusError error);

	[LibraryImport("libdbus-1.so.3")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool dbus_error_is_set(in DBusError error);

	private enum DBusBusType
	{
		DBUS_BUS_SESSION,
	}

	[LibraryImport("libdbus-1.so.3")]
	private static partial nint dbus_bus_get(DBusBusType type, ref DBusError error);

	[LibraryImport("libdbus-1.so.3")]
	private static partial void dbus_connection_unref(nint connection);

	[LibraryImport("libdbus-1.so.3")]
	private static partial nint dbus_bus_get_unique_name(nint connection);

	[LibraryImport("libdbus-1.so.3", StringMarshalling = StringMarshalling.Utf8)]
	private static partial void dbus_bus_add_match(nint connection, string rule, ref DBusError error);

	[LibraryImport("libdbus-1.so.3", StringMarshalling = StringMarshalling.Utf8)]
	private static partial void dbus_bus_remove_match(nint connection, string rule, ref DBusError error);

	[LibraryImport("libdbus-1.so.3", StringMarshalling = StringMarshalling.Utf8)]
	private static partial nint dbus_message_new_method_call(string destination, string path, string iface, string method);

	[LibraryImport("libdbus-1.so.3")]
	private static partial void dbus_message_unref(nint message);

	[LibraryImport("libdbus-1.so.3", StringMarshalling = StringMarshalling.Utf8)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool dbus_message_is_signal(nint message, string iface, string signal_name);

	[StructLayout(LayoutKind.Sequential)]
	private struct DBusMessageIter
	{
		public nint dummy1, dummy2;
		public uint dummy3;
		public int dummy4, dummy5, dummy6, dummy7, dummy8, dummy9, dummy10, dummy11;
		public int pad1;
		public nint pad2, pad3;
	}

	[LibraryImport("libdbus-1.so.3")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool dbus_message_iter_init(nint message, out DBusMessageIter iter);

	[LibraryImport("libdbus-1.so.3")]
	private static partial DBusType dbus_message_iter_get_arg_type(ref DBusMessageIter iter);

	[LibraryImport("libdbus-1.so.3")]
	private static partial void dbus_message_iter_get_basic(ref DBusMessageIter iter, out uint value);

	[LibraryImport("libdbus-1.so.3")]
	private static partial void dbus_message_iter_get_basic(ref DBusMessageIter iter, out nint value);

	[LibraryImport("libdbus-1.so.3")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool dbus_message_iter_next(ref DBusMessageIter iter);

	[LibraryImport("libdbus-1.so.3")]
	private static partial void dbus_message_iter_recurse(ref DBusMessageIter iter, out DBusMessageIter sub);

	[LibraryImport("libdbus-1.so.3")]
	private static partial void dbus_message_iter_init_append(nint message, out DBusMessageIter iter);

	private enum DBusType
	{
		DBUS_TYPE_ARRAY = 'a',
		DBUS_TYPE_BOOLEAN = 'b',
		DBUS_TYPE_BYTE = 'y',
		DBUS_TYPE_DICT_ENTRY = 'e',
		DBUS_TYPE_OBJECT_PATH = 'o',
		DBUS_TYPE_STRING = 's',
		DBUS_TYPE_STRUCT = 'r',
		DBUS_TYPE_UINT32 = 'u',
		DBUS_TYPE_VARIANT = 'v',
	}

	[LibraryImport("libdbus-1.so.3", EntryPoint = "dbus_message_iter_append_basic", StringMarshalling = StringMarshalling.Utf8)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool dbus_message_iter_append_basic_string(ref DBusMessageIter iter, DBusType type, in string value);

	[LibraryImport("libdbus-1.so.3", EntryPoint = "dbus_message_iter_append_basic")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool dbus_message_iter_append_basic_bool(ref DBusMessageIter iter, DBusType type, [MarshalAs(UnmanagedType.Bool)] in bool value);

	[LibraryImport("libdbus-1.so.3", EntryPoint = "dbus_message_iter_append_basic")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool dbus_message_iter_append_basic_byte(ref DBusMessageIter iter, DBusType type, in byte value);

	[LibraryImport("libdbus-1.so.3", EntryPoint = "dbus_message_iter_append_basic")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool dbus_message_iter_append_basic_uint(ref DBusMessageIter iter, DBusType type, in uint value);

	[LibraryImport("libdbus-1.so.3", StringMarshalling = StringMarshalling.Utf8)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool dbus_message_iter_open_container(ref DBusMessageIter iter, DBusType type, string contained_signature, out DBusMessageIter sub);

	[LibraryImport("libdbus-1.so.3")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool dbus_message_iter_close_container(ref DBusMessageIter iter, ref DBusMessageIter sub);

	private const int DBUS_TIMEOUT_INFINITE = int.MaxValue;

	[LibraryImport("libdbus-1.so.3")]
	private static partial nint dbus_connection_send_with_reply_and_block(nint connection, nint message, int timeout_milliseconds, ref DBusError error);

	[LibraryImport("libdbus-1.so.3")]
	private static partial nint dbus_connection_pop_message(nint connection);

	[LibraryImport("libdbus-1.so.3")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool dbus_connection_read_write(nint connection, int timeout_milliseconds);
}
