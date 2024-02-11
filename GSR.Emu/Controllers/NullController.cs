namespace GSR.Emu.Controllers;

internal sealed class NullController : IEmuController
{
	public static readonly NullController Singleton = new();

	private NullController()
	{
	}

	public EmuControllerState GetState(bool immediateUpdate) => default;
}
