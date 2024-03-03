using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using static GSR.Input.Keyboards.LibcImports;
using static GSR.Input.Keyboards.WlImports;

namespace GSR.Input.Keyboards;

internal sealed class WlKeyInput : EvDevKeyInput
{
	/// <summary>
	/// These map keysyms to strings
	/// These should be preferred for strings, as these will depend on the keyboard layout
	/// </summary>
	private static readonly FrozenDictionary<xkb_keysym_t, string> _keysymToStrMap = new Dictionary<xkb_keysym_t, string>
	{
		[xkb_keysym_t.XKB_KEY_Escape] = "Escape",
		[xkb_keysym_t.XKB_KEY_Return] = "Enter",
		[xkb_keysym_t.XKB_KEY_space] = "Spacebar",
		[xkb_keysym_t.XKB_KEY_BackSpace] = "Backspace",
		[xkb_keysym_t.XKB_KEY_Shift_L] = "Left Shift",
		[xkb_keysym_t.XKB_KEY_Shift_R] = "Right Shift",
		[xkb_keysym_t.XKB_KEY_Alt_L] = "Left Alt",
		[xkb_keysym_t.XKB_KEY_Alt_R] = "Right Alt",
		[xkb_keysym_t.XKB_KEY_Control_L] = "Left Control",
		[xkb_keysym_t.XKB_KEY_Control_R] = "Right Control",
		[xkb_keysym_t.XKB_KEY_Super_L] = "Left Super",
		[xkb_keysym_t.XKB_KEY_Super_R] = "Right Super",
		[xkb_keysym_t.XKB_KEY_Meta_L] = "Left Alt",
		[xkb_keysym_t.XKB_KEY_Meta_R] = "Right Alt",
		[xkb_keysym_t.XKB_KEY_Mode_switch] = "Right Alt",
		[xkb_keysym_t.XKB_KEY_Menu] = "Menu",
		[xkb_keysym_t.XKB_KEY_Tab] = "Tab",
		[xkb_keysym_t.XKB_KEY_underscore] = "Minus",
		[xkb_keysym_t.XKB_KEY_minus] = "Minus",
		[xkb_keysym_t.XKB_KEY_plus] = "Equals",
		[xkb_keysym_t.XKB_KEY_equal] = "Equals",
		[xkb_keysym_t.XKB_KEY_Caps_Lock] = "Caps Lock",
		[xkb_keysym_t.XKB_KEY_Num_Lock] = "Num Lock",
		[xkb_keysym_t.XKB_KEY_F1] = "F1",
		[xkb_keysym_t.XKB_KEY_F2] = "F2",
		[xkb_keysym_t.XKB_KEY_F3] = "F3",
		[xkb_keysym_t.XKB_KEY_F4] = "F4",
		[xkb_keysym_t.XKB_KEY_F5] = "F5",
		[xkb_keysym_t.XKB_KEY_F6] = "F6",
		[xkb_keysym_t.XKB_KEY_F7] = "F7",
		[xkb_keysym_t.XKB_KEY_F8] = "F8",
		[xkb_keysym_t.XKB_KEY_F9] = "F9",
		[xkb_keysym_t.XKB_KEY_F10] = "F10",
		[xkb_keysym_t.XKB_KEY_F11] = "F11",
		[xkb_keysym_t.XKB_KEY_F12] = "F12",
		[xkb_keysym_t.XKB_KEY_F13] = "F13",
		[xkb_keysym_t.XKB_KEY_F14] = "F14",
		[xkb_keysym_t.XKB_KEY_F15] = "F15",
		[xkb_keysym_t.XKB_KEY_F16] = "F16",
		[xkb_keysym_t.XKB_KEY_F17] = "F17",
		[xkb_keysym_t.XKB_KEY_F18] = "F18",
		[xkb_keysym_t.XKB_KEY_F19] = "F19",
		[xkb_keysym_t.XKB_KEY_F20] = "F20",
		[xkb_keysym_t.XKB_KEY_F21] = "F21",
		[xkb_keysym_t.XKB_KEY_F22] = "F22",
		[xkb_keysym_t.XKB_KEY_F23] = "F23",
		[xkb_keysym_t.XKB_KEY_F24] = "F24",
		[xkb_keysym_t.XKB_KEY_A] = "A",
		[xkb_keysym_t.XKB_KEY_a] = "A",
		[xkb_keysym_t.XKB_KEY_B] = "B",
		[xkb_keysym_t.XKB_KEY_b] = "B",
		[xkb_keysym_t.XKB_KEY_C] = "C",
		[xkb_keysym_t.XKB_KEY_c] = "C",
		[xkb_keysym_t.XKB_KEY_D] = "D",
		[xkb_keysym_t.XKB_KEY_d] = "D",
		[xkb_keysym_t.XKB_KEY_E] = "E",
		[xkb_keysym_t.XKB_KEY_e] = "E",
		[xkb_keysym_t.XKB_KEY_F] = "F",
		[xkb_keysym_t.XKB_KEY_f] = "F",
		[xkb_keysym_t.XKB_KEY_G] = "G",
		[xkb_keysym_t.XKB_KEY_g] = "G",
		[xkb_keysym_t.XKB_KEY_H] = "H",
		[xkb_keysym_t.XKB_KEY_h] = "H",
		[xkb_keysym_t.XKB_KEY_I] = "I",
		[xkb_keysym_t.XKB_KEY_i] = "I",
		[xkb_keysym_t.XKB_KEY_J] = "J",
		[xkb_keysym_t.XKB_KEY_j] = "J",
		[xkb_keysym_t.XKB_KEY_K] = "K",
		[xkb_keysym_t.XKB_KEY_k] = "K",
		[xkb_keysym_t.XKB_KEY_L] = "L",
		[xkb_keysym_t.XKB_KEY_l] = "L",
		[xkb_keysym_t.XKB_KEY_M] = "M",
		[xkb_keysym_t.XKB_KEY_m] = "M",
		[xkb_keysym_t.XKB_KEY_N] = "N",
		[xkb_keysym_t.XKB_KEY_n] = "N",
		[xkb_keysym_t.XKB_KEY_O] = "O",
		[xkb_keysym_t.XKB_KEY_o] = "O",
		[xkb_keysym_t.XKB_KEY_P] = "P",
		[xkb_keysym_t.XKB_KEY_p] = "P",
		[xkb_keysym_t.XKB_KEY_Q] = "Q",
		[xkb_keysym_t.XKB_KEY_q] = "Q",
		[xkb_keysym_t.XKB_KEY_R] = "R",
		[xkb_keysym_t.XKB_KEY_r] = "R",
		[xkb_keysym_t.XKB_KEY_S] = "S",
		[xkb_keysym_t.XKB_KEY_s] = "S",
		[xkb_keysym_t.XKB_KEY_T] = "T",
		[xkb_keysym_t.XKB_KEY_t] = "T",
		[xkb_keysym_t.XKB_KEY_U] = "U",
		[xkb_keysym_t.XKB_KEY_u] = "U",
		[xkb_keysym_t.XKB_KEY_V] = "V",
		[xkb_keysym_t.XKB_KEY_v] = "V",
		[xkb_keysym_t.XKB_KEY_W] = "W",
		[xkb_keysym_t.XKB_KEY_w] = "W",
		[xkb_keysym_t.XKB_KEY_X] = "X",
		[xkb_keysym_t.XKB_KEY_x] = "X",
		[xkb_keysym_t.XKB_KEY_Y] = "Y",
		[xkb_keysym_t.XKB_KEY_y] = "Y",
		[xkb_keysym_t.XKB_KEY_Z] = "Z",
		[xkb_keysym_t.XKB_KEY_z] = "Z",
		[xkb_keysym_t.XKB_KEY_0] = "0",
		[xkb_keysym_t.XKB_KEY_1] = "1",
		[xkb_keysym_t.XKB_KEY_2] = "2",
		[xkb_keysym_t.XKB_KEY_3] = "3",
		[xkb_keysym_t.XKB_KEY_4] = "4",
		[xkb_keysym_t.XKB_KEY_5] = "5",
		[xkb_keysym_t.XKB_KEY_6] = "6",
		[xkb_keysym_t.XKB_KEY_7] = "7",
		[xkb_keysym_t.XKB_KEY_8] = "8",
		[xkb_keysym_t.XKB_KEY_9] = "9",
		[xkb_keysym_t.XKB_KEY_KP_0] = "Numpad 0",
		[xkb_keysym_t.XKB_KEY_KP_1] = "Numpad 1",
		[xkb_keysym_t.XKB_KEY_KP_2] = "Numpad 2",
		[xkb_keysym_t.XKB_KEY_KP_3] = "Numpad 3",
		[xkb_keysym_t.XKB_KEY_KP_4] = "Numpad 4",
		[xkb_keysym_t.XKB_KEY_KP_5] = "Numpad 5",
		[xkb_keysym_t.XKB_KEY_KP_6] = "Numpad 6",
		[xkb_keysym_t.XKB_KEY_KP_7] = "Numpad 7",
		[xkb_keysym_t.XKB_KEY_KP_8] = "Numpad 8",
		[xkb_keysym_t.XKB_KEY_KP_9] = "Numpad 9",
		[xkb_keysym_t.XKB_KEY_Pause] = "Pause",
		[xkb_keysym_t.XKB_KEY_Break] = "Pause",
		[xkb_keysym_t.XKB_KEY_Scroll_Lock] = "Scroll Lock",
		[xkb_keysym_t.XKB_KEY_Insert] = "Insert",
		[xkb_keysym_t.XKB_KEY_Print] = "Print Screen",
		[xkb_keysym_t.XKB_KEY_Sys_Req] = "Print Screen",
		[xkb_keysym_t.XKB_KEY_backslash] = "Pipe",
		[xkb_keysym_t.XKB_KEY_bar] = "Pipe",
		[xkb_keysym_t.XKB_KEY_braceleft] = "Left Bracket",
		[xkb_keysym_t.XKB_KEY_bracketleft] = "Left Bracket",
		[xkb_keysym_t.XKB_KEY_braceright] = "Right Bracket",
		[xkb_keysym_t.XKB_KEY_bracketright] = "Right Bracket",
		[xkb_keysym_t.XKB_KEY_colon] = "Semicolon",
		[xkb_keysym_t.XKB_KEY_semicolon] = "Semicolon",
		[xkb_keysym_t.XKB_KEY_apostrophe] = "Quotes",
		[xkb_keysym_t.XKB_KEY_quotedbl] = "Quotes",
		[xkb_keysym_t.XKB_KEY_grave] = "Tilde",
		[xkb_keysym_t.XKB_KEY_asciitilde] = "Tilde",
		[xkb_keysym_t.XKB_KEY_comma] = "Comma",
		[xkb_keysym_t.XKB_KEY_less] = "Comma",
		[xkb_keysym_t.XKB_KEY_period] = "Period",
		[xkb_keysym_t.XKB_KEY_greater] = "Period",
		[xkb_keysym_t.XKB_KEY_slash] = "Question",
		[xkb_keysym_t.XKB_KEY_question] = "Question",
		[xkb_keysym_t.XKB_KEY_Left] = "Left",
		[xkb_keysym_t.XKB_KEY_Down] = "Down",
		[xkb_keysym_t.XKB_KEY_Right] = "Right",
		[xkb_keysym_t.XKB_KEY_Up] = "Up",
		[xkb_keysym_t.XKB_KEY_Delete] = "Delete",
		[xkb_keysym_t.XKB_KEY_Home] = "Home",
		[xkb_keysym_t.XKB_KEY_End] = "End",
		[xkb_keysym_t.XKB_KEY_Page_Up] = "Page Up",
		[xkb_keysym_t.XKB_KEY_Page_Down] = "Page Down",
		[xkb_keysym_t.XKB_KEY_KP_Add] = "Add",
		[xkb_keysym_t.XKB_KEY_KP_Subtract] = "Subtract",
		[xkb_keysym_t.XKB_KEY_KP_Multiply] = "Multiply",
		[xkb_keysym_t.XKB_KEY_KP_Divide] = "Divide",
		[xkb_keysym_t.XKB_KEY_KP_Decimal] = "Decimal",
		[xkb_keysym_t.XKB_KEY_KP_Insert] = "Numpad 0",
		[xkb_keysym_t.XKB_KEY_KP_End] = "Numpad 1",
		[xkb_keysym_t.XKB_KEY_KP_Down] = "Numpad 2",
		[xkb_keysym_t.XKB_KEY_KP_Page_Down] = "Numpad 3",
		[xkb_keysym_t.XKB_KEY_KP_Left] = "Numpad 4",
		[xkb_keysym_t.XKB_KEY_KP_Right] = "Numpad 6",
		[xkb_keysym_t.XKB_KEY_KP_Home] = "Numpad 7",
		[xkb_keysym_t.XKB_KEY_KP_Up] = "Numpad 8",
		[xkb_keysym_t.XKB_KEY_KP_Page_Up] = "Numpad 9",
		[xkb_keysym_t.XKB_KEY_KP_Delete] = "Decimal",
		[xkb_keysym_t.XKB_KEY_KP_Enter] = "Numpad Enter",
		[xkb_keysym_t.XKB_KEY_parenright] = "0",
		[xkb_keysym_t.XKB_KEY_exclam] = "1",
		[xkb_keysym_t.XKB_KEY_at] = "2",
		[xkb_keysym_t.XKB_KEY_numbersign] = "3",
		[xkb_keysym_t.XKB_KEY_dollar] = "4",
		[xkb_keysym_t.XKB_KEY_percent] = "5",
		[xkb_keysym_t.XKB_KEY_asciicircum] = "6",
		[xkb_keysym_t.XKB_KEY_ampersand] = "7",
		[xkb_keysym_t.XKB_KEY_asterisk] = "8",
		[xkb_keysym_t.XKB_KEY_parenleft] = "9",
	}.ToFrozenDictionary();

	// wayland doesn't save copies of listeners, so these need to be kept alive in unmanaged memory
	private static readonly IntPtr _wlRegistryListener;
	private static readonly IntPtr _wlSeatListener;
	private static readonly IntPtr _wlKeyboardListener;

	static WlKeyInput()
	{
		unsafe
		{
			try
			{
				_wlRegistryListener = (IntPtr)NativeMemory.Alloc((uint)sizeof(wl_registry_listener));
				var wlRegistryListener = (wl_registry_listener*)_wlRegistryListener;
				wlRegistryListener->global = &RegistryGlobal;
				wlRegistryListener->global_remove = &RegistryGlobalRemove;

				_wlSeatListener = (IntPtr)NativeMemory.Alloc((uint)sizeof(wl_seat_listener));
				var wlSeatListener = (wl_seat_listener*)_wlSeatListener;
				wlSeatListener->capabilities = &SeatCapabilities;
				wlSeatListener->name = &SeatName;

				_wlKeyboardListener = (IntPtr)NativeMemory.Alloc((uint)sizeof(wl_keyboard_listener));
				var wlKeyboardListener = (wl_keyboard_listener*)_wlKeyboardListener;
				wlKeyboardListener->keymap = &KeyboardKeymap;
				wlKeyboardListener->enter = &KeyboardEnter;
				wlKeyboardListener->leave = &KeyboardLeave;
				wlKeyboardListener->key = &KeyboardKey;
				wlKeyboardListener->modifiers = &KeyboardModifiers;
			}
			catch
			{
				NativeMemory.Free((void*)_wlRegistryListener);
				NativeMemory.Free((void*)_wlSeatListener);
				NativeMemory.Free((void*)_wlKeyboardListener);
				throw;
			}
		}
	}

	[UnmanagedCallersOnly]
	private static void RegistryGlobal(IntPtr userdata, IntPtr wlRegistry, uint name, IntPtr iface, uint version)
	{
		var handle = GCHandle.FromIntPtr(userdata);
		var wlKeyInput = (WlKeyInput)handle.Target!;

		// ignore this if we already have a seat
		if (wlKeyInput.WlSeat != IntPtr.Zero)
		{
			return;
		}

		var ifaceStr = Marshal.PtrToStringUTF8(iface);
		if (ifaceStr == "wl_seat")
		{
			wlKeyInput.WlSeat = wl_registry_bind(wlRegistry, name, wl_seat_interface, 1);
			if (wlKeyInput.WlSeat == IntPtr.Zero)
			{
				return;
			}

			_ = wl_seat_add_listener(wlKeyInput.WlSeat, _wlSeatListener, userdata);
		}
	}

	[UnmanagedCallersOnly]
	private static void RegistryGlobalRemove(IntPtr userdata, IntPtr wlRegistry, uint name)
	{
		// this is not used for seats
	}

	[UnmanagedCallersOnly]
	private static void SeatCapabilities(IntPtr userdata, IntPtr wlSeat, WlSeatCapabilities capabilities)
	{
		var handle = GCHandle.FromIntPtr(userdata);
		var wlKeyInput = (WlKeyInput)handle.Target!;

		// ignore this if we already have a keyboard
		if (wlKeyInput.WlKeyboard != IntPtr.Zero)
		{
			return;
		}

		if ((capabilities & WlSeatCapabilities.WL_SEAT_CAPABILITY_KEYBOARD) != 0)
		{
			wlKeyInput.WlKeyboard = wl_seat_get_keyboard(wlSeat);
			if (wlKeyInput.WlKeyboard == IntPtr.Zero)
			{
				return;
			}

			_ = wl_keyboard_add_listener(wlKeyInput.WlKeyboard, _wlKeyboardListener, userdata);
		}
	}

	[UnmanagedCallersOnly]
	private static void SeatName(IntPtr userdata, IntPtr wlSeat, IntPtr name)
	{
		// don't care
	}

	[UnmanagedCallersOnly]
	private static void KeyboardKeymap(IntPtr userdata, IntPtr wlKeyboard, WlKeymapFormat format, int fd, uint size)
	{
		var handle = GCHandle.FromIntPtr(userdata);
		var wlKeyInput = (WlKeyInput)handle.Target!;

		// ignore this if we already have a keymap
		if (wlKeyInput.XkbKeymap != IntPtr.Zero)
		{
			_ = close(fd);
			return;
		}

		// this is the only format currently, and it's the only one we support
		if (format != WlKeymapFormat.WL_KEYBOARD_KEYMAP_FORMAT_XKB_V1)
		{
			_ = close(fd);
			return;
		}

		var keymapStr = mmap(IntPtr.Zero, size, PROT_READ, MAP_PRIVATE, fd, 0);
		if (keymapStr == MAP_FAILED)
		{
			_ = close(fd);
			return;
		}

		wlKeyInput.XkbKeymap = xkb_keymap_new_from_string(wlKeyInput.XkbContext, keymapStr, xkb_keymap_format.XKB_KEYMAP_FORMAT_TEXT_V1, 0);
		_ = munmap(keymapStr, size);
		_ = close(fd);

		if (wlKeyInput.XkbKeymap != IntPtr.Zero)
		{
			wlKeyInput.XkbState = xkb_state_new(wlKeyInput.XkbKeymap);
		}
	}

	[UnmanagedCallersOnly]
	private static void KeyboardEnter(IntPtr userdata, IntPtr wlKeyboard, uint serial, IntPtr wlSurface, IntPtr keys)
	{
		// don't care
	}

	[UnmanagedCallersOnly]
	private static void KeyboardLeave(IntPtr userdata, IntPtr wlKeyboard, uint serial, IntPtr wlSurface)
	{
		// don't care
	}

	[UnmanagedCallersOnly]
	private static void KeyboardKey(IntPtr userdata, IntPtr wlKeyboard, uint serial, uint time, uint key, WlKeyState state)
	{
		// if we have root, we'll be deferring keyboard events to our underlying evdev handler
		if (HasRoot && EvDevImports.IsAvailable)
		{
			return;
		}

		if (state is not (WlKeyState.WL_KEYBOARD_KEY_STATE_PRESSED or WlKeyState.WL_KEYBOARD_KEY_STATE_RELEASED))
		{
			return;
		}

		if (key > (uint)EvDevImports.EvDevKeyCode.KEY_MAX)
		{
			return;
		}

		if (EvDevKeyCodeMap.TryGetValue((EvDevImports.EvDevKeyCode)key, out var scancode))
		{
			var handle = GCHandle.FromIntPtr(userdata);
			var wlKeyInput = (WlKeyInput)handle.Target!;
			wlKeyInput.KeyEvents.Add(new(scancode, state == WlKeyState.WL_KEYBOARD_KEY_STATE_PRESSED));
		}
	}

	[UnmanagedCallersOnly]
	private static void KeyboardModifiers(IntPtr userdata, IntPtr wlKeyboard, uint serial, uint modsDepressed, uint modsLatched, uint modsLocked, uint group)
	{
		// don't care
	}

	private readonly string[] _scanCodeSymStrMap = new string[256];
	private readonly List<KeyEvent> KeyEvents = [];

	private readonly bool _ownsDisplay;
	private readonly IntPtr _wlDisplay;
	private readonly IntPtr _wlDisplayProxy;
	private readonly IntPtr _wlEventQueue;
	private readonly IntPtr _wlRegistry;

	private readonly IntPtr XkbContext;

	private IntPtr WlSeat;
	private IntPtr WlKeyboard;
	private IntPtr XkbKeymap;
	private IntPtr XkbState;

	public WlKeyInput(IntPtr wlDisplay)
	{
		_wlDisplay = wlDisplay;

		if (_wlDisplay == IntPtr.Zero)
		{
			_ownsDisplay = true;
			_wlDisplay = wl_display_connect(IntPtr.Zero);
			if (_wlDisplay == IntPtr.Zero)
			{
				throw new("Failed to connect to display");
			}
		}

		try
		{
			// have to create a proxy / separate event queue
			// as we don't want to interfere with SDL's event handling
			_wlDisplayProxy = wl_proxy_create_wrapper(_wlDisplay);
			if (_wlDisplayProxy == IntPtr.Zero)
			{
				throw new("Failed to create display proxy");
			}

			_wlEventQueue = wl_display_create_queue(_wlDisplay);
			if (_wlEventQueue == IntPtr.Zero)
			{
				throw new("Failed to create event queue");
			}

			wl_proxy_set_queue(_wlDisplayProxy, _wlEventQueue);

			_wlRegistry = wl_display_get_registry(_wlDisplayProxy);
			if (_wlRegistry == IntPtr.Zero)
			{
				throw new("Failed to get global registry");
			}

			// TODO: xkb isn't strictly needed (and in theory might not work?), perhaps only do this if we get an xkb keymap?
			XkbContext = xkb_context_new(0);
			if (XkbContext == IntPtr.Zero)
			{
				throw new("Failed to create xkb context");
			}

			var handle = GCHandle.Alloc(this, GCHandleType.Weak);
			_ = wl_registry_add_listener(_wlRegistry, _wlRegistryListener, GCHandle.ToIntPtr(handle));

			// sync so we get the seat
			_ = wl_display_roundtrip_queue(_wlDisplay, _wlEventQueue);

			if (WlSeat == IntPtr.Zero)
			{
				throw new("Failed to obtain seat");
			}

			// sync again for the keyboard
			_ = wl_display_roundtrip_queue(_wlDisplay, _wlEventQueue);

			if (WlKeyboard == IntPtr.Zero)
			{
				throw new("Failed to obtain keyboard");
			}

			// sync again for the keymap
			_ = wl_display_roundtrip_queue(_wlDisplay, _wlEventQueue);

			if (XkbKeymap == IntPtr.Zero)
			{
				throw new("Failed to obtain keymap");
			}

			if (XkbState == IntPtr.Zero)
			{
				throw new("Failed to create xkb state");
			}

			var minKeyCode = (xkb_keycode_t)Math.Max((uint)xkb_keymap_min_keycode(XkbKeymap), XKB_EVDEV_OFFSET);
			var maxKeyCode = (xkb_keycode_t)Math.Min((uint)xkb_keymap_max_keycode(XkbKeymap), 256);
			for (var i = minKeyCode; i <= maxKeyCode; i++)
			{
				var evDevScanCode = (EvDevImports.EvDevKeyCode)(i - XKB_EVDEV_OFFSET);
				if (EvDevKeyCodeMap.TryGetValue(evDevScanCode, out var scanCode))
				{
					var keysym = xkb_state_key_get_one_sym(XkbState, i);
					if (_keysymToStrMap.TryGetValue(keysym, out var keyStr))
					{
						_scanCodeSymStrMap[(byte)scanCode] = keyStr;
					}
				}
			}
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	public override void Dispose()
	{
		if (XkbState != IntPtr.Zero)
		{
			xkb_state_unref(XkbState);
		}

		if (XkbKeymap != IntPtr.Zero)
		{
			xkb_keymap_unref(XkbKeymap);
		}

		if (WlKeyboard != IntPtr.Zero)
		{
			wl_keyboard_destroy(WlKeyboard);
		}

		if (WlSeat != IntPtr.Zero)
		{
			wl_seat_destroy(WlSeat);
		}

		if (XkbContext != IntPtr.Zero)
		{
			xkb_context_unref(XkbContext);
		}

		if (_wlRegistry != IntPtr.Zero)
		{
			wl_registry_destroy(_wlRegistry);
		}

		if (_wlEventQueue != IntPtr.Zero)
		{
			wl_event_queue_destroy(_wlEventQueue);
		}

		if (_wlDisplayProxy != IntPtr.Zero)
		{
			wl_proxy_wrapper_destroy(_wlDisplayProxy);
		}

		if (_ownsDisplay)
		{
			wl_display_disconnect(_wlDisplay);
		}

		base.Dispose();
	}

	public override IEnumerable<KeyEvent> GetEvents()
	{
		// if we have root, then we can use our underlying evdev handler to get inputs 
		if (HasRoot && EvDevImports.IsAvailable)
		{
			return base.GetEvents();
		}

		// prep reading new events
		// existing events need to be drained for this to succeed
		while (wl_display_prepare_read_queue(_wlDisplay, _wlEventQueue) != 0)
		{
			_ = wl_display_dispatch_queue_pending(_wlDisplay, _wlEventQueue);
		}

		// read and dispatch new events
		_ = wl_display_flush(_wlDisplay);
		_ = wl_display_read_events(_wlDisplay);
		_ = wl_display_dispatch_queue_pending(_wlDisplay, _wlEventQueue);

		var ret = new KeyEvent[KeyEvents.Count];
		KeyEvents.CopyTo(ret.AsSpan());
		KeyEvents.Clear();
		return ret;
	}

	public override string ConvertScanCodeToString(ScanCode key)
	{
		return _scanCodeSymStrMap[(byte)key] ?? base.ConvertScanCodeToString(key);
	}
}
