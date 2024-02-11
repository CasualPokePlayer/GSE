namespace GSR.Emu.Controllers;

public interface IEmuController
{
	EmuControllerState GetState(bool immediateUpdate);
}
