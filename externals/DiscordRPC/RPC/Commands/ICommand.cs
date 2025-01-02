using DiscordRPC.RPC.Payload;

namespace DiscordRPC.RPC.Commands;

internal interface ICommand
{
	BasePayload PreparePayload(long nonce);
}
