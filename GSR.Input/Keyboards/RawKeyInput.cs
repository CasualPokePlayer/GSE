// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace GSR.Input.Keyboards;

[SupportedOSPlatform("windows6.0.6000")] // Windows Vista (technically Windows XP also works, but MapVirtualKey only works for extended scancodes starting in Windows Vista)
internal sealed class RawKeyInput : IKeyInput
{
	private readonly HWND RawInputWindow;
	private readonly List<KeyEvent> KeyEvents = [];

	private static readonly Lazy<ushort> _rawInputWindowAtom = new(() =>
	{
		unsafe
		{
			fixed (char* className = "RawKeyInputClass")
			{
				var wc = default(WNDCLASSW);
				wc.lpfnWndProc = &WndProc;
				wc.hInstance = PInvoke.GetModuleHandle(new PCWSTR(null));
				wc.lpszClassName = className;

				var atom = PInvoke.RegisterClass(&wc);
				if (atom == 0)
				{
					throw new Win32Exception("Failed to register RAWINPUT window class");
				}

				return atom;
			}
		}
	});

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
	private static unsafe LRESULT WndProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
	{
		if (uMsg != PInvoke.WM_INPUT)
		{
			return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
		}

		var ud =
#if GSR_64BIT
			PInvoke.GetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA);
#else
			PInvoke.GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_USERDATA);
#endif
		if (ud == 0)
		{
			return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
		}

		uint size;
		if (PInvoke.GetRawInputData(new(lParam.Value), RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, null,
			    &size, (uint)sizeof(RAWINPUTHEADER)) == unchecked((uint)-1))
		{
			return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
		}

		// don't think size should ever be this big, but just in case
		var buffer = size > 1024
			? new byte[size]
			: stackalloc byte[(int)size];

		fixed (byte* p = buffer)
		{
			var input = (RAWINPUT*)p;

			if (PInvoke.GetRawInputData(new(lParam.Value), RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, input,
					&size, (uint)sizeof(RAWINPUTHEADER)) == unchecked((uint)-1))
			{
				return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
			}

			if (input->header.dwType == (uint)RID_DEVICE_INFO_TYPE.RIM_TYPEKEYBOARD &&
				(input->data.keyboard.Flags & ~(PInvoke.RI_KEY_E0 | PInvoke.RI_KEY_BREAK)) == 0)
			{
				var scanCode = (ScanCode)(input->data.keyboard.MakeCode | ((input->data.keyboard.Flags & PInvoke.RI_KEY_E0) != 0 ? 0x80 : 0));

				// This is actually just the Pause key
				if (scanCode == ScanCode.SC_NUMLOCK && input->data.keyboard.VKey == 0xFF)
				{
					scanCode = ScanCode.SC_PAUSE;
				}

				var rawKeyInput = (RawKeyInput)GCHandle.FromIntPtr(ud).Target!;
				rawKeyInput.KeyEvents.Add(new(scanCode, (input->data.keyboard.Flags & PInvoke.RI_KEY_BREAK) == PInvoke.RI_KEY_MAKE));
			}

			return PInvoke.DefRawInputProc(&input, 0, (uint)sizeof(RAWINPUTHEADER));
		}
	}

	public RawKeyInput()
	{
		unsafe
		{
			fixed (char* windowName = "RawKeyInput")
			{
				RawInputWindow = PInvoke.CreateWindowEx(
					dwExStyle: 0,
					lpClassName: (char*)_rawInputWindowAtom.Value,
					lpWindowName: windowName,
					dwStyle: WINDOW_STYLE.WS_CHILD,
					X: 0,
					Y: 0,
					nWidth: 1,
					nHeight: 1,
					hWndParent: HWND.HWND_MESSAGE,
					hMenu: HMENU.Null,
					hInstance: PInvoke.GetModuleHandle(new PCWSTR(null)),
					lpParam: null);
			}

			if (RawInputWindow.IsNull)
			{
				throw new Win32Exception("Failed to create RAWINPUT window");
			}

			try
			{
				var rid = default(RAWINPUTDEVICE);
				rid.usUsagePage = PInvoke.HID_USAGE_PAGE_GENERIC;
				rid.usUsage = PInvoke.HID_USAGE_GENERIC_KEYBOARD;
				rid.dwFlags = RAWINPUTDEVICE_FLAGS.RIDEV_INPUTSINK;
				rid.hwndTarget = RawInputWindow;

				if (!PInvoke.RegisterRawInputDevices(&rid, 1, (uint)sizeof(RAWINPUTDEVICE)))
				{
					throw new Win32Exception("Failed to register RAWINPUTDEVICE");
				}

				var handle = GCHandle.Alloc(this, GCHandleType.Weak);
#if GSR_64BIT
				_ = PInvoke.SetWindowLongPtr(RawInputWindow, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA, GCHandle.ToIntPtr(handle));
#else
				_ = PInvoke.SetWindowLong(RawInputWindow, WINDOW_LONG_PTR_INDEX.GWL_USERDATA, (int)GCHandle.ToIntPtr(handle));
#endif
				if (Marshal.GetLastSystemError() != 0)
				{
					throw new Win32Exception("Failed to set window userdata");
				}
			}
			catch
			{
				_ = PInvoke.DestroyWindow(RawInputWindow);
				throw;
			}
		}
	}

	public void Dispose()
	{
		_ = PInvoke.DestroyWindow(RawInputWindow);
	}

	public IEnumerable<KeyEvent> GetEvents()
	{
		var ret = new KeyEvent[KeyEvents.Count];
		KeyEvents.CopyTo(ret.AsSpan());
		KeyEvents.Clear();
		return ret;
	}

	private static readonly FrozenDictionary<VIRTUAL_KEY, string> _vkStringMap = new Dictionary<VIRTUAL_KEY, string>
	{
		[VIRTUAL_KEY.VK_0] = "0",
		[VIRTUAL_KEY.VK_1] = "1",
		[VIRTUAL_KEY.VK_2] = "2",
		[VIRTUAL_KEY.VK_3] = "3",
		[VIRTUAL_KEY.VK_4] = "4",
		[VIRTUAL_KEY.VK_5] = "5",
		[VIRTUAL_KEY.VK_6] = "6",
		[VIRTUAL_KEY.VK_7] = "7",
		[VIRTUAL_KEY.VK_8] = "8",
		[VIRTUAL_KEY.VK_9] = "9",
		[VIRTUAL_KEY.VK_A] = "A",
		[VIRTUAL_KEY.VK_B] = "B",
		[VIRTUAL_KEY.VK_C] = "C",
		[VIRTUAL_KEY.VK_D] = "D",
		[VIRTUAL_KEY.VK_E] = "E",
		[VIRTUAL_KEY.VK_F] = "F",
		[VIRTUAL_KEY.VK_G] = "G",
		[VIRTUAL_KEY.VK_H] = "H",
		[VIRTUAL_KEY.VK_I] = "I",
		[VIRTUAL_KEY.VK_J] = "J",
		[VIRTUAL_KEY.VK_K] = "K",
		[VIRTUAL_KEY.VK_L] = "L",
		[VIRTUAL_KEY.VK_M] = "M",
		[VIRTUAL_KEY.VK_N] = "N",
		[VIRTUAL_KEY.VK_O] = "O",
		[VIRTUAL_KEY.VK_P] = "P",
		[VIRTUAL_KEY.VK_Q] = "Q",
		[VIRTUAL_KEY.VK_R] = "R",
		[VIRTUAL_KEY.VK_S] = "S",
		[VIRTUAL_KEY.VK_T] = "T",
		[VIRTUAL_KEY.VK_U] = "U",
		[VIRTUAL_KEY.VK_V] = "V",
		[VIRTUAL_KEY.VK_W] = "W",
		[VIRTUAL_KEY.VK_X] = "X",
		[VIRTUAL_KEY.VK_Y] = "Y",
		[VIRTUAL_KEY.VK_Z] = "Z",
		[VIRTUAL_KEY.VK_ABNT_C1] = "Abnt C1",
		[VIRTUAL_KEY.VK_ABNT_C2] = "Abnt C2",
		[VIRTUAL_KEY.VK_CANCEL] = "Cancel",
		[VIRTUAL_KEY.VK_BACK] = "Backspace",
		[VIRTUAL_KEY.VK_TAB] = "Tab",
		[VIRTUAL_KEY.VK_CLEAR] = "Clear",
		[VIRTUAL_KEY.VK_RETURN] = "Enter",
		[VIRTUAL_KEY.VK_PAUSE] = "Pause",
		[VIRTUAL_KEY.VK_CAPITAL] = "Caps Lock",
		[VIRTUAL_KEY.VK_KANA] = "Kana",
		[VIRTUAL_KEY.VK_IME_ON] = "Ime On",
		[VIRTUAL_KEY.VK_JUNJA] = "Junja",
		[VIRTUAL_KEY.VK_FINAL] = "Final",
		[VIRTUAL_KEY.VK_KANJI] = "Kanji",
		[VIRTUAL_KEY.VK_IME_OFF] = "Ime Off",
		[VIRTUAL_KEY.VK_ESCAPE] = "Escape",
		[VIRTUAL_KEY.VK_CONVERT] = "Ime Convert",
		[VIRTUAL_KEY.VK_NONCONVERT] = "Ime Nonconvert",
		[VIRTUAL_KEY.VK_ACCEPT] = "Ime Accept",
		[VIRTUAL_KEY.VK_MODECHANGE] = "Ime Mode Change",
		[VIRTUAL_KEY.VK_SPACE] = "Spacebar",
		[VIRTUAL_KEY.VK_PRIOR] = "Page Up",
		[VIRTUAL_KEY.VK_NEXT] = "Page Down",
		[VIRTUAL_KEY.VK_END] = "End",
		[VIRTUAL_KEY.VK_HOME] = "Home",
		[VIRTUAL_KEY.VK_LEFT] = "Left",
		[VIRTUAL_KEY.VK_UP] = "Up",
		[VIRTUAL_KEY.VK_RIGHT] = "Right",
		[VIRTUAL_KEY.VK_DOWN] = "Down",
		[VIRTUAL_KEY.VK_SELECT] = "Select",
		[VIRTUAL_KEY.VK_PRINT] = "Print",
		[VIRTUAL_KEY.VK_EXECUTE] = "Execute",
		[VIRTUAL_KEY.VK_SNAPSHOT] = "Print Screen",
		[VIRTUAL_KEY.VK_INSERT] = "Insert",
		[VIRTUAL_KEY.VK_DELETE] = "Delete",
		[VIRTUAL_KEY.VK_HELP] = "Help",
		[VIRTUAL_KEY.VK_LWIN] = "Left Windows",
		[VIRTUAL_KEY.VK_RWIN] = "Right Windows",
		[VIRTUAL_KEY.VK_APPS] = "Applications",
		[VIRTUAL_KEY.VK_SLEEP] = "Sleep",
		[VIRTUAL_KEY.VK_NUMPAD0] = "Numpad 0",
		[VIRTUAL_KEY.VK_NUMPAD1] = "Numpad 1",
		[VIRTUAL_KEY.VK_NUMPAD2] = "Numpad 2",
		[VIRTUAL_KEY.VK_NUMPAD3] = "Numpad 3",
		[VIRTUAL_KEY.VK_NUMPAD4] = "Numpad 4",
		[VIRTUAL_KEY.VK_NUMPAD5] = "Numpad 5",
		[VIRTUAL_KEY.VK_NUMPAD6] = "Numpad 6",
		[VIRTUAL_KEY.VK_NUMPAD7] = "Numpad 7",
		[VIRTUAL_KEY.VK_NUMPAD8] = "Numpad 8",
		[VIRTUAL_KEY.VK_NUMPAD9] = "Numpad 9",
		[VIRTUAL_KEY.VK_MULTIPLY] = "Multiply",
		[VIRTUAL_KEY.VK_ADD] = "Add",
		[VIRTUAL_KEY.VK_SEPARATOR] = "Separator",
		[VIRTUAL_KEY.VK_SUBTRACT] = "Substract",
		[VIRTUAL_KEY.VK_DECIMAL] = "Decimal",
		[VIRTUAL_KEY.VK_DIVIDE] = "Divide",
		[VIRTUAL_KEY.VK_F1] = "F1",
		[VIRTUAL_KEY.VK_F2] = "F2",
		[VIRTUAL_KEY.VK_F3] = "F3",
		[VIRTUAL_KEY.VK_F4] = "F4",
		[VIRTUAL_KEY.VK_F5] = "F5",
		[VIRTUAL_KEY.VK_F6] = "F6",
		[VIRTUAL_KEY.VK_F7] = "F7",
		[VIRTUAL_KEY.VK_F8] = "F8",
		[VIRTUAL_KEY.VK_F9] = "F9",
		[VIRTUAL_KEY.VK_F10] = "F10",
		[VIRTUAL_KEY.VK_F11] = "F11",
		[VIRTUAL_KEY.VK_F12] = "F12",
		[VIRTUAL_KEY.VK_F13] = "F13",
		[VIRTUAL_KEY.VK_F14] = "F14",
		[VIRTUAL_KEY.VK_F15] = "F15",
		[VIRTUAL_KEY.VK_F16] = "F16",
		[VIRTUAL_KEY.VK_F17] = "F17",
		[VIRTUAL_KEY.VK_F18] = "F18",
		[VIRTUAL_KEY.VK_F19] = "F19",
		[VIRTUAL_KEY.VK_F20] = "F20",
		[VIRTUAL_KEY.VK_F21] = "F21",
		[VIRTUAL_KEY.VK_F22] = "F22",
		[VIRTUAL_KEY.VK_F23] = "F23",
		[VIRTUAL_KEY.VK_F24] = "F24",
		[VIRTUAL_KEY.VK_NUMLOCK] = "NumLock",
		[VIRTUAL_KEY.VK_SCROLL] = "ScrollLock",
		[VIRTUAL_KEY.VK_OEM_FJ_JISHO] = "Jisho",
		[VIRTUAL_KEY.VK_OEM_FJ_MASSHOU] = "Mashu",
		[VIRTUAL_KEY.VK_OEM_FJ_TOUROKU] = "Touroku",
		[VIRTUAL_KEY.VK_OEM_FJ_LOYA] = "Loya",
		[VIRTUAL_KEY.VK_OEM_FJ_ROYA] = "Roya",
		[VIRTUAL_KEY.VK_LSHIFT] = "Left Shift",
		[VIRTUAL_KEY.VK_RSHIFT] = "Right Shift",
		[VIRTUAL_KEY.VK_LCONTROL] = "Left Control",
		[VIRTUAL_KEY.VK_RCONTROL] = "Right Control",
		[VIRTUAL_KEY.VK_LMENU] = "Left Alt",
		[VIRTUAL_KEY.VK_RMENU] = "Right Alt",
		[VIRTUAL_KEY.VK_BROWSER_BACK] = "Back",
		[VIRTUAL_KEY.VK_BROWSER_FORWARD] = "Forward",
		[VIRTUAL_KEY.VK_BROWSER_REFRESH] = "Refresh",
		[VIRTUAL_KEY.VK_BROWSER_STOP] = "Browser Stop",
		[VIRTUAL_KEY.VK_BROWSER_SEARCH] = "Search",
		[VIRTUAL_KEY.VK_BROWSER_FAVORITES] = "Favorites",
		[VIRTUAL_KEY.VK_BROWSER_HOME] = "Browser Home",
		[VIRTUAL_KEY.VK_VOLUME_MUTE] = "Mute",
		[VIRTUAL_KEY.VK_VOLUME_DOWN] = "Volume Down",
		[VIRTUAL_KEY.VK_VOLUME_UP] = "Volume Up",
		[VIRTUAL_KEY.VK_MEDIA_NEXT_TRACK] = "Next Track",
		[VIRTUAL_KEY.VK_MEDIA_PREV_TRACK] = "Prev Track",
		[VIRTUAL_KEY.VK_MEDIA_STOP] = "Media Stop",
		[VIRTUAL_KEY.VK_MEDIA_PLAY_PAUSE] = "Play/Pause",
		[VIRTUAL_KEY.VK_LAUNCH_MAIL] = "Mail",
		[VIRTUAL_KEY.VK_LAUNCH_MEDIA_SELECT] = "Media",
		[VIRTUAL_KEY.VK_LAUNCH_APP1] = "App1",
		[VIRTUAL_KEY.VK_LAUNCH_APP2] = "App2",
		[VIRTUAL_KEY.VK_OEM_1] = "Semicolon",
		[VIRTUAL_KEY.VK_OEM_PLUS] = "Equals",
		[VIRTUAL_KEY.VK_OEM_COMMA] = "Comma",
		[VIRTUAL_KEY.VK_OEM_MINUS] = "Minus",
		[VIRTUAL_KEY.VK_OEM_PERIOD] = "Period",
		[VIRTUAL_KEY.VK_OEM_2] = "Question",
		[VIRTUAL_KEY.VK_OEM_3] = "Tilde",
		[VIRTUAL_KEY.VK_OEM_4] = "Left Bracket",
		[VIRTUAL_KEY.VK_OEM_5] = "Pipe",
		[VIRTUAL_KEY.VK_OEM_6] = "Right Bracket",
		[VIRTUAL_KEY.VK_OEM_7] = "Quotes",
		[VIRTUAL_KEY.VK_OEM_8] = "Section",
		[VIRTUAL_KEY.VK_OEM_AX] = "Ax",
		[VIRTUAL_KEY.VK_OEM_102] = "Oem 102",
		[VIRTUAL_KEY.VK_ICO_HELP] = "Ico Help",
		[VIRTUAL_KEY.VK_ICO_00] = "Ico 00",
		[VIRTUAL_KEY.VK_PROCESSKEY] = "Process",
		[VIRTUAL_KEY.VK_ICO_CLEAR] = "Ico Clear",
		[VIRTUAL_KEY.VK_PACKET] = "Packet",
		[VIRTUAL_KEY.VK_OEM_RESET] = "Reset",
		[VIRTUAL_KEY.VK_OEM_JUMP] = "Jump",
		[VIRTUAL_KEY.VK_OEM_PA1] = "Oem Pa1",
		[VIRTUAL_KEY.VK_OEM_PA2] = "Pa2",
		[VIRTUAL_KEY.VK_OEM_PA3] = "Pa3",
		[VIRTUAL_KEY.VK_OEM_WSCTRL] = "Wsctrl",
		[VIRTUAL_KEY.VK_OEM_CUSEL] = "Cu Sel",
		[VIRTUAL_KEY.VK_OEM_ATTN] = "Oem Attn",
		[VIRTUAL_KEY.VK_OEM_FINISH] = "Finish",
		[VIRTUAL_KEY.VK_OEM_COPY] = "Copy",
		[VIRTUAL_KEY.VK_OEM_AUTO] = "Auto",
		[VIRTUAL_KEY.VK_OEM_ENLW] = "Enlw",
		[VIRTUAL_KEY.VK_OEM_BACKTAB] = "Back Tab",
		[VIRTUAL_KEY.VK_ATTN] = "Attn",
		[VIRTUAL_KEY.VK_CRSEL] = "Cr Sel",
		[VIRTUAL_KEY.VK_EXSEL] = "Ex Sel",
		[VIRTUAL_KEY.VK_EREOF] = "Er Eof",
		[VIRTUAL_KEY.VK_PLAY] = "Play",
		[VIRTUAL_KEY.VK_ZOOM] = "Zoom",
		[VIRTUAL_KEY.VK_NONAME] = "No Name",
		[VIRTUAL_KEY.VK_PA1] = "Pa1",
		[VIRTUAL_KEY.VK_OEM_CLEAR] = "Oem Clear",
	}.ToFrozenDictionary();

	public string ConvertScanCodeToString(ScanCode key)
	{
		// this apparently just shares its VKey with normal Enter
		if (key == ScanCode.SC_NUMPADENTER)
		{
			return "Numpad Enter";
		}

		var scanCode = (uint)key;
		if ((scanCode & 0x80) != 0)
		{
			if (key == ScanCode.SC_PAUSE)
			{
				scanCode = 0xE11D;
			}
			else
			{
				scanCode &= 0x7F;
				scanCode |= 0xE000;
			}
		}

		var virtualKey = (VIRTUAL_KEY)PInvoke.MapVirtualKey(scanCode, MAP_VIRTUAL_KEY_TYPE.MAPVK_VSC_TO_VK_EX);
		return _vkStringMap.GetValueOrDefault(virtualKey);
	}
}
