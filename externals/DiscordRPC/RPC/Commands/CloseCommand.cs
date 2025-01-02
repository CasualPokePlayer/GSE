using DiscordRPC.RPC.Payload;

namespace DiscordRPC.RPC.Commands;

/// <summary>
/// This more a psuedo-command, actual payload sent is <see cref="IO.Handshake" />
/// </summary>
internal sealed class CloseCommand : ICommand
{
	private sealed class DummyPayload : BasePayload;

	public BasePayload PreparePayload(long nonce)
	{
		return new DummyPayload();
	}
}
