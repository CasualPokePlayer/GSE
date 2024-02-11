using System;

namespace GSR.Emu;

[Flags]
public enum EmuButtons : uint
{
	A = 1 << 0,
	B = 1 << 1,
	Select = 1 << 2,
	Start = 1 << 3,
	Right = 1 << 4,
	Left = 1 << 5,
	Up = 1 << 6,
	Down = 1 << 7,
	R = 1 << 8,
	L = 1 << 9,

	GB_BUTTON_MASK = A | B | Select | Start | Right | Left | Up | Down,
	GBA_BUTTON_MASK = A | B | Select | Start | Right | Left | Up | Down | R | L,
	LR_DIR_MASK = Right | Left,
	UD_DIR_MASK = Up | Down,

	HardReset = 1u << 31,
}

public readonly record struct EmuControllerState(EmuButtons InputState)
{
	public EmuButtons GBInputState => InputState & EmuButtons.GB_BUTTON_MASK;

	public EmuButtons GBAInputState => InputState & EmuButtons.GBA_BUTTON_MASK;

	public bool HardReset => (InputState & EmuButtons.HardReset) != 0;
}
