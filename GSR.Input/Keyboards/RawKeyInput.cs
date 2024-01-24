using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input;
using Windows.Win32.UI.WindowsAndMessaging;

namespace GSR.Input.Keyboards;

[SupportedOSPlatform("windows5.1.2600")]
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

		var ud = IntPtr.Size == 8
			? PInvoke.GetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA)
			: PInvoke.GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_USERDATA);
		if (ud == IntPtr.Zero)
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
				if (IntPtr.Size == 8)
				{
					PInvoke.SetWindowLongPtr(RawInputWindow, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA, GCHandle.ToIntPtr(handle));
				}
				else
				{
					PInvoke.SetWindowLong(RawInputWindow, WINDOW_LONG_PTR_INDEX.GWL_USERDATA, (int)GCHandle.ToIntPtr(handle));
				}

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

	public string ConvertScanCodeToString(ScanCode key)
	{
		return string.Empty;
	}
}
