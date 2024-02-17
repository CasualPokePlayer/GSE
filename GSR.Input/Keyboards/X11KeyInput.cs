using System;
using System.Collections.Frozen;
using System.Collections.Generic;

using static GSR.Input.Keyboards.X11Imports;

namespace GSR.Input.Keyboards;

/// <summary>
/// Strategies taken from OpenTK and GLFW
/// </summary>
internal sealed class X11KeyInput : IKeyInput
{
	/// <summary>
	/// These map xkb keycode strings to scancodes
	/// These should be used first, as keycodes correspond to keyboard positions
	/// </summary>
	private static readonly FrozenDictionary<string, ScanCode> _xkbStrToScanCodeMap = new Dictionary<string, ScanCode>
	{
		["TLDE"] = ScanCode.SC_GRAVE,
		["AE01"] = ScanCode.SC_1,
		["AE02"] = ScanCode.SC_2,
		["AE03"] = ScanCode.SC_3,
		["AE04"] = ScanCode.SC_4,
		["AE05"] = ScanCode.SC_5,
		["AE06"] = ScanCode.SC_6,
		["AE07"] = ScanCode.SC_7,
		["AE08"] = ScanCode.SC_8,
		["AE09"] = ScanCode.SC_9,
		["AE10"] = ScanCode.SC_0,
		["AE11"] = ScanCode.SC_MINUS,
		["AE12"] = ScanCode.SC_EQUALS,
		["AD01"] = ScanCode.SC_Q,
		["AD02"] = ScanCode.SC_W,
		["AD03"] = ScanCode.SC_E,
		["AD04"] = ScanCode.SC_R,
		["AD05"] = ScanCode.SC_T,
		["AD06"] = ScanCode.SC_Y,
		["AD07"] = ScanCode.SC_U,
		["AD08"] = ScanCode.SC_I,
		["AD09"] = ScanCode.SC_O,
		["AD10"] = ScanCode.SC_P,
		["AD11"] = ScanCode.SC_LEFTBRACKET,
		["AD12"] = ScanCode.SC_RIGHTBRACKET,
		["AC01"] = ScanCode.SC_A,
		["AC02"] = ScanCode.SC_S,
		["AC03"] = ScanCode.SC_D,
		["AC04"] = ScanCode.SC_F,
		["AC05"] = ScanCode.SC_G,
		["AC06"] = ScanCode.SC_H,
		["AC07"] = ScanCode.SC_J,
		["AC08"] = ScanCode.SC_K,
		["AC09"] = ScanCode.SC_L,
		["AC10"] = ScanCode.SC_SEMICOLON,
		["AC11"] = ScanCode.SC_APOSTROPHE,
		["AB01"] = ScanCode.SC_Z,
		["AB02"] = ScanCode.SC_X,
		["AB03"] = ScanCode.SC_C,
		["AB04"] = ScanCode.SC_V,
		["AB05"] = ScanCode.SC_B,
		["AB06"] = ScanCode.SC_N,
		["AB07"] = ScanCode.SC_M,
		["AB08"] = ScanCode.SC_COMMA,
		["AB09"] = ScanCode.SC_PERIOD,
		["AB10"] = ScanCode.SC_SLASH,
		["BKSL"] = ScanCode.SC_BACKSLASH,
		["LSGT"] = ScanCode.SC_BACKSLASH,
		["SPCE"] = ScanCode.SC_SPACEBAR,
		["ESC\0"] = ScanCode.SC_ESCAPE,
		["RTRN"] = ScanCode.SC_ENTER,
		["TAB\0"] = ScanCode.SC_TAB,
		["BKSP"] = ScanCode.SC_BACKSPACE,
		["INS\0"] = ScanCode.SC_INSERT,
		["DELE"] = ScanCode.SC_DELETE,
		["RGHT"] = ScanCode.SC_RIGHT,
		["LEFT"] = ScanCode.SC_LEFT,
		["DOWN"] = ScanCode.SC_DOWN,
		["UP\0\0"] = ScanCode.SC_UP,
		["PGUP"] = ScanCode.SC_PAGEUP,
		["PGDN"] = ScanCode.SC_PAGEDOWN,
		["HOME"] = ScanCode.SC_HOME,
		["END\0"] = ScanCode.SC_END,
		["CAPS"] = ScanCode.SC_CAPSLOCK,
		["SCLK"] = ScanCode.SC_SCROLLLOCK,
		["NMLK"] = ScanCode.SC_NUMLOCK,
		["PRSC"] = ScanCode.SC_PRINTSCREEN,
		["PAUS"] = ScanCode.SC_PAUSE,
		["FK01"] = ScanCode.SC_F1,
		["FK02"] = ScanCode.SC_F2,
		["FK03"] = ScanCode.SC_F3,
		["FK04"] = ScanCode.SC_F4,
		["FK05"] = ScanCode.SC_F5,
		["FK06"] = ScanCode.SC_F6,
		["FK07"] = ScanCode.SC_F7,
		["FK08"] = ScanCode.SC_F8,
		["FK09"] = ScanCode.SC_F9,
		["FK10"] = ScanCode.SC_F10,
		["FK11"] = ScanCode.SC_F11,
		["FK12"] = ScanCode.SC_F12,
		["FK13"] = ScanCode.SC_F13,
		["FK14"] = ScanCode.SC_F14,
		["FK15"] = ScanCode.SC_F15,
		["FK16"] = ScanCode.SC_F16,
		["FK17"] = ScanCode.SC_F17,
		["FK18"] = ScanCode.SC_F18,
		["FK19"] = ScanCode.SC_F19,
		["FK20"] = ScanCode.SC_F20,
		["FK21"] = ScanCode.SC_F21,
		["FK22"] = ScanCode.SC_F22,
		["FK23"] = ScanCode.SC_F23,
		["FK24"] = ScanCode.SC_F24,
		// ["FK25"] = ScanCode.SC_F25,
		["KP0\0"] = ScanCode.SC_NUMPAD0,
		["KP1\0"] = ScanCode.SC_NUMPAD1,
		["KP2\0"] = ScanCode.SC_NUMPAD2,
		["KP3\0"] = ScanCode.SC_NUMPAD3,
		["KP4\0"] = ScanCode.SC_NUMPAD4,
		["KP5\0"] = ScanCode.SC_NUMPAD5,
		["KP6\0"] = ScanCode.SC_NUMPAD6,
		["KP7\0"] = ScanCode.SC_NUMPAD7,
		["KP8\0"] = ScanCode.SC_NUMPAD8,
		["KP9\0"] = ScanCode.SC_NUMPAD9,
		["KPDL"] = ScanCode.SC_DECIMAL,
		["KPDV"] = ScanCode.SC_DIVIDE,
		["KPMU"] = ScanCode.SC_MULTIPLY,
		["KPSU"] = ScanCode.SC_SUBSTRACT,
		["KPAD"] = ScanCode.SC_ADD,
		["KPEN"] = ScanCode.SC_NUMPADENTER,
		["KPEQ"] = ScanCode.SC_NUMPADEQUALS,
		["LFSH"] = ScanCode.SC_LEFTSHIFT,
		["LCTL"] = ScanCode.SC_LEFTCONTROL,
		["LALT"] = ScanCode.SC_LEFTALT,
		["LWIN"] = ScanCode.SC_LEFTGUI,
		["RTSH"] = ScanCode.SC_RIGHTSHIFT,
		["RCTL"] = ScanCode.SC_RIGHTCONTROL,
		["RALT"] = ScanCode.SC_RIGHTALT,
		["LVL3"] = ScanCode.SC_RIGHTALT,
		["MDSW"] = ScanCode.SC_RIGHTALT,
		["RWIN"] = ScanCode.SC_RIGHTGUI,
		["MENU"] = ScanCode.SC_APPS,
	}.ToFrozenDictionary();

	/// <summary>
	/// These map keysyms to scancodes
	/// These should be used only as a fallback, as keysyms change depending on the keyboard layout
	/// </summary>
	private static readonly FrozenDictionary<Keysym, ScanCode> _keysymToScanCodeMap = new Dictionary<Keysym, ScanCode>
	{
		[Keysym.Escape] = ScanCode.SC_ESCAPE,
		[Keysym.Return] = ScanCode.SC_ENTER,
		[Keysym.space] = ScanCode.SC_SPACEBAR,
		[Keysym.BackSpace] = ScanCode.SC_BACKSPACE,
		[Keysym.Shift_L] = ScanCode.SC_LEFTSHIFT,
		[Keysym.Shift_R] = ScanCode.SC_RIGHTSHIFT,
		[Keysym.Alt_L] = ScanCode.SC_LEFTALT,
		[Keysym.Alt_R] = ScanCode.SC_RIGHTALT,
		[Keysym.Control_L] = ScanCode.SC_LEFTCONTROL,
		[Keysym.Control_R] = ScanCode.SC_RIGHTCONTROL,
		[Keysym.Super_L] = ScanCode.SC_LEFTGUI,
		[Keysym.Super_R] = ScanCode.SC_RIGHTGUI,
		[Keysym.Meta_L] = ScanCode.SC_LEFTALT,
		[Keysym.Meta_R] = ScanCode.SC_RIGHTALT,
		[Keysym.Mode_switch] = ScanCode.SC_RIGHTALT,
		[Keysym.ISO_Level3_Shift] = ScanCode.SC_RIGHTALT,
		[Keysym.Menu] = ScanCode.SC_APPS,
		[Keysym.Tab] = ScanCode.SC_TAB,
		[Keysym.underscore] = ScanCode.SC_MINUS,
		[Keysym.minus] = ScanCode.SC_MINUS,
		[Keysym.plus] = ScanCode.SC_EQUALS,
		[Keysym.equal] = ScanCode.SC_EQUALS,
		[Keysym.Caps_Lock] = ScanCode.SC_CAPSLOCK,
		[Keysym.Num_Lock] = ScanCode.SC_NUMLOCK,
		[Keysym.F1] = ScanCode.SC_F1,
		[Keysym.F2] = ScanCode.SC_F2,
		[Keysym.F3] = ScanCode.SC_F3,
		[Keysym.F4] = ScanCode.SC_F4,
		[Keysym.F5] = ScanCode.SC_F5,
		[Keysym.F6] = ScanCode.SC_F6,
		[Keysym.F7] = ScanCode.SC_F7,
		[Keysym.F8] = ScanCode.SC_F8,
		[Keysym.F9] = ScanCode.SC_F9,
		[Keysym.F10] = ScanCode.SC_F10,
		[Keysym.F11] = ScanCode.SC_F11,
		[Keysym.F12] = ScanCode.SC_F12,
		[Keysym.F13] = ScanCode.SC_F13,
		[Keysym.F14] = ScanCode.SC_F14,
		[Keysym.F15] = ScanCode.SC_F15,
		[Keysym.F16] = ScanCode.SC_F16,
		[Keysym.F17] = ScanCode.SC_F17,
		[Keysym.F18] = ScanCode.SC_F18,
		[Keysym.F19] = ScanCode.SC_F19,
		[Keysym.F20] = ScanCode.SC_F20,
		[Keysym.F21] = ScanCode.SC_F21,
		[Keysym.F22] = ScanCode.SC_F22,
		[Keysym.F23] = ScanCode.SC_F23,
		[Keysym.F24] = ScanCode.SC_F24,
		[Keysym.A] = ScanCode.SC_A,
		[Keysym.a] = ScanCode.SC_A,
		[Keysym.B] = ScanCode.SC_B,
		[Keysym.b] = ScanCode.SC_B,
		[Keysym.C] = ScanCode.SC_C,
		[Keysym.c] = ScanCode.SC_C,
		[Keysym.D] = ScanCode.SC_D,
		[Keysym.d] = ScanCode.SC_D,
		[Keysym.E] = ScanCode.SC_E,
		[Keysym.e] = ScanCode.SC_E,
		[Keysym.F] = ScanCode.SC_F,
		[Keysym.f] = ScanCode.SC_F,
		[Keysym.G] = ScanCode.SC_G,
		[Keysym.g] = ScanCode.SC_G,
		[Keysym.H] = ScanCode.SC_H,
		[Keysym.h] = ScanCode.SC_H,
		[Keysym.I] = ScanCode.SC_I,
		[Keysym.i] = ScanCode.SC_I,
		[Keysym.J] = ScanCode.SC_J,
		[Keysym.j] = ScanCode.SC_J,
		[Keysym.K] = ScanCode.SC_K,
		[Keysym.k] = ScanCode.SC_K,
		[Keysym.L] = ScanCode.SC_L,
		[Keysym.l] = ScanCode.SC_L,
		[Keysym.M] = ScanCode.SC_M,
		[Keysym.m] = ScanCode.SC_M,
		[Keysym.N] = ScanCode.SC_N,
		[Keysym.n] = ScanCode.SC_N,
		[Keysym.O] = ScanCode.SC_O,
		[Keysym.o] = ScanCode.SC_O,
		[Keysym.P] = ScanCode.SC_P,
		[Keysym.p] = ScanCode.SC_P,
		[Keysym.Q] = ScanCode.SC_Q,
		[Keysym.q] = ScanCode.SC_Q,
		[Keysym.R] = ScanCode.SC_R,
		[Keysym.r] = ScanCode.SC_R,
		[Keysym.S] = ScanCode.SC_S,
		[Keysym.s] = ScanCode.SC_S,
		[Keysym.T] = ScanCode.SC_T,
		[Keysym.t] = ScanCode.SC_T,
		[Keysym.U] = ScanCode.SC_U,
		[Keysym.u] = ScanCode.SC_U,
		[Keysym.V] = ScanCode.SC_V,
		[Keysym.v] = ScanCode.SC_V,
		[Keysym.W] = ScanCode.SC_W,
		[Keysym.w] = ScanCode.SC_W,
		[Keysym.X] = ScanCode.SC_X,
		[Keysym.x] = ScanCode.SC_X,
		[Keysym.Y] = ScanCode.SC_Y,
		[Keysym.y] = ScanCode.SC_Y,
		[Keysym.Z] = ScanCode.SC_Z,
		[Keysym.z] = ScanCode.SC_Z,
		[Keysym.Number0] = ScanCode.SC_0,
		[Keysym.Number1] = ScanCode.SC_1,
		[Keysym.Number2] = ScanCode.SC_2,
		[Keysym.Number3] = ScanCode.SC_3,
		[Keysym.Number4] = ScanCode.SC_4,
		[Keysym.Number5] = ScanCode.SC_5,
		[Keysym.Number6] = ScanCode.SC_6,
		[Keysym.Number7] = ScanCode.SC_7,
		[Keysym.Number8] = ScanCode.SC_8,
		[Keysym.Number9] = ScanCode.SC_9,
		[Keysym.KP_0] = ScanCode.SC_NUMPAD0,
		[Keysym.KP_1] = ScanCode.SC_NUMPAD1,
		[Keysym.KP_2] = ScanCode.SC_NUMPAD2,
		[Keysym.KP_3] = ScanCode.SC_NUMPAD3,
		[Keysym.KP_4] = ScanCode.SC_NUMPAD4,
		[Keysym.KP_5] = ScanCode.SC_NUMPAD5,
		[Keysym.KP_6] = ScanCode.SC_NUMPAD6,
		[Keysym.KP_7] = ScanCode.SC_NUMPAD7,
		[Keysym.KP_8] = ScanCode.SC_NUMPAD8,
		[Keysym.KP_9] = ScanCode.SC_NUMPAD9,
		[Keysym.Pause] = ScanCode.SC_PAUSE,
		[Keysym.Break] = ScanCode.SC_PAUSE,
		[Keysym.Scroll_Lock] = ScanCode.SC_SCROLLLOCK,
		[Keysym.Insert] = ScanCode.SC_INSERT,
		[Keysym.Print] = ScanCode.SC_PRINTSCREEN,
		[Keysym.Sys_Req] = ScanCode.SC_PRINTSCREEN,
		[Keysym.backslash] = ScanCode.SC_BACKSLASH,
		[Keysym.bar] = ScanCode.SC_BACKSLASH,
		[Keysym.braceleft] = ScanCode.SC_LEFTBRACKET,
		[Keysym.bracketleft] = ScanCode.SC_LEFTBRACKET,
		[Keysym.braceright] = ScanCode.SC_RIGHTBRACKET,
		[Keysym.bracketright] = ScanCode.SC_RIGHTBRACKET,
		[Keysym.colon] = ScanCode.SC_SEMICOLON,
		[Keysym.semicolon] = ScanCode.SC_SEMICOLON,
		[Keysym.apostrophe] = ScanCode.SC_APOSTROPHE,
		[Keysym.quotedbl] = ScanCode.SC_APOSTROPHE,
		[Keysym.grave] = ScanCode.SC_GRAVE,
		[Keysym.asciitilde] = ScanCode.SC_GRAVE,
		[Keysym.comma] = ScanCode.SC_COMMA,
		[Keysym.less] = ScanCode.SC_COMMA,
		[Keysym.period] = ScanCode.SC_PERIOD,
		[Keysym.greater] = ScanCode.SC_PERIOD,
		[Keysym.slash] = ScanCode.SC_SLASH,
		[Keysym.question] = ScanCode.SC_SLASH,
		[Keysym.Left] = ScanCode.SC_LEFT,
		[Keysym.Down] = ScanCode.SC_DOWN,
		[Keysym.Right] = ScanCode.SC_RIGHT,
		[Keysym.Up] = ScanCode.SC_UP,
		[Keysym.Delete] = ScanCode.SC_DELETE,
		[Keysym.Home] = ScanCode.SC_HOME,
		[Keysym.End] = ScanCode.SC_END,
		[Keysym.Page_Up] = ScanCode.SC_PAGEUP,
		[Keysym.Page_Down] = ScanCode.SC_PAGEDOWN,
		[Keysym.KP_Add] = ScanCode.SC_ADD,
		[Keysym.KP_Subtract] = ScanCode.SC_SUBSTRACT,
		[Keysym.KP_Multiply] = ScanCode.SC_MULTIPLY,
		[Keysym.KP_Divide] = ScanCode.SC_DIVIDE,
		[Keysym.KP_Separator] = ScanCode.SC_SEPARATOR,
		[Keysym.KP_Decimal] = ScanCode.SC_DECIMAL,
		[Keysym.KP_Insert] = ScanCode.SC_NUMPAD0,
		[Keysym.KP_End] = ScanCode.SC_NUMPAD1,
		[Keysym.KP_Down] = ScanCode.SC_NUMPAD2,
		[Keysym.KP_Page_Down] = ScanCode.SC_NUMPAD3,
		[Keysym.KP_Left] = ScanCode.SC_NUMPAD4,
		[Keysym.KP_Right] = ScanCode.SC_NUMPAD6,
		[Keysym.KP_Home] = ScanCode.SC_NUMPAD7,
		[Keysym.KP_Up] = ScanCode.SC_NUMPAD8,
		[Keysym.KP_Page_Up] = ScanCode.SC_NUMPAD9,
		[Keysym.KP_Delete] = ScanCode.SC_DECIMAL,
		[Keysym.KP_Enter] = ScanCode.SC_NUMPADENTER,
		[Keysym.parenright] = ScanCode.SC_0,
		[Keysym.exclam] = ScanCode.SC_1,
		[Keysym.at] = ScanCode.SC_2,
		[Keysym.numbersign] = ScanCode.SC_3,
		[Keysym.dollar] = ScanCode.SC_4,
		[Keysym.percent] = ScanCode.SC_5,
		[Keysym.asciicircum] = ScanCode.SC_6,
		[Keysym.ampersand] = ScanCode.SC_7,
		[Keysym.asterisk] = ScanCode.SC_8,
		[Keysym.parenleft] = ScanCode.SC_9,
	}.ToFrozenDictionary();

	/// <summary>
	/// These map keysyms to strings
	/// These should be preferred for strings, as these will depend on the keyboard layout
	/// </summary>
	private static readonly FrozenDictionary<Keysym, string> _keysymToStrMap = new Dictionary<Keysym, string>
	{
		[Keysym.Escape] = "Escape",
		[Keysym.Return] = "Enter",
		[Keysym.space] = "Spacebar",
		[Keysym.BackSpace] = "Backspace",
		[Keysym.Shift_L] = "Left Shift",
		[Keysym.Shift_R] = "Right Shift",
		[Keysym.Alt_L] = "Left Alt",
		[Keysym.Alt_R] = "Right Alt",
		[Keysym.Control_L] = "Left Control",
		[Keysym.Control_R] = "Right Control",
		[Keysym.Super_L] = "Left Super",
		[Keysym.Super_R] = "Right Super",
		[Keysym.Meta_L] = "Left Alt",
		[Keysym.Meta_R] = "Right Alt",
		[Keysym.Mode_switch] = "Right Alt",
		[Keysym.ISO_Level3_Shift] = "Right Alt",
		[Keysym.Menu] = "Menu",
		[Keysym.Tab] = "Tab",
		[Keysym.underscore] = "Minus",
		[Keysym.minus] = "Minus",
		[Keysym.plus] = "Equals",
		[Keysym.equal] = "Equals",
		[Keysym.Caps_Lock] = "Caps Lock",
		[Keysym.Num_Lock] = "Num Lock",
		[Keysym.F1] = "F1",
		[Keysym.F2] = "F2",
		[Keysym.F3] = "F3",
		[Keysym.F4] = "F4",
		[Keysym.F5] = "F5",
		[Keysym.F6] = "F6",
		[Keysym.F7] = "F7",
		[Keysym.F8] = "F8",
		[Keysym.F9] = "F9",
		[Keysym.F10] = "F10",
		[Keysym.F11] = "F11",
		[Keysym.F12] = "F12",
		[Keysym.F13] = "F13",
		[Keysym.F14] = "F14",
		[Keysym.F15] = "F15",
		[Keysym.F16] = "F16",
		[Keysym.F17] = "F17",
		[Keysym.F18] = "F18",
		[Keysym.F19] = "F19",
		[Keysym.F20] = "F20",
		[Keysym.F21] = "F21",
		[Keysym.F22] = "F22",
		[Keysym.F23] = "F23",
		[Keysym.F24] = "F24",
		[Keysym.A] = "A",
		[Keysym.a] = "A",
		[Keysym.B] = "B",
		[Keysym.b] = "B",
		[Keysym.C] = "C",
		[Keysym.c] = "C",
		[Keysym.D] = "D",
		[Keysym.d] = "D",
		[Keysym.E] = "E",
		[Keysym.e] = "E",
		[Keysym.F] = "F",
		[Keysym.f] = "F",
		[Keysym.G] = "G",
		[Keysym.g] = "G",
		[Keysym.H] = "H",
		[Keysym.h] = "H",
		[Keysym.I] = "I",
		[Keysym.i] = "I",
		[Keysym.J] = "J",
		[Keysym.j] = "J",
		[Keysym.K] = "K",
		[Keysym.k] = "K",
		[Keysym.L] = "L",
		[Keysym.l] = "L",
		[Keysym.M] = "M",
		[Keysym.m] = "M",
		[Keysym.N] = "N",
		[Keysym.n] = "N",
		[Keysym.O] = "O",
		[Keysym.o] = "O",
		[Keysym.P] = "P",
		[Keysym.p] = "P",
		[Keysym.Q] = "Q",
		[Keysym.q] = "Q",
		[Keysym.R] = "R",
		[Keysym.r] = "R",
		[Keysym.S] = "S",
		[Keysym.s] = "S",
		[Keysym.T] = "T",
		[Keysym.t] = "T",
		[Keysym.U] = "U",
		[Keysym.u] = "U",
		[Keysym.V] = "V",
		[Keysym.v] = "V",
		[Keysym.W] = "W",
		[Keysym.w] = "W",
		[Keysym.X] = "X",
		[Keysym.x] = "X",
		[Keysym.Y] = "Y",
		[Keysym.y] = "Y",
		[Keysym.Z] = "Z",
		[Keysym.z] = "Z",
		[Keysym.Number0] = "0",
		[Keysym.Number1] = "1",
		[Keysym.Number2] = "2",
		[Keysym.Number3] = "3",
		[Keysym.Number4] = "4",
		[Keysym.Number5] = "5",
		[Keysym.Number6] = "6",
		[Keysym.Number7] = "7",
		[Keysym.Number8] = "8",
		[Keysym.Number9] = "9",
		[Keysym.KP_0] = "Numpad 0",
		[Keysym.KP_1] = "Numpad 1",
		[Keysym.KP_2] = "Numpad 2",
		[Keysym.KP_3] = "Numpad 3",
		[Keysym.KP_4] = "Numpad 4",
		[Keysym.KP_5] = "Numpad 5",
		[Keysym.KP_6] = "Numpad 6",
		[Keysym.KP_7] = "Numpad 7",
		[Keysym.KP_8] = "Numpad 8",
		[Keysym.KP_9] = "Numpad 9",
		[Keysym.Pause] = "Pause",
		[Keysym.Break] = "Pause",
		[Keysym.Scroll_Lock] = "Scroll Lock",
		[Keysym.Insert] = "Insert",
		[Keysym.Print] = "Print Screen",
		[Keysym.Sys_Req] = "Print Screen",
		[Keysym.backslash] = "Pipe",
		[Keysym.bar] = "Pipe",
		[Keysym.braceleft] = "Left Bracket",
		[Keysym.bracketleft] = "Left Bracket",
		[Keysym.braceright] = "Right Bracket",
		[Keysym.bracketright] = "Right Bracket",
		[Keysym.colon] = "Semicolon",
		[Keysym.semicolon] = "Semicolon",
		[Keysym.apostrophe] = "Quotes",
		[Keysym.quotedbl] = "Quotes",
		[Keysym.grave] = "Tilde",
		[Keysym.asciitilde] = "Tilde",
		[Keysym.comma] = "Comma",
		[Keysym.less] = "Comma",
		[Keysym.period] = "Period",
		[Keysym.greater] = "Period",
		[Keysym.slash] = "Question",
		[Keysym.question] = "Question",
		[Keysym.Left] = "Left",
		[Keysym.Down] = "Down",
		[Keysym.Right] = "Right",
		[Keysym.Up] = "Up",
		[Keysym.Delete] = "Delete",
		[Keysym.Home] = "Home",
		[Keysym.End] = "End",
		[Keysym.Page_Up] = "Page Up",
		[Keysym.Page_Down] = "Page Down",
		[Keysym.KP_Add] = "Add",
		[Keysym.KP_Subtract] = "Subtract",
		[Keysym.KP_Multiply] = "Multiply",
		[Keysym.KP_Divide] = "Divide",
		[Keysym.KP_Decimal] = "Decimal",
		[Keysym.KP_Insert] = "Numpad 0",
		[Keysym.KP_End] = "Numpad 1",
		[Keysym.KP_Down] = "Numpad 2",
		[Keysym.KP_Page_Down] = "Numpad 3",
		[Keysym.KP_Left] = "Numpad 4",
		[Keysym.KP_Right] = "Numpad 6",
		[Keysym.KP_Home] = "Numpad 7",
		[Keysym.KP_Up] = "Numpad 8",
		[Keysym.KP_Page_Up] = "Numpad 9",
		[Keysym.KP_Delete] = "Decimal",
		[Keysym.KP_Enter] = "Numpad Enter",
		[Keysym.parenright] = "0",
		[Keysym.exclam] = "1",
		[Keysym.at] = "2",
		[Keysym.numbersign] = "3",
		[Keysym.dollar] = "4",
		[Keysym.percent] = "5",
		[Keysym.asciicircum] = "6",
		[Keysym.ampersand] = "7",
		[Keysym.asterisk] = "8",
		[Keysym.parenleft] = "9",
	}.ToFrozenDictionary();

	private readonly ScanCode[] _keyToScanCodeMap = new ScanCode[256];
	private readonly string[] _scanCodeSymStrMap = new string[256];

	private readonly IntPtr _display;
	private readonly bool[] _lastKeyState = new bool[256];

	public X11KeyInput()
	{
		_display = XOpenDisplay(IntPtr.Zero);
		if (_display == IntPtr.Zero)
		{
			throw new("Failed to open display");
		}

		try
		{
			using (new XLock(_display))
			{
				// check if we can use xkb
				int major = 1, minor = 0;
				var supportsXkb = XkbQueryExtension(_display, out _, out _, out _, ref major, ref minor);

				int keyCodeMin, keyCodeMax;
				if (supportsXkb)
				{
					// we generally want this behavior, as it prevents xkb from
					// generating fake KeyRelease events to go with fake auto repeat KeyPress events
					// not particularly bad if this isn't actually supported, so we don't care if this call fails
					_ = XkbSetDetectableAutoRepeat(_display, true, out _);

					unsafe
					{
						var keyboard = XkbAllocKeyboard(_display);
						if (keyboard == null)
						{
							throw new("Failed to allocate Xkb keyboard");
						}

						try
						{
							var status = XkbGetNames(_display, 0x3FF, keyboard);
							if (status != 0)
							{
								throw new($"Failed to get Xkb names, error code: {status}");
							}

							keyCodeMin = keyboard->min_key_code;
							keyCodeMax = keyboard->max_key_code;
							for (var i = keyCodeMin; i <= keyCodeMax; i++)
							{
								var name = new string(keyboard->names->keys[i].name, 0, XkbKeyNameLength);
								if (_xkbStrToScanCodeMap.TryGetValue(name, out var scanCode))
								{
									_keyToScanCodeMap[i] = scanCode;
									continue;
								}

								for (var j = 0; j < keyboard->names->num_key_aliases; j++)
								{
									var real = new string(keyboard->names->key_aliases[i].real, 0, XkbKeyNameLength);
									if (name == real)
									{
										var alias = new string(keyboard->names->key_aliases[i].alias, 0, XkbKeyNameLength);
										if (_xkbStrToScanCodeMap.TryGetValue(alias, out scanCode))
										{
											_keyToScanCodeMap[i] = scanCode;
											break;
										}
									}
								}
							}
						}
						finally
						{
							XkbFreeKeyboard(keyboard, 0, true);
						}
					}
				}
				else
				{
					_ = XDisplayKeycodes(_display, out keyCodeMin, out keyCodeMax);
				}

				unsafe
				{
					var keysyms = XGetKeyboardMapping(_display, (uint)keyCodeMin, keyCodeMax - keyCodeMin + 1, out var keysymsPerKeycode);
					if (keysyms == null)
					{
						throw new("Failed to obtain X keyboard mapping");
					}

					try
					{
						for (var i = keyCodeMin; i <= keyCodeMax; i++)
						{
							static Keysym FindKeysym(nuint* keysyms, bool hasNumpad)
							{
								if (!hasNumpad)
								{
									return (Keysym)keysyms[0];
								}

								return (Keysym)keysyms[1] switch
								{
									Keysym.KP_0 => Keysym.KP_0,
									Keysym.KP_1 => Keysym.KP_1,
									Keysym.KP_2 => Keysym.KP_2,
									Keysym.KP_3 => Keysym.KP_3,
									Keysym.KP_4 => Keysym.KP_4,
									Keysym.KP_5 => Keysym.KP_5,
									Keysym.KP_6 => Keysym.KP_6,
									Keysym.KP_7 => Keysym.KP_7,
									Keysym.KP_8 => Keysym.KP_8,
									Keysym.KP_9 => Keysym.KP_9,
									Keysym.KP_Separator => Keysym.KP_Separator,
									Keysym.KP_Decimal => Keysym.KP_Decimal,
									Keysym.KP_Equal => Keysym.KP_Equal,
									Keysym.KP_Enter => Keysym.KP_Enter,
									_ => (Keysym)keysyms[0],
								};
							}

							var keysym = FindKeysym(&keysyms[(i - keyCodeMin) * keysymsPerKeycode], keysymsPerKeycode > 1);

							if (_keyToScanCodeMap[i] == 0)
							{
								_keyToScanCodeMap[i] = _keysymToScanCodeMap.GetValueOrDefault(keysym);
							}

							if (_keyToScanCodeMap[i] != 0)
							{
								_scanCodeSymStrMap[i] = _keysymToStrMap.GetValueOrDefault(keysym);
							}
						}
					}
					finally
					{
						_ = XFree((IntPtr)keysyms);
					}
				}
			}
		}
		catch
		{
			_ = XCloseDisplay(_display);
			throw;
		}
	}

	public void Dispose()
	{
		_ = XCloseDisplay(_display);
	}

	public IEnumerable<KeyEvent> GetEvents()
	{
		Span<byte> keys = stackalloc byte[32];

		using (new XLock(_display))
		{
			_ = XQueryKeymap(_display, keys);
		}

		var keyEvents = new List<KeyEvent>();
		for (var keycode = 0; keycode < 256; keycode++)
		{
			var scanCode = _keyToScanCodeMap[keycode];
			if (scanCode != 0)
			{
				var keystate = (keys[keycode >> 3] >> (keycode & 0x07) & 0x01) != 0;
				if (_lastKeyState[keycode] != keystate)
				{
					keyEvents.Add(new(scanCode, IsPressed: keystate));
					_lastKeyState[keycode] = keystate;
				}
			}
		}

		return keyEvents;
	}

	/// <summary>
	/// Fallback scancode string map in case there was no keysym translation
	/// </summary>
	private static readonly FrozenDictionary<ScanCode, string> _scanCodeStrMap = new Dictionary<ScanCode, string>
	{
		[ScanCode.SC_GRAVE] = "Tilde",
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
		[ScanCode.SC_BACKSLASH] = "Pipe",
		[ScanCode.SC_SPACEBAR] = "Spacebar",
		[ScanCode.SC_ESCAPE] = "Escape",
		[ScanCode.SC_ENTER] = "Enter",
		[ScanCode.SC_TAB] = "Tab",
		[ScanCode.SC_BACKSPACE] = "Backspace",
		[ScanCode.SC_INSERT] = "Insert",
		[ScanCode.SC_DELETE] = "Delete",
		[ScanCode.SC_RIGHT] = "Right",
		[ScanCode.SC_LEFT] = "Left",
		[ScanCode.SC_DOWN] = "Down",
		[ScanCode.SC_UP] = "Up",
		[ScanCode.SC_PAGEUP] = "Page Up",
		[ScanCode.SC_PAGEDOWN] = "Page Down",
		[ScanCode.SC_HOME] = "Home",
		[ScanCode.SC_END] = "End",
		[ScanCode.SC_CAPSLOCK] = "Caps Lock",
		[ScanCode.SC_SCROLLLOCK] = "Scroll Lock",
		[ScanCode.SC_NUMLOCK] = "Num Lock",
		[ScanCode.SC_PRINTSCREEN] = "Print Screen",
		[ScanCode.SC_PAUSE] = "Pause",
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
		[ScanCode.SC_F11] = "F11",
		[ScanCode.SC_F12] = "F12",
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
		[ScanCode.SC_NUMPAD0] = "Numpad 0",
		[ScanCode.SC_NUMPAD1] = "Numpad 1",
		[ScanCode.SC_NUMPAD2] = "Numpad 2",
		[ScanCode.SC_NUMPAD3] = "Numpad 3",
		[ScanCode.SC_NUMPAD4] = "Numpad 4",
		[ScanCode.SC_NUMPAD5] = "Numpad 5",
		[ScanCode.SC_NUMPAD6] = "Numpad 6",
		[ScanCode.SC_NUMPAD7] = "Numpad 7",
		[ScanCode.SC_NUMPAD8] = "Numpad 8",
		[ScanCode.SC_NUMPAD9] = "Numpad 9",
		[ScanCode.SC_DECIMAL] = "Decimal",
		[ScanCode.SC_DIVIDE] = "Divide",
		[ScanCode.SC_MULTIPLY] = "Multiply",
		[ScanCode.SC_SUBSTRACT] = "Substract",
		[ScanCode.SC_ADD] = "Add",
		[ScanCode.SC_NUMPADENTER] = "Numpad Enter",
		[ScanCode.SC_NUMPADEQUALS] = "Numpad Equals",
		[ScanCode.SC_LEFTSHIFT] = "Left Shift",
		[ScanCode.SC_LEFTCONTROL] = "Left Control",
		[ScanCode.SC_LEFTALT] = "Left Alt",
		[ScanCode.SC_LEFTGUI] = "Left Super",
		[ScanCode.SC_RIGHTSHIFT] = "Right Shift",
		[ScanCode.SC_RIGHTCONTROL] = "Right Control",
		[ScanCode.SC_RIGHTALT] = "Right Alt",
		[ScanCode.SC_RIGHTGUI] = "Right Super",
		[ScanCode.SC_APPS] = "Menu",
	}.ToFrozenDictionary();

	public string ConvertScanCodeToString(ScanCode key)
	{
		return _scanCodeSymStrMap[(byte)key] ?? _scanCodeStrMap.GetValueOrDefault(key);
	}
}
