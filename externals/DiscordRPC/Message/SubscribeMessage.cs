using DiscordRPC.RPC.Payload;

namespace DiscordRPC.Message;

/// <summary>
/// Called as validation of a subscribe
/// </summary>
public sealed class SubscribeMessage : BaseMessage
{
	/// <summary>
	/// The type of message received from discord
	/// </summary>
	public override MessageType Type => MessageType.Subscribe;

	/// <summary>
	/// The event that was subscribed too.
	/// </summary>
	public EventType Event { get; }

	internal SubscribeMessage(ServerEvent evt)
	{
        Event = evt switch
        {
            ServerEvent.ActivityJoinRequest => EventType.JoinRequest,
            ServerEvent.ActivitySpectate => EventType.Spectate,
            _ => EventType.Join,
        };
    }
}
