// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

namespace GSR.Input.Keyboards;

/// <summary>
/// This enum generally assumes a QWERTY layout (and goes off PS/2 Set 1 scancodes)
/// Bit7 will indicate that the input has an E0 prefix
/// (This also somewhat mimics DirectInput's DIK_ enum)
/// TODO: There are several missing members here (although that shouldn't really matter in practice)
/// </summary>
public enum ScanCode : byte
{
	// 0x00 not set
	SC_ESCAPE = 0x01,
	SC_1 = 0x02,
	SC_2 = 0x03,
	SC_3 = 0x04,
	SC_4 = 0x05,
	SC_5 = 0x06,
	SC_6 = 0x07,
	SC_7 = 0x08,
	SC_8 = 0x09,
	SC_9 = 0x0A,
	SC_0 = 0x0B,
	SC_MINUS = 0x0C,
	SC_EQUALS = 0x0D,
	SC_BACKSPACE = 0x0E,
	SC_TAB = 0x0F,
	SC_Q = 0x10,
	SC_W = 0x11,
	SC_E = 0x12,
	SC_R = 0x13,
	SC_T = 0x14,
	SC_Y = 0x15,
	SC_U = 0x16,
	SC_I = 0x17,
	SC_O = 0x18,
	SC_P = 0x19,
	SC_LEFTBRACKET = 0x1A,
	SC_RIGHTBRACKET = 0x1B,
	SC_ENTER = 0x1C,
	SC_LEFTCONTROL = 0x1D,
	SC_A = 0x1E,
	SC_S = 0x1F,
	SC_D = 0x20,
	SC_F = 0x21,
	SC_G = 0x22,
	SC_H = 0x23,
	SC_J = 0x24,
	SC_K = 0x25,
	SC_L = 0x26,
	SC_SEMICOLON = 0x27,
	SC_APOSTROPHE = 0x28,
	SC_GRAVE = 0x29,
	SC_LEFTSHIFT = 0x2A,
	SC_BACKSLASH = 0x2B, // also SC_EUROPE1
	SC_Z = 0x2C,
	SC_X = 0x2D,
	SC_C = 0x2E,
	SC_V = 0x2F,
	SC_B = 0x30,
	SC_N = 0x31,
	SC_M = 0x32,
	SC_COMMA = 0x33,
	SC_PERIOD = 0x34,
	SC_SLASH = 0x35,
	SC_RIGHTSHIFT = 0x36,
	SC_MULTIPLY = 0x37,
	SC_LEFTALT = 0x38,
	SC_SPACEBAR = 0x39,
	SC_CAPSLOCK = 0x3A,
	SC_F1 = 0x3B,
	SC_F2 = 0x3C,
	SC_F3 = 0x3D,
	SC_F4 = 0x3E,
	SC_F5 = 0x3F,
	SC_F6 = 0x40,
	SC_F7 = 0x41,
	SC_F8 = 0x42,
	SC_F9 = 0x43,
	SC_F10 = 0x44,
	SC_NUMLOCK = 0x45,
	SC_SCROLLLOCK = 0x46,
	SC_NUMPAD7 = 0x47,
	SC_NUMPAD8 = 0x48,
	SC_NUMPAD9 = 0x49,
	SC_SUBSTRACT = 0x4A,
	SC_NUMPAD4 = 0x4B,
	SC_NUMPAD5 = 0x4C,
	SC_NUMPAD6 = 0x4D,
	SC_ADD = 0x4E,
	SC_NUMPAD1 = 0x4F,
	SC_NUMPAD2 = 0x50,
	SC_NUMPAD3 = 0x51,
	SC_NUMPAD0 = 0x52,
	SC_DECIMAL = 0x53,
	// 0x54-0x55 not set 
	SC_EUROPE2 = 0x56,
	SC_F11 = 0x57,
	SC_F12 = 0x58,
	SC_NUMPADEQUALS = 0x59,
	// 0x5A-0x5B not set
	SC_INTL6 = 0x5C,
	// 0x5D-0x63 not set
	SC_F13 = 0x64,
	SC_F14 = 0x65,
	SC_F15 = 0x66,
	SC_F16 = 0x67,
	SC_F17 = 0x68,
	SC_F18 = 0x69,
	SC_F19 = 0x6A,
	SC_F20 = 0x6B,
	SC_F21 = 0x6C,
	SC_F22 = 0x6D,
	SC_F23 = 0x6E,
	// 0x6F not set
	SC_INTL2 = 0x70,
	// 0x71-0x72 not set
	SC_INTL1 = 0x73,
	// 0x74-0x75 not set
	SC_F24 = 0x76, // also SC_LANG5
	SC_LANG4 = 0x77,
	SC_LANG3 = 0x78,
	SC_INTL4 = 0x79,
	// 0x7A not set
	SC_INTL5 = 0x7B,
	// 0x7C not set
	SC_INTL3 = 0x7D,
	SC_SEPARATOR = 0x7E,
	// 0x7F-0x8F not set
	SC_PREVTRACK = 0x90,
	// 0x91-0x96 not set
	SC_NEXTTRACK = 0x97,
	// 0x98-0x9B not set
	SC_NUMPADENTER = 0x9C,
	SC_RIGHTCONTROL = 0x9D,
	// 0x9E-0x9F not set
	SC_MUTE = 0xA0,
	SC_CALCULATOR = 0xA1,
	SC_PLAYPAUSE = 0xA2,
	// 0xA3 not set
	SC_STOP = 0xA4,
	// 0xA5-0xAD not set
	SC_VOLUMEDOWN = 0xAE,
	// 0xAF not set
	SC_VOLUMEUP = 0xB0,
	// 0xB1 not set
	SC_BROWSERHOME = 0xB2,
	// 0xB3-0xB4 not set
	SC_DIVIDE = 0xB5,
	// 0xB6 not set
	SC_PRINTSCREEN = 0xB7,
	SC_RIGHTALT = 0xB8,
	// 0xB9-0xC4 not set
	// Pause key is special, this actually emits 0xE11D45 / 0xE19DC5
	// special handling is possibly needed...
	SC_PAUSE = 0xC5,
	// 0xC6 not set
	SC_HOME = 0xC7,
	SC_UP = 0xC8,
	SC_PAGEUP = 0xC9,
	// 0xCA not set
	SC_LEFT = 0xCB,
	// 0xCC not set
	SC_RIGHT = 0xCD,
	// 0xCE not set
	SC_END = 0xCF,
	SC_DOWN = 0xD0,
	SC_PAGEDOWN = 0xD1,
	SC_INSERT = 0xD2,
	SC_DELETE = 0xD3,
	// 0xD4-0xDA not set
	SC_LEFTGUI = 0xDB,
	SC_RIGHTGUI = 0xDC,
	SC_APPS = 0xDD,
	SC_POWER = 0xDE,
	SC_SLEEP = 0xDF,
	// 0xE0-0xE2 not set
	SC_WAKE = 0xE3,
	// 0xE4 not set
	SC_BROWSERSEARCH = 0xE5,
	SC_BROWSERFAVORITES = 0xE6,
	SC_BROWSERREFRESH = 0xE7,
	SC_BROWSERSTOP = 0xE8,
	SC_BROWSERFORWARD = 0xE9,
	SC_BROWSERBACK = 0xEA,
	SC_MYCOMPUTER = 0xEB,
	SC_MAIL = 0xEC,
	SC_MEDIASELECT = 0xED,
	// 0xEE-0xFF not set
}
