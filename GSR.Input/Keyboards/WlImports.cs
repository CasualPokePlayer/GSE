using System;
using System.Runtime.InteropServices;

namespace GSR.Input.Keyboards;

#pragma warning disable IDE1006 // Naming rule violations

internal static partial class WlImports
{
	public static readonly bool HasDisplay;

	public static readonly IntPtr wl_registry_interface;
	public static readonly IntPtr wl_seat_interface;
	public static readonly IntPtr wl_keyboard_interface;

	static WlImports()
	{
		try
		{
			var display = wl_display_connect(IntPtr.Zero);
			if (display != IntPtr.Zero)
			{
				HasDisplay = true;
				wl_display_disconnect(display);
			}
		}
		catch
		{
			HasDisplay = false;
		}

		if (HasDisplay)
		{
			// there are a few interface structs exported we need access to
			// library import can't handle these, so have to manually load them!
			var handle = NativeLibrary.Load("libwayland-client.so.0");
			try
			{
				wl_registry_interface = NativeLibrary.GetExport(handle, "wl_registry_interface");
				wl_seat_interface = NativeLibrary.GetExport(handle, "wl_seat_interface");
				wl_keyboard_interface = NativeLibrary.GetExport(handle, "wl_keyboard_interface");
			}
			finally
			{
				NativeLibrary.Free(handle);
			}
		}
	}

	[LibraryImport("libwayland-client.so.0")]
	public static partial IntPtr wl_display_connect(IntPtr name);

	[LibraryImport("libwayland-client.so.0")]
	public static partial void wl_display_disconnect(IntPtr display);

	[LibraryImport("libwayland-client.so.0")]
	public static partial IntPtr wl_display_create_queue(IntPtr display);

	[LibraryImport("libwayland-client.so.0")]
	public static partial void wl_event_queue_destroy(IntPtr queue);

	[LibraryImport("libwayland-client.so.0")]
	public static partial int wl_display_flush(IntPtr display);

	[LibraryImport("libwayland-client.so.0")]
	public static partial int wl_display_roundtrip_queue(IntPtr display, IntPtr queue);

	[LibraryImport("libwayland-client.so.0")]
	public static partial int wl_display_prepare_read_queue(IntPtr display, IntPtr queue);

	[LibraryImport("libwayland-client.so.0")]
	public static partial int wl_display_read_events(IntPtr display);

	[LibraryImport("libwayland-client.so.0")]
	public static partial int wl_display_dispatch_queue_pending(IntPtr display, IntPtr queue);

	[LibraryImport("libwayland-client.so.0")]
	public static partial IntPtr wl_proxy_create_wrapper(IntPtr proxy);

	[LibraryImport("libwayland-client.so.0")]
	public static partial void wl_proxy_wrapper_destroy(IntPtr proxy_wrapper);

	[LibraryImport("libwayland-client.so.0")]
	public static partial void wl_proxy_set_queue(IntPtr proxy, IntPtr queue);

	// quite a few functions in wayland are simply implemented as static inline functions, using wl_proxy_* methods
	// so we have to re-create them here

	[LibraryImport("libwayland-client.so.0")]
	private static partial IntPtr wl_proxy_marshal_constructor(IntPtr proxy, uint opcode, IntPtr iface, IntPtr end_args);

	[LibraryImport("libwayland-client.so.0")]
	private static partial IntPtr wl_proxy_marshal_constructor(IntPtr proxy, uint opcode, IntPtr iface, uint name, IntPtr iface_name, uint iface_ver, IntPtr end_args);

	[LibraryImport("libwayland-client.so.0")]
	private static partial int wl_proxy_add_listener(IntPtr proxy, IntPtr implementation, IntPtr data);

	[LibraryImport("libwayland-client.so.0")]
	private static partial void wl_proxy_destroy(IntPtr proxy);

	public static IntPtr wl_display_get_registry(IntPtr display)
	{
		const uint WL_DISPLAY_GET_REGISTRY = 1;
		return wl_proxy_marshal_constructor(display, WL_DISPLAY_GET_REGISTRY, wl_registry_interface, IntPtr.Zero);
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct wl_interface
	{
		public IntPtr name;
		// there are more members here, but we don't care about them :)
	}

	public static unsafe IntPtr wl_registry_bind(IntPtr wl_registry, uint name, IntPtr iface, uint ver)
	{
		const uint WL_REGISTRY_BIND = 0;
		return wl_proxy_marshal_constructor(wl_registry, WL_REGISTRY_BIND, iface, name, ((wl_interface*)iface)->name, ver, IntPtr.Zero);
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct wl_registry_listener
	{
		public delegate* unmanaged<IntPtr, IntPtr, uint, IntPtr, uint, void> global;
		public delegate* unmanaged<IntPtr, IntPtr, uint, void> global_remove;
	}

	public static int wl_registry_add_listener(IntPtr wl_registry, IntPtr listener, IntPtr data)
	{
		return wl_proxy_add_listener(wl_registry, listener, data);
	}

	public static void wl_registry_destroy(IntPtr wl_registry)
	{
		wl_proxy_destroy(wl_registry);
	}

	[Flags]
	public enum WlSeatCapabilities : uint
	{
		WL_SEAT_CAPABILITY_KEYBOARD = 2,
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct wl_seat_listener
	{
		public delegate* unmanaged<IntPtr, IntPtr, WlSeatCapabilities, void> capabilities;
		public delegate* unmanaged<IntPtr, IntPtr, IntPtr, void> name;
	}

	public static int wl_seat_add_listener(IntPtr wl_seat, IntPtr listener, IntPtr data)
	{
		return wl_proxy_add_listener(wl_seat, listener, data);
	}

	public static IntPtr wl_seat_get_keyboard(IntPtr wl_seat)
	{
		const uint WL_SEAT_GET_KEYBOARD = 1;
		return wl_proxy_marshal_constructor(wl_seat, WL_SEAT_GET_KEYBOARD, wl_keyboard_interface, IntPtr.Zero);
	}

	public static void wl_seat_destroy(IntPtr wl_seat)
	{
		wl_proxy_destroy(wl_seat);
	}

	public enum WlKeymapFormat : uint
	{
		WL_KEYBOARD_KEYMAP_FORMAT_XKB_V1 = 1,
	}

	public enum WlKeyState : uint
	{
		WL_KEYBOARD_KEY_STATE_RELEASED = 0,
		WL_KEYBOARD_KEY_STATE_PRESSED = 1,
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct wl_keyboard_listener
	{
		public delegate* unmanaged<IntPtr, IntPtr, WlKeymapFormat, int, uint, void> keymap;
		public delegate* unmanaged<IntPtr, IntPtr, uint, IntPtr, IntPtr, void> enter;
		public delegate* unmanaged<IntPtr, IntPtr, uint, IntPtr, void> leave;
		public delegate* unmanaged<IntPtr, IntPtr, uint, uint, uint, WlKeyState, void> key;
		public delegate* unmanaged<IntPtr, IntPtr, uint, uint, uint, uint, uint, void> modifiers;
	}

	public static int wl_keyboard_add_listener(IntPtr wl_keyboard, IntPtr listener, IntPtr data)
	{
		return wl_proxy_add_listener(wl_keyboard, listener, data);
	}

	public static void wl_keyboard_destroy(IntPtr wl_keyboard)
	{
		wl_proxy_destroy(wl_keyboard);
	}

	[LibraryImport("libxkbcommon.so.0")]
	public static partial IntPtr xkb_context_new(int flags);

	[LibraryImport("libxkbcommon.so.0")]
	public static partial void xkb_context_unref(IntPtr context);

	public enum xkb_keymap_format
	{
		XKB_KEYMAP_FORMAT_TEXT_V1 = 1,
	}

	[LibraryImport("libxkbcommon.so.0")]
	public static partial IntPtr xkb_keymap_new_from_string(IntPtr context, IntPtr str, xkb_keymap_format format, int flags);

	[LibraryImport("libxkbcommon.so.0")]
	public static partial void xkb_keymap_unref(IntPtr keymap);

	public enum xkb_keycode_t : uint
	{
		// subtract 8 to translate to an EvDevScanCode
	}

	public const uint XKB_EVDEV_OFFSET = 8;

	[LibraryImport("libxkbcommon.so.0")]
	public static partial xkb_keycode_t xkb_keymap_min_keycode(IntPtr keymap);

	[LibraryImport("libxkbcommon.so.0")]
	public static partial xkb_keycode_t xkb_keymap_max_keycode(IntPtr keymap);

	[LibraryImport("libxkbcommon.so.0")]
	public static partial IntPtr xkb_state_new(IntPtr keymap);

	[LibraryImport("libxkbcommon.so.0")]
	public static partial void xkb_state_unref(IntPtr state);

	public enum xkb_keysym_t : uint
	{
		XKB_KEY_BackSpace = 0xFF08,
		XKB_KEY_Tab = 0xFF09,
		XKB_KEY_Linefeed = 0xFF0A,
		XKB_KEY_Clear = 0xFF0B,
		XKB_KEY_Return = 0xFF0D,
		XKB_KEY_Pause = 0xFF13,
		XKB_KEY_Scroll_Lock = 0xFF14,
		XKB_KEY_Sys_Req = 0xFF15,
		XKB_KEY_Escape = 0xFF1B,
		XKB_KEY_Delete = 0xFFFF,
		XKB_KEY_Home = 0xFF50,
		XKB_KEY_Left = 0xFF51,
		XKB_KEY_Up = 0xFF52,
		XKB_KEY_Right = 0xFF53,
		XKB_KEY_Down = 0xFF54,
		XKB_KEY_Page_Up = 0xFF55,
		XKB_KEY_Page_Down = 0xFF56,
		XKB_KEY_End = 0xFF57,
		XKB_KEY_Begin = 0xFF58,
		XKB_KEY_Select = 0xFF60,
		XKB_KEY_Print = 0xFF61,
		XKB_KEY_Execute = 0xFF62,
		XKB_KEY_Insert = 0xFF63,
		XKB_KEY_Undo = 0xFF65,
		XKB_KEY_Redo = 0xFF66,
		XKB_KEY_Menu = 0xFF67,
		XKB_KEY_Find = 0xFF68,
		XKB_KEY_Cancel = 0xFF69,
		XKB_KEY_Help = 0xFF6A,
		XKB_KEY_Break = 0xFF6B,
		XKB_KEY_Mode_switch = 0xFF7E,
		XKB_KEY_Num_Lock = 0xFF7F,
		XKB_KEY_KP_Space = 0xFF80,
		XKB_KEY_KP_Tab = 0xFF89,
		XKB_KEY_KP_Enter = 0xFF8D,
		XKB_KEY_KP_F1 = 0xFF91,
		XKB_KEY_KP_F2 = 0xFF92,
		XKB_KEY_KP_F3 = 0xFF93,
		XKB_KEY_KP_F4 = 0xFF94,
		XKB_KEY_KP_Home = 0xFF95,
		XKB_KEY_KP_Left = 0xFF96,
		XKB_KEY_KP_Up = 0xFF97,
		XKB_KEY_KP_Right = 0xFF98,
		XKB_KEY_KP_Down = 0xFF99,
		XKB_KEY_KP_Page_Up = 0xFF9A,
		XKB_KEY_KP_Page_Down = 0xFF9B,
		XKB_KEY_KP_End = 0xFF9C,
		XKB_KEY_KP_Begin = 0xFF9D,
		XKB_KEY_KP_Insert = 0xFF9E,
		XKB_KEY_KP_Delete = 0xFF9F,
		XKB_KEY_KP_Equal = 0xFFBD,
		XKB_KEY_KP_Multiply = 0xFFAA,
		XKB_KEY_KP_Add = 0xFFAB,
		XKB_KEY_KP_Separator = 0xFFAC,
		XKB_KEY_KP_Subtract = 0xFFAD,
		XKB_KEY_KP_Decimal = 0xFFAE,
		XKB_KEY_KP_Divide = 0xFFAF,
		XKB_KEY_KP_0 = 0xFFB0,
		XKB_KEY_KP_1 = 0xFFB1,
		XKB_KEY_KP_2 = 0xFFB2,
		XKB_KEY_KP_3 = 0xFFB3,
		XKB_KEY_KP_4 = 0xFFB4,
		XKB_KEY_KP_5 = 0xFFB5,
		XKB_KEY_KP_6 = 0xFFB6,
		XKB_KEY_KP_7 = 0xFFB7,
		XKB_KEY_KP_8 = 0xFFB8,
		XKB_KEY_KP_9 = 0xFFB9,
		XKB_KEY_F1 = 0xFFBE,
		XKB_KEY_F2 = 0xFFBF,
		XKB_KEY_F3 = 0xFFC0,
		XKB_KEY_F4 = 0xFFC1,
		XKB_KEY_F5 = 0xFFC2,
		XKB_KEY_F6 = 0xFFC3,
		XKB_KEY_F7 = 0xFFC4,
		XKB_KEY_F8 = 0xFFC5,
		XKB_KEY_F9 = 0xFFC6,
		XKB_KEY_F10 = 0xFFC7,
		XKB_KEY_F11 = 0xFFC8,
		XKB_KEY_F12 = 0xFFC9,
		XKB_KEY_F13 = 0xFFCA,
		XKB_KEY_F14 = 0xFFCB,
		XKB_KEY_F15 = 0xFFCC,
		XKB_KEY_F16 = 0xFFCD,
		XKB_KEY_F17 = 0xFFCE,
		XKB_KEY_F18 = 0xFFCF,
		XKB_KEY_F19 = 0xFFD0,
		XKB_KEY_F20 = 0xFFD1,
		XKB_KEY_F21 = 0xFFD2,
		XKB_KEY_F22 = 0xFFD3,
		XKB_KEY_F23 = 0xFFD4,
		XKB_KEY_F24 = 0xFFD5,
		XKB_KEY_F25 = 0xFFD6,
		XKB_KEY_F26 = 0xFFD7,
		XKB_KEY_F27 = 0xFFD8,
		XKB_KEY_F28 = 0xFFD9,
		XKB_KEY_F29 = 0xFFDA,
		XKB_KEY_F30 = 0xFFDB,
		XKB_KEY_F31 = 0xFFDC,
		XKB_KEY_F32 = 0xFFDD,
		XKB_KEY_F33 = 0xFFDE,
		XKB_KEY_F34 = 0xFFDF,
		XKB_KEY_F35 = 0xFFE0,
		XKB_KEY_Shift_L = 0xFFE1,
		XKB_KEY_Shift_R = 0xFFE2,
		XKB_KEY_Control_L = 0xFFE3,
		XKB_KEY_Control_R = 0xFFE4,
		XKB_KEY_Caps_Lock = 0xFFE5,
		XKB_KEY_Shift_Lock = 0xFFE6,
		XKB_KEY_Meta_L = 0xFFE7,
		XKB_KEY_Meta_R = 0xFFE8,
		XKB_KEY_Alt_L = 0xFFE9,
		XKB_KEY_Alt_R = 0xFFEA,
		XKB_KEY_Super_L = 0xFFEB,
		XKB_KEY_Super_R = 0xFFEC,
		XKB_KEY_Hyper_L = 0xFFED,
		XKB_KEY_Hyper_R = 0xFFEE,
		XKB_KEY_space = 0x0020,
		XKB_KEY_exclam = 0x0021,
		XKB_KEY_quotedbl = 0x0022,
		XKB_KEY_numbersign = 0x0023,
		XKB_KEY_dollar = 0x0024,
		XKB_KEY_percent = 0x0025,
		XKB_KEY_ampersand = 0x0026,
		XKB_KEY_apostrophe = 0x0027,
		XKB_KEY_parenleft = 0x0028,
		XKB_KEY_parenright = 0x0029,
		XKB_KEY_asterisk = 0x002A,
		XKB_KEY_plus = 0x002B,
		XKB_KEY_comma = 0x002C,
		XKB_KEY_minus = 0x002D,
		XKB_KEY_period = 0x002E,
		XKB_KEY_slash = 0x002F,
		XKB_KEY_0 = 0x0030,
		XKB_KEY_1 = 0x0031,
		XKB_KEY_2 = 0x0032,
		XKB_KEY_3 = 0x0033,
		XKB_KEY_4 = 0x0034,
		XKB_KEY_5 = 0x0035,
		XKB_KEY_6 = 0x0036,
		XKB_KEY_7 = 0x0037,
		XKB_KEY_8 = 0x0038,
		XKB_KEY_9 = 0x0039,
		XKB_KEY_colon = 0x003A,
		XKB_KEY_semicolon = 0x003B,
		XKB_KEY_less = 0x003C,
		XKB_KEY_equal = 0x003D,
		XKB_KEY_greater = 0x003E,
		XKB_KEY_question = 0x003F,
		XKB_KEY_at = 0x0040,
		XKB_KEY_A = 0x0041,
		XKB_KEY_B = 0x0042,
		XKB_KEY_C = 0x0043,
		XKB_KEY_D = 0x0044,
		XKB_KEY_E = 0x0045,
		XKB_KEY_F = 0x0046,
		XKB_KEY_G = 0x0047,
		XKB_KEY_H = 0x0048,
		XKB_KEY_I = 0x0049,
		XKB_KEY_J = 0x004A,
		XKB_KEY_K = 0x004B,
		XKB_KEY_L = 0x004C,
		XKB_KEY_M = 0x004D,
		XKB_KEY_N = 0x004E,
		XKB_KEY_O = 0x004F,
		XKB_KEY_P = 0x0050,
		XKB_KEY_Q = 0x0051,
		XKB_KEY_R = 0x0052,
		XKB_KEY_S = 0x0053,
		XKB_KEY_T = 0x0054,
		XKB_KEY_U = 0x0055,
		XKB_KEY_V = 0x0056,
		XKB_KEY_W = 0x0057,
		XKB_KEY_X = 0x0058,
		XKB_KEY_Y = 0x0059,
		XKB_KEY_Z = 0x005A,
		XKB_KEY_bracketleft = 0x005B,
		XKB_KEY_backslash = 0x005C,
		XKB_KEY_bracketright = 0x005D,
		XKB_KEY_asciicircum = 0x005E,
		XKB_KEY_underscore = 0x005F,
		XKB_KEY_grave = 0x0060,
		XKB_KEY_a = 0x0061,
		XKB_KEY_b = 0x0062,
		XKB_KEY_c = 0x0063,
		XKB_KEY_d = 0x0064,
		XKB_KEY_e = 0x0065,
		XKB_KEY_f = 0x0066,
		XKB_KEY_g = 0x0067,
		XKB_KEY_h = 0x0068,
		XKB_KEY_i = 0x0069,
		XKB_KEY_j = 0x006A,
		XKB_KEY_k = 0x006B,
		XKB_KEY_l = 0x006C,
		XKB_KEY_m = 0x006D,
		XKB_KEY_n = 0x006E,
		XKB_KEY_o = 0x006F,
		XKB_KEY_p = 0x0070,
		XKB_KEY_q = 0x0071,
		XKB_KEY_r = 0x0072,
		XKB_KEY_s = 0x0073,
		XKB_KEY_t = 0x0074,
		XKB_KEY_u = 0x0075,
		XKB_KEY_v = 0x0076,
		XKB_KEY_w = 0x0077,
		XKB_KEY_x = 0x0078,
		XKB_KEY_y = 0x0079,
		XKB_KEY_z = 0x007A,
		XKB_KEY_braceleft = 0x007B,
		XKB_KEY_bar = 0x007C,
		XKB_KEY_braceright = 0x007D,
		XKB_KEY_asciitilde = 0x007E,
	}

	[LibraryImport("libxkbcommon.so.0")]
	public static partial xkb_keysym_t xkb_state_key_get_one_sym(IntPtr state, xkb_keycode_t key);
}
