using System;
using System.Collections.Generic;

namespace GSR.Input.Keyboards;

internal interface IKeyInput : IDisposable
{
	IEnumerable<KeyEvent> GetEvents();
	string ConvertScanCodeToString(ScanCode key);
}
