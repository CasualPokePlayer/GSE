namespace GSR.Input.Keyboards;

internal readonly record struct KeyEvent(ScanCode Key, bool IsPressed);
