using System;
using System.Collections.Generic;

namespace GSR.Input.Keyboards;

internal readonly record struct KeyEvent(ScanCode Key, bool IsPressed);

internal interface IKeyInput : IDisposable
{
	IEnumerable<KeyEvent> GetEvents();
	string ConvertScanCodeToString(ScanCode key);
}

internal static class KeyInputFactory
{
	public static IKeyInput CreateKeyInput()
	{
		if (OperatingSystem.IsWindowsVersionAtLeast(5, 1, 2600))
		{
			return new RawKeyInput();
		}

		if (OperatingSystem.IsLinux())
		{
			//return new X11KeyInput();
		}

		if (OperatingSystem.IsMacOS())
		{
			//return new QuartzKeyInput();
		}

		throw new NotSupportedException("Key input is not supported on this platform");
	}
}

/// <summary>
/// This enum generally assumes a QWERTY layout
/// Bit7 will indicate that the input has an E0 prefix
/// (This also just mimics DirectInput's DIK_ enum)
/// </summary>
public enum ScanCode : byte
{
	SC_ESCAPE = 1,
	SC_1,
	SC_2,
	SC_3,
	SC_4,
	SC_5,
	SC_6,
	SC_7,
	SC_8,
	SC_9,
	SC_0,
	SC_MINUS,
	SC_EQUALS,
	SC_BACK,
	SC_TAB,
	SC_Q,
	SC_W,
	SC_E,
	SC_R,
	SC_T,
	SC_Y,
	SC_U,
	SC_I,
	SC_O,
	SC_P,
	SC_LBRACKET,
	SC_RBRACKET,
	SC_RETURN,
	SC_LCONTROL,
	SC_A,
	SC_S,
	SC_D,
	SC_F,
	SC_G,
	SC_H,
	SC_J,
	SC_K,
	SC_L,
	SC_SEMICOLON,
	SC_APOSTROPHE,
	SC_GRAVE,
	SC_LSHIFT,
	SC_BACKSLASH,
	SC_Z,
	SC_X,
	SC_C,
	SC_V,
	SC_B,
	SC_N,
	SC_M,
	SC_COMMA,
	SC_PERIOD,
	SC_SLASH,
	SC_RSHIFT,
	SC_MULTIPLY,
	SC_LMENU,
	SC_SPACE,
	SC_CAPITAL,
	SC_F1,
	SC_F2,
	SC_F3,
	SC_F4,
	SC_F5,
	SC_F6,
	SC_F7,
	SC_F8,
	SC_F9,
	SC_F10,
	SC_NUMLOCK,
	SC_SCROLL,
	SC_NUMPAD7,
	SC_NUMPAD8,
	SC_NUMPAD9,
	SC_SUBSTRACT,
	SC_NUMPAD4,
	SC_NUMPAD5,
	SC_NUMPAD6,
	SC_ADD,
	SC_NUMPAD1,
	SC_NUMPAD2,
	SC_NUMPAD3,
	SC_NUMPAD0,
	SC_DECIMAL,
	SC_F11 = 0x57,
	SC_F12,
	SC_F13 = 0x64,
	SC_F14,
	SC_F15,
	SC_KANA = 0x70,
	SC_CONVERT = 0x79,
	SC_NOCONVERT = 0x7B,
	SC_YEN = 0x7D,
	SC_NUMPADEQUALS = 0x8D,
	SC_CIRCUMFLEX = 0x90,
	SC_AT,
	SC_COLON,
	SC_UNDERLINE,
	SC_KANJI,
	SC_STOP,
	SC_AX,
	SC_UNLABELED,
	SC_NUMPADENTER = 0x9C,
	SC_RCTRL,
	SC_NUMPADCOMMA = 0xB3,
	SC_DIVIDE = 0xB5,
	SC_SYSRQ = 0xB7,
	SC_RMENU,
	SC_PAUSE = 0xC5, // note that this actually has an E1 prefix, special handling possibly needed...
	SC_HOME = 0xC7,
	SC_UP,
	SC_PRIOR,
	SC_LEFT = 0xCB,
	SC_RIGHT = 0xCD,
	SC_END = 0xCF,
	SC_DOWN,
	SC_NEXT,
	SC_INSERT,
	SC_DELETE,
	SC_LWIN = 0xDB,
	SC_RWIN,
	SC_APPS,
	SC_POWER,
	SC_SLEEP,
}
