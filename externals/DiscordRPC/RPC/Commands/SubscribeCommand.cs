using DiscordRPC.RPC.Payload;

namespace DiscordRPC.RPC.Commands;

internal class SubscribeCommand : ICommand
{
	public ServerEvent Event { get; set; }
	public bool IsUnsubscribe { get; set; }

	public BasePayload PreparePayload(long nonce)
	{
		return new EventPayload(nonce)
		{
			Command = IsUnsubscribe ? Command.Unsubscribe : Command.Subscribe,
			Event = Event
		};
	}
}
