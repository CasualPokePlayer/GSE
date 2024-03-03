using System;
using System.Runtime.InteropServices;

namespace GSR.Input.Keyboards;

internal static class EvDevImports
{
	public enum EvDevEventType : ushort
	{
		EV_SYN = 0x00,
		EV_KEY = 0x01,
		EV_REL = 0x02,
		EV_ABS = 0x03,
		EV_MSC = 0x04,
		EV_SW = 0x05,
		EV_LED = 0x11,
		EV_SND = 0x12,
		EV_REP = 0x14,
		EV_FF = 0x15,
		EV_PWR = 0x16,
		EV_FF_STATUS = 0x17,
		EV_MAX = 0x1f,
		EV_CNT = EV_MAX + 1,
	}

	public enum EvDevKeyCode : ushort
	{
		KEY_ESC = 0x01,
		KEY_1 = 0x02,
		KEY_2 = 0x03,
		KEY_3 = 0x04,
		KEY_4 = 0x05,
		KEY_5 = 0x06,
		KEY_6 = 0x07,
		KEY_7 = 0x08,
		KEY_8 = 0x09,
		KEY_9 = 0x0A,
		KEY_0 = 0x0B,
		KEY_MINUS = 0x0C,
		KEY_EQUAL = 0x0D,
		KEY_BACKSPACE = 0x0E,
		KEY_TAB = 0x0F,
		KEY_Q = 0x10,
		KEY_W = 0x11,
		KEY_E = 0x12,
		KEY_R = 0x13,
		KEY_T = 0x14,
		KEY_Y = 0x15,
		KEY_U = 0x16,
		KEY_I = 0x17,
		KEY_O = 0x18,
		KEY_P = 0x19,
		KEY_LEFTBRACE = 0x1A,
		KEY_RIGHTBRACE = 0x1B,
		KEY_ENTER = 0x1C,
		KEY_LEFTCTRL = 0x1D,
		KEY_A = 0x1E,
		KEY_S = 0x1F,
		KEY_D = 0x20,
		KEY_F = 0x21,
		KEY_G = 0x22,
		KEY_H = 0x23,
		KEY_J = 0x24,
		KEY_K = 0x25,
		KEY_L = 0x26,
		KEY_SEMICOLON = 0x27,
		KEY_APOSTROPHE = 0x28,
		KEY_GRAVE = 0x29,
		KEY_LEFTSHIFT = 0x2A,
		KEY_BACKSLASH = 0x2B,
		KEY_Z = 0x2C,
		KEY_X = 0x2D,
		KEY_C = 0x2E,
		KEY_V = 0x2F,
		KEY_B = 0x30,
		KEY_N = 0x31,
		KEY_M = 0x32,
		KEY_COMMA = 0x33,
		KEY_DOT = 0x34,
		KEY_SLASH = 0x35,
		KEY_RIGHTSHIFT = 0x36,
		KEY_KPASTERISK = 0x37,
		KEY_LEFTALT = 0x38,
		KEY_SPACE = 0x39,
		KEY_CAPSLOCK = 0x3A,
		KEY_F1 = 0x3B,
		KEY_F2 = 0x3C,
		KEY_F3 = 0x3D,
		KEY_F4 = 0x3E,
		KEY_F5 = 0x3F,
		KEY_F6 = 0x40,
		KEY_F7 = 0x41,
		KEY_F8 = 0x42,
		KEY_F9 = 0x43,
		KEY_F10 = 0x44,
		KEY_NUMLOCK = 0x45,
		KEY_SCROLLLOCK = 0x46,
		KEY_KP7 = 0x47,
		KEY_KP8 = 0x48,
		KEY_KP9 = 0x49,
		KEY_KPMINUS = 0x4A,
		KEY_KP4 = 0x4B,
		KEY_KP5 = 0x4C,
		KEY_KP6 = 0x4D,
		KEY_KPPLUS = 0x4E,
		KEY_KP1 = 0x4F,
		KEY_KP2 = 0x50,
		KEY_KP3 = 0x51,
		KEY_KP0 = 0x52,
		KEY_KPDOT = 0x53,
		KEY_ZENKAKUHANKAKU = 0x55,
		KEY_102ND = 0x56,
		KEY_F11 = 0x57,
		KEY_F12 = 0x58,
		KEY_RO = 0x59,
		KEY_KATAKANA = 0x5A,
		KEY_HIRAGANA = 0x5B,
		KEY_HENKAN = 0x5C,
		KEY_KATAKANAHIRAGANA = 0x5D,
		KEY_MUHENKAN = 0x5E,
		KEY_KPJPCOMMA = 0x5F,
		KEY_KPENTER = 0x60,
		KEY_RIGHTCTRL = 0x61,
		KEY_KPSLASH = 0x62,
		KEY_SYSRQ = 0x63,
		KEY_RIGHTALT = 0x64,
		KEY_LINEFEED = 0x65,
		KEY_HOME = 0x66,
		KEY_UP = 0x67,
		KEY_PAGEUP = 0x68,
		KEY_LEFT = 0x69,
		KEY_RIGHT = 0x6A,
		KEY_END = 0x6B,
		KEY_DOWN = 0x6C,
		KEY_PAGEDOWN = 0x6D,
		KEY_INSERT = 0x6E,
		KEY_DELETE = 0x6F,
		KEY_MACRO = 0x70,
		KEY_MUTE = 0x71,
		KEY_VOLUMEDOWN = 0x72,
		KEY_VOLUMEUP = 0x73,
		KEY_POWER = 0x74,
		KEY_KPEQUAL = 0x75,
		KEY_KPPLUSMINUS = 0x76,
		KEY_PAUSE = 0x77,
		KEY_SCALE = 0x78,
		KEY_KPCOMMA = 0x79,
		KEY_HANGEUL = 0x7A,
		KEY_HANJA = 0x7B,
		KEY_YEN = 0x7C,
		KEY_LEFTMETA = 0x7D,
		KEY_RIGHTMETA = 0x7E,
		KEY_COMPOSE = 0x7F,
		KEY_STOP = 0x80,
		KEY_AGAIN = 0x81,
		KEY_PROPS = 0x82,
		KEY_UNDO = 0x83,
		KEY_FRONT = 0x84,
		KEY_COPY = 0x85,
		KEY_OPEN = 0x86,
		KEY_PASTE = 0x87,
		KEY_FIND = 0x88,
		KEY_CUT = 0x89,
		KEY_HELP = 0x8A,
		KEY_MENU = 0x8B,
		KEY_CALC = 0x8C,
		KEY_SETUP = 0x8D,
		KEY_SLEEP = 0x8E,
		KEY_WAKEUP = 0x8F,
		KEY_FILE = 0x90,
		KEY_SENDFILE = 0x91,
		KEY_DELETEFILE = 0x92,
		KEY_XFER = 0x93,
		KEY_PROG1 = 0x94,
		KEY_PROG2 = 0x95,
		KEY_WWW = 0x96,
		KEY_MSDOS = 0x97,
		KEY_SCREENLOCK = 0x98,
		KEY_DIRECTION = 0x99,
		KEY_CYCLEWINDOWS = 0x9A,
		KEY_MAIL = 0x9B,
		KEY_BOOKMARKS = 0x9C,
		KEY_COMPUTER = 0x9D,
		KEY_BACK = 0x9E,
		KEY_FORWARD = 0x9F,
		KEY_CLOSECD = 0xA0,
		KEY_EJECTCD = 0xA1,
		KEY_EJECTCLOSECD = 0xA2,
		KEY_NEXTSONG = 0xA3,
		KEY_PLAYPAUSE = 0xA4,
		KEY_PREVIOUSSONG = 0xA5,
		KEY_STOPCD = 0xA6,
		KEY_RECORD = 0xA7,
		KEY_REWIND = 0xA8,
		KEY_PHONE = 0xA9,
		KEY_ISO = 0xAA,
		KEY_CONFIG = 0xAB,
		KEY_HOMEPAGE = 0xAC,
		KEY_REFRESH = 0xAD,
		KEY_EXIT = 0xAE,
		KEY_MOVE = 0xAF,
		KEY_EDIT = 0xB0,
		KEY_SCROLLUP = 0xB1,
		KEY_SCROLLDOWN = 0xB2,
		KEY_KPLEFTPAREN = 0xB3,
		KEY_KPRIGHTPAREN = 0xB4,
		KEY_NEW = 0xB5,
		KEY_REDO = 0xB6,
		KEY_F13 = 0xB7,
		KEY_F14 = 0xB8,
		KEY_F15 = 0xB9,
		KEY_F16 = 0xBA,
		KEY_F17 = 0xBB,
		KEY_F18 = 0xBC,
		KEY_F19 = 0xBD,
		KEY_F20 = 0xBE,
		KEY_F21 = 0xBF,
		KEY_F22 = 0xC0,
		KEY_F23 = 0xC1,
		KEY_F24 = 0xC2,
		KEY_PLAYCD = 0xC8,
		KEY_PAUSECD = 0xC9,
		KEY_PROG3 = 0xCA,
		KEY_PROG4 = 0xCB,
		KEY_DASHBOARD = 0xCC,
		KEY_SUSPEND = 0xCD,
		KEY_CLOSE = 0xCE,
		KEY_PLAY = 0xCF,
		KEY_FASTFORWARD = 0xD0,
		KEY_BASSBOOST = 0xD1,
		KEY_PRINT = 0xD2,
		KEY_HP = 0xD3,
		KEY_CAMERA = 0xD4,
		KEY_SOUND = 0xD5,
		KEY_QUESTION = 0xD6,
		KEY_EMAIL = 0xD7,
		KEY_CHAT = 0xD8,
		KEY_SEARCH = 0xD9,
		KEY_CONNECT = 0xDA,
		KEY_FINANCE = 0xDB,
		KEY_SPORT = 0xDC,
		KEY_SHOP = 0xDD,
		KEY_ALTERASE = 0xDE,
		KEY_CANCEL = 0xDF,
		KEY_BRIGHTNESSDOWN = 0xE0,
		KEY_BRIGHTNESSUP = 0xE1,
		KEY_MEDIA = 0xE2,
		KEY_SWITCHVIDEOMODE = 0xE3,
		KEY_KBDILLUMTOGGLE = 0xE4,
		KEY_KBDILLUMDOWN = 0xE5,
		KEY_KBDILLUMUP = 0xE6,
		KEY_SEND = 0xE7,
		KEY_REPLY = 0xE8,
		KEY_FORWARDMAIL = 0xE9,
		KEY_SAVE = 0xEA,
		KEY_DOCUMENTS = 0xEB,
		KEY_BATTERY = 0xEC,
		KEY_BLUETOOTH = 0xED,
		KEY_WLAN = 0xEE,
		KEY_UWB = 0xEF,
		KEY_UNKNOWN = 0xF0,
		KEY_VIDEO_NEXT = 0xF1,
		KEY_VIDEO_PREV = 0xF2,
		KEY_BRIGHTNESS_CYCLE = 0xF3,
		KEY_BRIGHTNESS_ZERO = 0xF4,
		KEY_DISPLAY_OFF = 0xF5,
		KEY_WWAN = 0xF6,
		KEY_RFKILL = 0xF7,
		KEY_MICMUTE = 0xF8,
		KEY_MAX = 0x2FF,
	}

	public enum EvDevKeyValue : int
	{
		KeyUp = 0,
		KeyDown = 1,
		KeyRepeat = 2
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct EvDevKeyboardEvent
	{
		public IntPtr tv_sec;
		public IntPtr tv_usec;
		public EvDevEventType type;
		public EvDevKeyCode code;
		public EvDevKeyValue value;
	}

	private const uint IOC_READ = 2;
	private const uint IOC_EV_TYPE = 'E';

	private static uint EVIOCG(long nr, long size)
		=> (uint)((IOC_READ << 30) | (size << 16) | (IOC_EV_TYPE << 8) | nr);

	public static uint EVIOCGNAME(long len) => EVIOCG(0x06, len);
	public static uint EVIOCGBIT(EvDevEventType ev, long len) => EVIOCG(0x20 + (long)ev, len);

	public const uint EVIOCGVERSION = 0x80044501;
	public const uint EVIOCGID = 0x80084502;
}
