using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using static GSR.Input.Keyboards.WlImports;

namespace GSR.Input.Keyboards;

internal sealed class WlKeyInput : IKeyInput
{
	/// <summary>
	/// These map evdev scancodes (based on USB HID scancodes) to our scancodes
	/// </summary>
	private static readonly FrozenDictionary<EvDevScanCode, ScanCode> _evDevScanCodeMap = new Dictionary<EvDevScanCode, ScanCode>
	{
		[EvDevScanCode.KEY_ESC] = ScanCode.SC_ESCAPE,
		[EvDevScanCode.KEY_1] = ScanCode.SC_1,
		[EvDevScanCode.KEY_2] = ScanCode.SC_2,
		[EvDevScanCode.KEY_3] = ScanCode.SC_3,
		[EvDevScanCode.KEY_4] = ScanCode.SC_4,
		[EvDevScanCode.KEY_5] = ScanCode.SC_5,
		[EvDevScanCode.KEY_6] = ScanCode.SC_6,
		[EvDevScanCode.KEY_7] = ScanCode.SC_7,
		[EvDevScanCode.KEY_8] = ScanCode.SC_8,
		[EvDevScanCode.KEY_9] = ScanCode.SC_9,
		[EvDevScanCode.KEY_0] = ScanCode.SC_0,
		[EvDevScanCode.KEY_MINUS] = ScanCode.SC_MINUS,
		[EvDevScanCode.KEY_EQUAL] = ScanCode.SC_EQUALS,
		[EvDevScanCode.KEY_BACKSPACE] = ScanCode.SC_BACKSPACE,
		[EvDevScanCode.KEY_TAB] = ScanCode.SC_TAB,
		[EvDevScanCode.KEY_Q] = ScanCode.SC_Q,
		[EvDevScanCode.KEY_W] = ScanCode.SC_W,
		[EvDevScanCode.KEY_E] = ScanCode.SC_E,
		[EvDevScanCode.KEY_R] = ScanCode.SC_R,
		[EvDevScanCode.KEY_T] = ScanCode.SC_T,
		[EvDevScanCode.KEY_Y] = ScanCode.SC_Y,
		[EvDevScanCode.KEY_U] = ScanCode.SC_U,
		[EvDevScanCode.KEY_I] = ScanCode.SC_I,
		[EvDevScanCode.KEY_O] = ScanCode.SC_O,
		[EvDevScanCode.KEY_P] = ScanCode.SC_P,
		[EvDevScanCode.KEY_LEFTBRACE] = ScanCode.SC_LEFTBRACKET,
		[EvDevScanCode.KEY_RIGHTBRACE] = ScanCode.SC_RIGHTBRACKET,
		[EvDevScanCode.KEY_ENTER] = ScanCode.SC_ENTER,
		[EvDevScanCode.KEY_LEFTCTRL] = ScanCode.SC_LEFTCONTROL,
		[EvDevScanCode.KEY_A] = ScanCode.SC_A,
		[EvDevScanCode.KEY_S] = ScanCode.SC_S,
		[EvDevScanCode.KEY_D] = ScanCode.SC_D,
		[EvDevScanCode.KEY_F] = ScanCode.SC_F,
		[EvDevScanCode.KEY_G] = ScanCode.SC_G,
		[EvDevScanCode.KEY_H] = ScanCode.SC_H,
		[EvDevScanCode.KEY_J] = ScanCode.SC_J,
		[EvDevScanCode.KEY_K] = ScanCode.SC_K,
		[EvDevScanCode.KEY_L] = ScanCode.SC_L,
		[EvDevScanCode.KEY_SEMICOLON] = ScanCode.SC_SEMICOLON,
		[EvDevScanCode.KEY_APOSTROPHE] = ScanCode.SC_APOSTROPHE,
		[EvDevScanCode.KEY_GRAVE] = ScanCode.SC_GRAVE,
		[EvDevScanCode.KEY_LEFTSHIFT] = ScanCode.SC_LEFTSHIFT,
		[EvDevScanCode.KEY_BACKSLASH] = ScanCode.SC_BACKSLASH,
		[EvDevScanCode.KEY_Z] = ScanCode.SC_Z,
		[EvDevScanCode.KEY_X] = ScanCode.SC_X,
		[EvDevScanCode.KEY_C] = ScanCode.SC_C,
		[EvDevScanCode.KEY_V] = ScanCode.SC_V,
		[EvDevScanCode.KEY_B] = ScanCode.SC_B,
		[EvDevScanCode.KEY_N] = ScanCode.SC_N,
		[EvDevScanCode.KEY_M] = ScanCode.SC_M,
		[EvDevScanCode.KEY_COMMA] = ScanCode.SC_COMMA,
		[EvDevScanCode.KEY_DOT] = ScanCode.SC_PERIOD,
		[EvDevScanCode.KEY_SLASH] = ScanCode.SC_SLASH,
		[EvDevScanCode.KEY_RIGHTSHIFT] = ScanCode.SC_RIGHTSHIFT,
		[EvDevScanCode.KEY_KPASTERISK] = ScanCode.SC_MULTIPLY,
		[EvDevScanCode.KEY_LEFTALT] = ScanCode.SC_LEFTALT,
		[EvDevScanCode.KEY_SPACE] = ScanCode.SC_SPACEBAR,
		[EvDevScanCode.KEY_CAPSLOCK] = ScanCode.SC_CAPSLOCK,
		[EvDevScanCode.KEY_F1] = ScanCode.SC_F1,
		[EvDevScanCode.KEY_F2] = ScanCode.SC_F2,
		[EvDevScanCode.KEY_F3] = ScanCode.SC_F3,
		[EvDevScanCode.KEY_F4] = ScanCode.SC_F4,
		[EvDevScanCode.KEY_F5] = ScanCode.SC_F5,
		[EvDevScanCode.KEY_F6] = ScanCode.SC_F6,
		[EvDevScanCode.KEY_F7] = ScanCode.SC_F7,
		[EvDevScanCode.KEY_F8] = ScanCode.SC_F8,
		[EvDevScanCode.KEY_F9] = ScanCode.SC_F9,
		[EvDevScanCode.KEY_F10] = ScanCode.SC_F10,
		[EvDevScanCode.KEY_NUMLOCK] = ScanCode.SC_NUMLOCK,
		[EvDevScanCode.KEY_SCROLLLOCK] = ScanCode.SC_SCROLLLOCK,
		[EvDevScanCode.KEY_KP7] = ScanCode.SC_NUMPAD7,
		[EvDevScanCode.KEY_KP8] = ScanCode.SC_NUMPAD8,
		[EvDevScanCode.KEY_KP9] = ScanCode.SC_NUMPAD9,
		[EvDevScanCode.KEY_KPMINUS] = ScanCode.SC_SUBSTRACT,
		[EvDevScanCode.KEY_KP4] = ScanCode.SC_NUMPAD4,
		[EvDevScanCode.KEY_KP5] = ScanCode.SC_NUMPAD5,
		[EvDevScanCode.KEY_KP6] = ScanCode.SC_NUMPAD6,
		[EvDevScanCode.KEY_KPPLUS] = ScanCode.SC_ADD,
		[EvDevScanCode.KEY_KP1] = ScanCode.SC_NUMPAD1,
		[EvDevScanCode.KEY_KP2] = ScanCode.SC_NUMPAD2,
		[EvDevScanCode.KEY_KP3] = ScanCode.SC_NUMPAD3,
		[EvDevScanCode.KEY_KP0] = ScanCode.SC_NUMPAD0,
		[EvDevScanCode.KEY_KPDOT] = ScanCode.SC_DECIMAL,
		[EvDevScanCode.KEY_ZENKAKUHANKAKU] = ScanCode.SC_F24,
		[EvDevScanCode.KEY_102ND] = ScanCode.SC_EUROPE2,
		[EvDevScanCode.KEY_F11] = ScanCode.SC_F11,
		[EvDevScanCode.KEY_F12] = ScanCode.SC_F12,
		[EvDevScanCode.KEY_RO] = ScanCode.SC_INTL1,
		[EvDevScanCode.KEY_KATAKANA] = ScanCode.SC_LANG3,
		[EvDevScanCode.KEY_HIRAGANA] = ScanCode.SC_LANG4,
		[EvDevScanCode.KEY_HENKAN] = ScanCode.SC_INTL4,
		[EvDevScanCode.KEY_KATAKANAHIRAGANA] = ScanCode.SC_INTL2,
		[EvDevScanCode.KEY_MUHENKAN] = ScanCode.SC_INTL5,
		[EvDevScanCode.KEY_KPJPCOMMA] = ScanCode.SC_INTL6,
		[EvDevScanCode.KEY_KPENTER] = ScanCode.SC_NUMPADENTER,
		[EvDevScanCode.KEY_RIGHTCTRL] = ScanCode.SC_RIGHTCONTROL,
		[EvDevScanCode.KEY_KPSLASH] = ScanCode.SC_DIVIDE,
		[EvDevScanCode.KEY_SYSRQ] = ScanCode.SC_PRINTSCREEN,
		[EvDevScanCode.KEY_RIGHTALT] = ScanCode.SC_RIGHTALT,
		[EvDevScanCode.KEY_HOME] = ScanCode.SC_HOME,
		[EvDevScanCode.KEY_UP] = ScanCode.SC_UP,
		[EvDevScanCode.KEY_PAGEUP] = ScanCode.SC_PAGEUP,
		[EvDevScanCode.KEY_LEFT] = ScanCode.SC_LEFT,
		[EvDevScanCode.KEY_RIGHT] = ScanCode.SC_RIGHT,
		[EvDevScanCode.KEY_END] = ScanCode.SC_END,
		[EvDevScanCode.KEY_DOWN] = ScanCode.SC_DOWN,
		[EvDevScanCode.KEY_PAGEDOWN] = ScanCode.SC_PAGEDOWN,
		[EvDevScanCode.KEY_INSERT] = ScanCode.SC_INSERT,
		[EvDevScanCode.KEY_DELETE] = ScanCode.SC_DELETE,
		[EvDevScanCode.KEY_MUTE] = ScanCode.SC_MUTE,
		[EvDevScanCode.KEY_VOLUMEDOWN] = ScanCode.SC_VOLUMEDOWN,
		[EvDevScanCode.KEY_VOLUMEUP] = ScanCode.SC_VOLUMEUP,
		[EvDevScanCode.KEY_POWER] = ScanCode.SC_POWER,
		[EvDevScanCode.KEY_KPEQUAL] = ScanCode.SC_NUMPADEQUALS,
		[EvDevScanCode.KEY_PAUSE] = ScanCode.SC_PAUSE,
		[EvDevScanCode.KEY_KPCOMMA] = ScanCode.SC_SEPARATOR,
		[EvDevScanCode.KEY_YEN] = ScanCode.SC_INTL3,
		[EvDevScanCode.KEY_LEFTMETA] = ScanCode.SC_LEFTGUI,
		[EvDevScanCode.KEY_RIGHTMETA] = ScanCode.SC_RIGHTGUI,
		[EvDevScanCode.KEY_STOP] = ScanCode.SC_STOP,
		[EvDevScanCode.KEY_MENU] = ScanCode.SC_APPS,
		[EvDevScanCode.KEY_CALC] = ScanCode.SC_CALCULATOR,
		[EvDevScanCode.KEY_SLEEP] = ScanCode.SC_SLEEP,
		[EvDevScanCode.KEY_WAKEUP] = ScanCode.SC_WAKE,
		[EvDevScanCode.KEY_MAIL] = ScanCode.SC_MAIL,
		[EvDevScanCode.KEY_BOOKMARKS] = ScanCode.SC_BROWSERFAVORITES,
		[EvDevScanCode.KEY_COMPUTER] = ScanCode.SC_MYCOMPUTER,
		[EvDevScanCode.KEY_BACK] = ScanCode.SC_BROWSERBACK,
		[EvDevScanCode.KEY_FORWARD] = ScanCode.SC_BROWSERFORWARD,
		[EvDevScanCode.KEY_NEXTSONG] = ScanCode.SC_NEXTTRACK,
		[EvDevScanCode.KEY_PLAYPAUSE] = ScanCode.SC_PLAYPAUSE,
		[EvDevScanCode.KEY_PREVIOUSSONG] = ScanCode.SC_PREVTRACK,
		[EvDevScanCode.KEY_HOMEPAGE] = ScanCode.SC_BROWSERHOME,
		[EvDevScanCode.KEY_REFRESH] = ScanCode.SC_BROWSERREFRESH,
		[EvDevScanCode.KEY_F13] = ScanCode.SC_F13,
		[EvDevScanCode.KEY_F14] = ScanCode.SC_F14,
		[EvDevScanCode.KEY_F15] = ScanCode.SC_F15,
		[EvDevScanCode.KEY_F16] = ScanCode.SC_F16,
		[EvDevScanCode.KEY_F17] = ScanCode.SC_F17,
		[EvDevScanCode.KEY_F18] = ScanCode.SC_F18,
		[EvDevScanCode.KEY_F19] = ScanCode.SC_F19,
		[EvDevScanCode.KEY_F20] = ScanCode.SC_F20,
		[EvDevScanCode.KEY_F21] = ScanCode.SC_F21,
		[EvDevScanCode.KEY_F22] = ScanCode.SC_F22,
		[EvDevScanCode.KEY_F23] = ScanCode.SC_F23,
		[EvDevScanCode.KEY_F24] = ScanCode.SC_F24,
		[EvDevScanCode.KEY_SEARCH] = ScanCode.SC_BROWSERSEARCH,
		[EvDevScanCode.KEY_MEDIA] = ScanCode.SC_MEDIASELECT,
	}.ToFrozenDictionary();

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
	private static void KeyboardKey(IntPtr userdata, IntPtr wlKeyboard, uint serial, uint time, EvDevScanCode key, WlKeyState state)
	{
		if (state is not (WlKeyState.WL_KEYBOARD_KEY_STATE_PRESSED or WlKeyState.WL_KEYBOARD_KEY_STATE_RELEASED))
		{
			return;
		}

		if (_evDevScanCodeMap.TryGetValue(key, out var scancode))
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

	private readonly IntPtr _wlDisplay;
	private readonly IntPtr _wlRegistry;

	private readonly IntPtr XkbContext;

	private IntPtr WlSeat;
	private IntPtr WlKeyboard;
	private IntPtr XkbKeymap;
	private IntPtr XkbState;

	public WlKeyInput()
	{
		_wlDisplay = wl_display_connect(IntPtr.Zero);
		if (_wlDisplay == IntPtr.Zero)
		{
			throw new("Failed to connect to display");
		}

		try
		{
			_wlRegistry = wl_display_get_registry(_wlDisplay);
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
			_ = wl_display_roundtrip(_wlDisplay);

			if (WlSeat == IntPtr.Zero)
			{
				throw new("Failed to obtain seat");
			}

			// sync so we get the keyboard and keymap
			_ = wl_display_roundtrip(_wlDisplay);

			if (WlKeyboard == IntPtr.Zero)
			{
				throw new("Failed to obtain keyboard");
			}

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
				var evDevScanCode = (EvDevScanCode)(i - XKB_EVDEV_OFFSET);
				if (_evDevScanCodeMap.TryGetValue(evDevScanCode, out var scanCode))
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

	public void Dispose()
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

		wl_display_disconnect(_wlDisplay);
	}

	public IEnumerable<KeyEvent> GetEvents()
	{
		// prep reading new events
		// existing events need to be drained for this to succeed
		while (wl_display_prepare_read(_wlDisplay) != 0)
		{
			_ = wl_display_dispatch_pending(_wlDisplay);
		}

		// read and dispatch new events
		_ = wl_display_flush(_wlDisplay);
		_ = wl_display_read_events(_wlDisplay);
		_ = wl_display_dispatch_pending(_wlDisplay);

		var ret = new KeyEvent[KeyEvents.Count];
		KeyEvents.CopyTo(ret.AsSpan());
		KeyEvents.Clear();
		return ret;
	}

	/// <summary>
	/// Fallback scancode string map in case there was no keysym translation
	/// </summary>
	private static readonly FrozenDictionary<ScanCode, string> _scanCodeStrMap = new Dictionary<ScanCode, string>
	{
		[ScanCode.SC_ESCAPE] = "Escape",
		[ScanCode.SC_1] = "1",
		[ScanCode.SC_2] = "2",
		[ScanCode.SC_3] = "3",
		[ScanCode.SC_4] = "4",
		[ScanCode.SC_5] = "5",
		[ScanCode.SC_6] = "6",
		[ScanCode.SC_7] = "7",
		[ScanCode.SC_8] = "8",
		[ScanCode.SC_9] = "9",
		[ScanCode.SC_0] = "0",
		[ScanCode.SC_MINUS] = "Minus",
		[ScanCode.SC_EQUALS] = "Equals",
		[ScanCode.SC_BACKSPACE] = "Backspace",
		[ScanCode.SC_TAB] = "Tab",
		[ScanCode.SC_Q] = "Q",
		[ScanCode.SC_W] = "W",
		[ScanCode.SC_E] = "E",
		[ScanCode.SC_R] = "R",
		[ScanCode.SC_T] = "T",
		[ScanCode.SC_Y] = "Y",
		[ScanCode.SC_U] = "U",
		[ScanCode.SC_I] = "I",
		[ScanCode.SC_O] = "O",
		[ScanCode.SC_P] = "P",
		[ScanCode.SC_LEFTBRACKET] = "Left Bracket",
		[ScanCode.SC_RIGHTBRACKET] = "Right Bracket",
		[ScanCode.SC_ENTER] = "Enter",
		[ScanCode.SC_LEFTCONTROL] = "Left Control",
		[ScanCode.SC_A] = "A",
		[ScanCode.SC_S] = "S",
		[ScanCode.SC_D] = "D",
		[ScanCode.SC_F] = "F",
		[ScanCode.SC_G] = "G",
		[ScanCode.SC_H] = "H",
		[ScanCode.SC_J] = "J",
		[ScanCode.SC_K] = "K",
		[ScanCode.SC_L] = "L",
		[ScanCode.SC_SEMICOLON] = "Semicolon",
		[ScanCode.SC_APOSTROPHE] = "Quotes",
		[ScanCode.SC_GRAVE] = "Tilde",
		[ScanCode.SC_LEFTSHIFT] = "Left Shift",
		[ScanCode.SC_BACKSLASH] = "Pipe",
		[ScanCode.SC_Z] = "Z",
		[ScanCode.SC_X] = "X",
		[ScanCode.SC_C] = "C",
		[ScanCode.SC_V] = "V",
		[ScanCode.SC_B] = "B",
		[ScanCode.SC_N] = "N",
		[ScanCode.SC_M] = "M",
		[ScanCode.SC_COMMA] = "Comma",
		[ScanCode.SC_PERIOD] = "Period",
		[ScanCode.SC_SLASH] = "Question",
		[ScanCode.SC_RIGHTSHIFT] = "Right Shift",
		[ScanCode.SC_MULTIPLY] = "Multiply",
		[ScanCode.SC_LEFTALT] = "Left Alt",
		[ScanCode.SC_SPACEBAR] = "Spacebar",
		[ScanCode.SC_CAPSLOCK] = "Caps Lock",
		[ScanCode.SC_F1] = "F1",
		[ScanCode.SC_F2] = "F2",
		[ScanCode.SC_F3] = "F3",
		[ScanCode.SC_F4] = "F4",
		[ScanCode.SC_F5] = "F5",
		[ScanCode.SC_F6] = "F6",
		[ScanCode.SC_F7] = "F7",
		[ScanCode.SC_F8] = "F8",
		[ScanCode.SC_F9] = "F9",
		[ScanCode.SC_F10] = "F10",
		[ScanCode.SC_NUMLOCK] = "Num Lock",
		[ScanCode.SC_SCROLLLOCK] = "Scroll Lock",
		[ScanCode.SC_NUMPAD7] = "Numpad 7",
		[ScanCode.SC_NUMPAD8] = "Numpad 8",
		[ScanCode.SC_NUMPAD9] = "Numpad 9",
		[ScanCode.SC_SUBSTRACT] = "Substract",
		[ScanCode.SC_NUMPAD4] = "Numpad 4",
		[ScanCode.SC_NUMPAD5] = "Numpad 5",
		[ScanCode.SC_NUMPAD6] = "Numpad 6",
		[ScanCode.SC_ADD] = "Add",
		[ScanCode.SC_NUMPAD1] = "Numpad 1",
		[ScanCode.SC_NUMPAD2] = "Numpad 2",
		[ScanCode.SC_NUMPAD3] = "Numpad 3",
		[ScanCode.SC_NUMPAD0] = "Numpad 0",
		[ScanCode.SC_DECIMAL] = "Decimal",
		[ScanCode.SC_EUROPE2] = "",
		[ScanCode.SC_F11] = "F11",
		[ScanCode.SC_F12] = "F12",
		[ScanCode.SC_INTL1] = "Intl 1",
		[ScanCode.SC_LANG3] = "Lang 3",
		[ScanCode.SC_LANG4] = "Lang 4",
		[ScanCode.SC_INTL4] = "Intl 4",
		[ScanCode.SC_INTL2] = "Intl 2",
		[ScanCode.SC_INTL5] = "Intl 5",
		[ScanCode.SC_INTL6] = "Intl 6",
		[ScanCode.SC_NUMPADENTER] = "Numpad Enter",
		[ScanCode.SC_RIGHTCONTROL] = "Right Control",
		[ScanCode.SC_DIVIDE] = "Divide",
		[ScanCode.SC_PRINTSCREEN] = "Print Screen",
		[ScanCode.SC_RIGHTALT] = "Right Alt",
		[ScanCode.SC_HOME] = "Home",
		[ScanCode.SC_UP] = "Up",
		[ScanCode.SC_PAGEUP] = "Page Up",
		[ScanCode.SC_LEFT] = "Left",
		[ScanCode.SC_RIGHT] = "Right",
		[ScanCode.SC_END] = "End",
		[ScanCode.SC_DOWN] = "Down",
		[ScanCode.SC_PAGEDOWN] = "Page Down",
		[ScanCode.SC_INSERT] = "Insert",
		[ScanCode.SC_DELETE] = "Delete",
		[ScanCode.SC_MUTE] = "Mute",
		[ScanCode.SC_VOLUMEDOWN] = "Volume Down",
		[ScanCode.SC_VOLUMEUP] = "Volume Up",
		[ScanCode.SC_POWER] = "Power",
		[ScanCode.SC_NUMPADEQUALS] = "Numpad Equals",
		[ScanCode.SC_PAUSE] = "Pause",
		[ScanCode.SC_SEPARATOR] = "Separator",
		[ScanCode.SC_INTL3] = "Intl 3",
		[ScanCode.SC_LEFTGUI] = "Left Super",
		[ScanCode.SC_RIGHTGUI] = "Right Super",
		[ScanCode.SC_STOP] = "Stop",
		[ScanCode.SC_APPS] = "Menu",
		[ScanCode.SC_CALCULATOR] = "Calculator",
		[ScanCode.SC_SLEEP] = "Sleep",
		[ScanCode.SC_WAKE] = "Wake",
		[ScanCode.SC_MAIL] = "Mail",
		[ScanCode.SC_BROWSERFAVORITES] = "Favorites",
		[ScanCode.SC_MYCOMPUTER] = "Computer",
		[ScanCode.SC_BROWSERBACK] = "Back",
		[ScanCode.SC_BROWSERFORWARD] = "Forward",
		[ScanCode.SC_NEXTTRACK] = "Next Track",
		[ScanCode.SC_PLAYPAUSE] = "Play/Pause",
		[ScanCode.SC_PREVTRACK] = "Prev Track",
		[ScanCode.SC_BROWSERHOME] = "Browser Home",
		[ScanCode.SC_BROWSERREFRESH] = "Refresh",
		[ScanCode.SC_F13] = "F13",
		[ScanCode.SC_F14] = "F14",
		[ScanCode.SC_F15] = "F15",
		[ScanCode.SC_F16] = "F16",
		[ScanCode.SC_F17] = "F17",
		[ScanCode.SC_F18] = "F18",
		[ScanCode.SC_F19] = "F19",
		[ScanCode.SC_F20] = "F20",
		[ScanCode.SC_F21] = "F21",
		[ScanCode.SC_F22] = "F22",
		[ScanCode.SC_F23] = "F23",
		[ScanCode.SC_F24] = "F24",
		[ScanCode.SC_BROWSERSEARCH] = "Search",
		[ScanCode.SC_MEDIASELECT] = "Media",
	}.ToFrozenDictionary();

	public string ConvertScanCodeToString(ScanCode key)
	{
		return _scanCodeSymStrMap[(byte)key] ?? _scanCodeStrMap.GetValueOrDefault(key);
	}
}
