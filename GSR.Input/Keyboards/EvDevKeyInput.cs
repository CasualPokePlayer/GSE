// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using static GSR.Input.Keyboards.EvDevImports;
using static GSR.Input.Keyboards.LibcImports;

namespace GSR.Input.Keyboards;

internal class EvDevKeyInput : IKeyInput
{
	/// <summary>
	/// These map keycodes to scancodes
	/// </summary>
	protected static readonly FrozenDictionary<EvDevKeyCode, ScanCode> EvDevKeyCodeMap = new Dictionary<EvDevKeyCode, ScanCode>
	{
		[EvDevKeyCode.KEY_ESC] = ScanCode.SC_ESCAPE,
		[EvDevKeyCode.KEY_1] = ScanCode.SC_1,
		[EvDevKeyCode.KEY_2] = ScanCode.SC_2,
		[EvDevKeyCode.KEY_3] = ScanCode.SC_3,
		[EvDevKeyCode.KEY_4] = ScanCode.SC_4,
		[EvDevKeyCode.KEY_5] = ScanCode.SC_5,
		[EvDevKeyCode.KEY_6] = ScanCode.SC_6,
		[EvDevKeyCode.KEY_7] = ScanCode.SC_7,
		[EvDevKeyCode.KEY_8] = ScanCode.SC_8,
		[EvDevKeyCode.KEY_9] = ScanCode.SC_9,
		[EvDevKeyCode.KEY_0] = ScanCode.SC_0,
		[EvDevKeyCode.KEY_MINUS] = ScanCode.SC_MINUS,
		[EvDevKeyCode.KEY_EQUAL] = ScanCode.SC_EQUALS,
		[EvDevKeyCode.KEY_BACKSPACE] = ScanCode.SC_BACKSPACE,
		[EvDevKeyCode.KEY_TAB] = ScanCode.SC_TAB,
		[EvDevKeyCode.KEY_Q] = ScanCode.SC_Q,
		[EvDevKeyCode.KEY_W] = ScanCode.SC_W,
		[EvDevKeyCode.KEY_E] = ScanCode.SC_E,
		[EvDevKeyCode.KEY_R] = ScanCode.SC_R,
		[EvDevKeyCode.KEY_T] = ScanCode.SC_T,
		[EvDevKeyCode.KEY_Y] = ScanCode.SC_Y,
		[EvDevKeyCode.KEY_U] = ScanCode.SC_U,
		[EvDevKeyCode.KEY_I] = ScanCode.SC_I,
		[EvDevKeyCode.KEY_O] = ScanCode.SC_O,
		[EvDevKeyCode.KEY_P] = ScanCode.SC_P,
		[EvDevKeyCode.KEY_LEFTBRACE] = ScanCode.SC_LEFTBRACKET,
		[EvDevKeyCode.KEY_RIGHTBRACE] = ScanCode.SC_RIGHTBRACKET,
		[EvDevKeyCode.KEY_ENTER] = ScanCode.SC_ENTER,
		[EvDevKeyCode.KEY_LEFTCTRL] = ScanCode.SC_LEFTCONTROL,
		[EvDevKeyCode.KEY_A] = ScanCode.SC_A,
		[EvDevKeyCode.KEY_S] = ScanCode.SC_S,
		[EvDevKeyCode.KEY_D] = ScanCode.SC_D,
		[EvDevKeyCode.KEY_F] = ScanCode.SC_F,
		[EvDevKeyCode.KEY_G] = ScanCode.SC_G,
		[EvDevKeyCode.KEY_H] = ScanCode.SC_H,
		[EvDevKeyCode.KEY_J] = ScanCode.SC_J,
		[EvDevKeyCode.KEY_K] = ScanCode.SC_K,
		[EvDevKeyCode.KEY_L] = ScanCode.SC_L,
		[EvDevKeyCode.KEY_SEMICOLON] = ScanCode.SC_SEMICOLON,
		[EvDevKeyCode.KEY_APOSTROPHE] = ScanCode.SC_APOSTROPHE,
		[EvDevKeyCode.KEY_GRAVE] = ScanCode.SC_GRAVE,
		[EvDevKeyCode.KEY_LEFTSHIFT] = ScanCode.SC_LEFTSHIFT,
		[EvDevKeyCode.KEY_BACKSLASH] = ScanCode.SC_BACKSLASH,
		[EvDevKeyCode.KEY_Z] = ScanCode.SC_Z,
		[EvDevKeyCode.KEY_X] = ScanCode.SC_X,
		[EvDevKeyCode.KEY_C] = ScanCode.SC_C,
		[EvDevKeyCode.KEY_V] = ScanCode.SC_V,
		[EvDevKeyCode.KEY_B] = ScanCode.SC_B,
		[EvDevKeyCode.KEY_N] = ScanCode.SC_N,
		[EvDevKeyCode.KEY_M] = ScanCode.SC_M,
		[EvDevKeyCode.KEY_COMMA] = ScanCode.SC_COMMA,
		[EvDevKeyCode.KEY_DOT] = ScanCode.SC_PERIOD,
		[EvDevKeyCode.KEY_SLASH] = ScanCode.SC_SLASH,
		[EvDevKeyCode.KEY_RIGHTSHIFT] = ScanCode.SC_RIGHTSHIFT,
		[EvDevKeyCode.KEY_KPASTERISK] = ScanCode.SC_MULTIPLY,
		[EvDevKeyCode.KEY_LEFTALT] = ScanCode.SC_LEFTALT,
		[EvDevKeyCode.KEY_SPACE] = ScanCode.SC_SPACEBAR,
		[EvDevKeyCode.KEY_CAPSLOCK] = ScanCode.SC_CAPSLOCK,
		[EvDevKeyCode.KEY_F1] = ScanCode.SC_F1,
		[EvDevKeyCode.KEY_F2] = ScanCode.SC_F2,
		[EvDevKeyCode.KEY_F3] = ScanCode.SC_F3,
		[EvDevKeyCode.KEY_F4] = ScanCode.SC_F4,
		[EvDevKeyCode.KEY_F5] = ScanCode.SC_F5,
		[EvDevKeyCode.KEY_F6] = ScanCode.SC_F6,
		[EvDevKeyCode.KEY_F7] = ScanCode.SC_F7,
		[EvDevKeyCode.KEY_F8] = ScanCode.SC_F8,
		[EvDevKeyCode.KEY_F9] = ScanCode.SC_F9,
		[EvDevKeyCode.KEY_F10] = ScanCode.SC_F10,
		[EvDevKeyCode.KEY_NUMLOCK] = ScanCode.SC_NUMLOCK,
		[EvDevKeyCode.KEY_SCROLLLOCK] = ScanCode.SC_SCROLLLOCK,
		[EvDevKeyCode.KEY_KP7] = ScanCode.SC_NUMPAD7,
		[EvDevKeyCode.KEY_KP8] = ScanCode.SC_NUMPAD8,
		[EvDevKeyCode.KEY_KP9] = ScanCode.SC_NUMPAD9,
		[EvDevKeyCode.KEY_KPMINUS] = ScanCode.SC_SUBSTRACT,
		[EvDevKeyCode.KEY_KP4] = ScanCode.SC_NUMPAD4,
		[EvDevKeyCode.KEY_KP5] = ScanCode.SC_NUMPAD5,
		[EvDevKeyCode.KEY_KP6] = ScanCode.SC_NUMPAD6,
		[EvDevKeyCode.KEY_KPPLUS] = ScanCode.SC_ADD,
		[EvDevKeyCode.KEY_KP1] = ScanCode.SC_NUMPAD1,
		[EvDevKeyCode.KEY_KP2] = ScanCode.SC_NUMPAD2,
		[EvDevKeyCode.KEY_KP3] = ScanCode.SC_NUMPAD3,
		[EvDevKeyCode.KEY_KP0] = ScanCode.SC_NUMPAD0,
		[EvDevKeyCode.KEY_KPDOT] = ScanCode.SC_DECIMAL,
		[EvDevKeyCode.KEY_ZENKAKUHANKAKU] = ScanCode.SC_F24,
		[EvDevKeyCode.KEY_102ND] = ScanCode.SC_EUROPE2,
		[EvDevKeyCode.KEY_F11] = ScanCode.SC_F11,
		[EvDevKeyCode.KEY_F12] = ScanCode.SC_F12,
		[EvDevKeyCode.KEY_RO] = ScanCode.SC_INTL1,
		[EvDevKeyCode.KEY_KATAKANA] = ScanCode.SC_LANG3,
		[EvDevKeyCode.KEY_HIRAGANA] = ScanCode.SC_LANG4,
		[EvDevKeyCode.KEY_HENKAN] = ScanCode.SC_INTL4,
		[EvDevKeyCode.KEY_KATAKANAHIRAGANA] = ScanCode.SC_INTL2,
		[EvDevKeyCode.KEY_MUHENKAN] = ScanCode.SC_INTL5,
		[EvDevKeyCode.KEY_KPJPCOMMA] = ScanCode.SC_INTL6,
		[EvDevKeyCode.KEY_KPENTER] = ScanCode.SC_NUMPADENTER,
		[EvDevKeyCode.KEY_RIGHTCTRL] = ScanCode.SC_RIGHTCONTROL,
		[EvDevKeyCode.KEY_KPSLASH] = ScanCode.SC_DIVIDE,
		[EvDevKeyCode.KEY_SYSRQ] = ScanCode.SC_PRINTSCREEN,
		[EvDevKeyCode.KEY_RIGHTALT] = ScanCode.SC_RIGHTALT,
		[EvDevKeyCode.KEY_HOME] = ScanCode.SC_HOME,
		[EvDevKeyCode.KEY_UP] = ScanCode.SC_UP,
		[EvDevKeyCode.KEY_PAGEUP] = ScanCode.SC_PAGEUP,
		[EvDevKeyCode.KEY_LEFT] = ScanCode.SC_LEFT,
		[EvDevKeyCode.KEY_RIGHT] = ScanCode.SC_RIGHT,
		[EvDevKeyCode.KEY_END] = ScanCode.SC_END,
		[EvDevKeyCode.KEY_DOWN] = ScanCode.SC_DOWN,
		[EvDevKeyCode.KEY_PAGEDOWN] = ScanCode.SC_PAGEDOWN,
		[EvDevKeyCode.KEY_INSERT] = ScanCode.SC_INSERT,
		[EvDevKeyCode.KEY_DELETE] = ScanCode.SC_DELETE,
		[EvDevKeyCode.KEY_MUTE] = ScanCode.SC_MUTE,
		[EvDevKeyCode.KEY_VOLUMEDOWN] = ScanCode.SC_VOLUMEDOWN,
		[EvDevKeyCode.KEY_VOLUMEUP] = ScanCode.SC_VOLUMEUP,
		[EvDevKeyCode.KEY_POWER] = ScanCode.SC_POWER,
		[EvDevKeyCode.KEY_KPEQUAL] = ScanCode.SC_NUMPADEQUALS,
		[EvDevKeyCode.KEY_PAUSE] = ScanCode.SC_PAUSE,
		[EvDevKeyCode.KEY_KPCOMMA] = ScanCode.SC_SEPARATOR,
		[EvDevKeyCode.KEY_YEN] = ScanCode.SC_INTL3,
		[EvDevKeyCode.KEY_LEFTMETA] = ScanCode.SC_LEFTGUI,
		[EvDevKeyCode.KEY_RIGHTMETA] = ScanCode.SC_RIGHTGUI,
		[EvDevKeyCode.KEY_STOP] = ScanCode.SC_STOP,
		[EvDevKeyCode.KEY_MENU] = ScanCode.SC_APPS,
		[EvDevKeyCode.KEY_CALC] = ScanCode.SC_CALCULATOR,
		[EvDevKeyCode.KEY_SLEEP] = ScanCode.SC_SLEEP,
		[EvDevKeyCode.KEY_WAKEUP] = ScanCode.SC_WAKE,
		[EvDevKeyCode.KEY_MAIL] = ScanCode.SC_MAIL,
		[EvDevKeyCode.KEY_BOOKMARKS] = ScanCode.SC_BROWSERFAVORITES,
		[EvDevKeyCode.KEY_COMPUTER] = ScanCode.SC_MYCOMPUTER,
		[EvDevKeyCode.KEY_BACK] = ScanCode.SC_BROWSERBACK,
		[EvDevKeyCode.KEY_FORWARD] = ScanCode.SC_BROWSERFORWARD,
		[EvDevKeyCode.KEY_NEXTSONG] = ScanCode.SC_NEXTTRACK,
		[EvDevKeyCode.KEY_PLAYPAUSE] = ScanCode.SC_PLAYPAUSE,
		[EvDevKeyCode.KEY_PREVIOUSSONG] = ScanCode.SC_PREVTRACK,
		[EvDevKeyCode.KEY_HOMEPAGE] = ScanCode.SC_BROWSERHOME,
		[EvDevKeyCode.KEY_REFRESH] = ScanCode.SC_BROWSERREFRESH,
		[EvDevKeyCode.KEY_F13] = ScanCode.SC_F13,
		[EvDevKeyCode.KEY_F14] = ScanCode.SC_F14,
		[EvDevKeyCode.KEY_F15] = ScanCode.SC_F15,
		[EvDevKeyCode.KEY_F16] = ScanCode.SC_F16,
		[EvDevKeyCode.KEY_F17] = ScanCode.SC_F17,
		[EvDevKeyCode.KEY_F18] = ScanCode.SC_F18,
		[EvDevKeyCode.KEY_F19] = ScanCode.SC_F19,
		[EvDevKeyCode.KEY_F20] = ScanCode.SC_F20,
		[EvDevKeyCode.KEY_F21] = ScanCode.SC_F21,
		[EvDevKeyCode.KEY_F22] = ScanCode.SC_F22,
		[EvDevKeyCode.KEY_F23] = ScanCode.SC_F23,
		[EvDevKeyCode.KEY_F24] = ScanCode.SC_F24,
		[EvDevKeyCode.KEY_SEARCH] = ScanCode.SC_BROWSERSEARCH,
		[EvDevKeyCode.KEY_MEDIA] = ScanCode.SC_MEDIASELECT,
	}.ToFrozenDictionary();

	private sealed record EvDevKeyboard(
		uint DriverVersion, ushort IdBus, ushort IdVendor, ushort IdProduct, ushort IdVersion, string Name, int Fd, string Path) : IDisposable
	{
		public void Dispose()
		{
			_ = close(Fd);
		}

		public override string ToString()
		{
			var verMajor = DriverVersion >> 16;
			var verMinor = DriverVersion >> 8 & 0xFF;
			var varRev = DriverVersion & 0xFF;
			return $"{Name} ({verMajor}.{verMinor}.{varRev} {IdBus:X4}/{IdVendor:X4}/{IdProduct:X4}/{IdVersion:X4})";
		}
	}

	private static List<int> DecodeBits(ReadOnlySpan<byte> bits)
	{
		var result = new List<int>(bits.Length * 8);
		for (var i = 0; i < bits.Length; i++)
		{
			var b = bits[i];
			var bitPos = i * 8;
			for (var j = 0; j < 8; j++)
			{
				if ((b & (1 << j)) != 0)
				{
					result.Add(bitPos + j);
				}
			}
		}

		return result;
	}

	private unsafe void TryAddKeyboard(string path)
	{
		if (_keyboards.ContainsKey(path))
		{
			// already have this, ignore
			return;
		}

		var fd = open(path, O_RDONLY | O_NONBLOCK | O_CLOEXEC);
		if (fd == -1)
		{
			return;
		}

		var version = 0u;
		Span<ushort> id = stackalloc ushort[4];
		id.Clear();
		Span<byte> str = stackalloc byte[256];
		str.Clear();

		// if any of these fail, the device was either removed or garbage
		if (ioctl(fd, EVIOCGVERSION, ref version) == -1 ||
			ioctl(fd, EVIOCGID, id) == -1 ||
			ioctl(fd, EVIOCGNAME(256), str) == -1)
		{
			_ = close(fd);
			return;
		}

		str[^1] = 0;
		var name = Encoding.UTF8.GetString(str[..(str.IndexOf((byte)0) + 1)]);

		Span<byte> eventBits = stackalloc byte[(int)EvDevEventType.EV_MAX / 8 + 1];
		eventBits.Clear();
		if (ioctl(fd, EVIOCGBIT(EvDevEventType.EV_SYN, (int)EvDevEventType.EV_MAX), eventBits) == -1)
		{
			_ = close(fd);
			return;
		}

		var supportedEvents = DecodeBits(eventBits);
		if (!supportedEvents.Contains((int)EvDevEventType.EV_KEY))
		{
			// we only care about keyboards
			_ = close(fd);
			return;
		}

		Span<byte> keyBits = stackalloc byte[(int)EvDevKeyCode.KEY_MAX / 8 + 1];
		keyBits.Clear();
		if (ioctl(fd, EVIOCGBIT(EvDevEventType.EV_KEY, (int)EvDevKeyCode.KEY_MAX), keyBits) == -1)
		{
			_ = close(fd);
			return;
		}

		var supportedKeys = DecodeBits(keyBits);
		if (supportedKeys.Count == 0)
		{
			// probably garbage
			return;
		}

		if (!name.Contains("keyboard", StringComparison.OrdinalIgnoreCase))
		{
			// probably not be a keyboard
			// TODO: do some better heuristics here (maybe check if supportedKeys has A-Z?)
			return;
		}

		var keyboard = new EvDevKeyboard(
			DriverVersion: version,
			IdBus: id[0],
			IdProduct: id[1],
			IdVendor: id[2],
			IdVersion: id[3],
			Name: name,
			Fd: fd,
			Path: path
		);

		Console.WriteLine($"Added evdev keyboard {keyboard}");
		_keyboards.Add(path, keyboard);
	}

	private void TryRemoveKeyboard(string path)
	{
		if (_keyboards.TryGetValue(path, out var keyboard))
		{
			Console.WriteLine($"Removed evdev keyboard {keyboard}");
			keyboard.Dispose();
			_keyboards.Remove(path);
		}
	}

	private void OnWatcherEvent(object _, FileSystemEventArgs e)
	{
		_watcherEvents.Enqueue(e);
	}

	protected readonly bool CanUseEvDev;

	private readonly ConcurrentQueue<FileSystemEventArgs> _watcherEvents = new();
	private readonly FileSystemWatcher _fileSystemWatcher;
	private readonly Dictionary<string, EvDevKeyboard> _keyboards = [];

	public EvDevKeyInput()
		: this(false)
	{
	}

	protected EvDevKeyInput(bool needsRoot)
	{
		// this could be the case for our wayland backend, which might not have evdev available for us
		// (main use case is WSL2)
		if (!HasEvDev)
		{
			CanUseEvDev = false;
			return;
		}

		// avoid doing anything if root is required and is unavailable
		if (needsRoot && !HasRoot)
		{
			CanUseEvDev = false;
			return;
		}

		_fileSystemWatcher = new("/dev/input/", "event*")
		{
			NotifyFilter = NotifyFilters.FileName | NotifyFilters.Attributes,
		};

		_fileSystemWatcher.Created += OnWatcherEvent;
		_fileSystemWatcher.Changed += OnWatcherEvent;
		_fileSystemWatcher.Deleted += OnWatcherEvent;
		_fileSystemWatcher.EnableRaisingEvents = true;

		var evFns = Directory.GetFiles("/dev/input/", "event*");
		foreach (var fn in evFns)
		{
			TryAddKeyboard(fn);
		}

		CanUseEvDev = true;
	}

	public virtual void Dispose()
	{
		_fileSystemWatcher?.Dispose();

		foreach (var keyboard in _keyboards.Values)
		{
			keyboard.Dispose();
		}
	}

	public virtual IEnumerable<KeyEvent> GetEvents()
	{
		while (_watcherEvents.TryDequeue(out var e))
		{
			// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
			switch (e.ChangeType)
			{
				case WatcherChangeTypes.Created:
				case WatcherChangeTypes.Changed:
					TryAddKeyboard(e.FullPath);
					break;
				case WatcherChangeTypes.Deleted:
					TryRemoveKeyboard(e.FullPath);
					break;
				default:
					Debug.WriteLine($"Unexpected evdev watcher event {e.ChangeType}");
					break;
			}
		}

		var kbEvent = default(EvDevKeyboardEvent);
		var kbEventSize = (uint)Unsafe.SizeOf<EvDevKeyboardEvent>();
		var kbsToClose = new List<string>();
		var keyEvents = new List<KeyEvent>();

		foreach (var keyboard in _keyboards.Values)
		{
			while (true)
			{
				var res = read(keyboard.Fd, ref kbEvent, kbEventSize);
				if (res == -1)
				{
					var errno = Marshal.GetLastPInvokeError();

					// EAGAIN means there's no more events left to read (generally expected)
					if (errno == EAGAIN)
					{
						break;
					}

					// ENODEV means the device is gone
					if (errno == ENODEV)
					{
						// can't remove the kbs while iterating them!
						kbsToClose.Add(keyboard.Path);
						break;
					}

					Debug.WriteLine($"Unexpected error reading evdev keyboards: {errno}");
					break;
				}

				if (res != kbEventSize)
				{
					Debug.WriteLine("Unexpected incomplete evdev read");
					break;
				}

				if (kbEvent.type != EvDevEventType.EV_KEY)
				{
					// don't care for non-EV_KEY events
					continue;
				}

				if (EvDevKeyCodeMap.TryGetValue(kbEvent.code, out var key))
				{
					switch (kbEvent.value)
					{
						case EvDevKeyValue.KeyUp:
							keyEvents.Add(new(key, IsPressed: false));
							break;
						case EvDevKeyValue.KeyDown:
							keyEvents.Add(new(key, IsPressed: true));
							break;
						case EvDevKeyValue.KeyRepeat:
							// should this be considered a press event?
							// probably not particularly useful to do so
							break;
						default:
							Debug.WriteLine($"Unexpected evdev event value {kbEvent.value}");
							break;
					}
				}
			}
		}

		foreach (var path in kbsToClose)
		{
			TryRemoveKeyboard(path);
		}

		return keyEvents;
	}

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

	public virtual string ConvertScanCodeToString(ScanCode key)
	{
		return _scanCodeStrMap.GetValueOrDefault(key);
	}
}
