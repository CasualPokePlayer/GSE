using System.Text.Json.Serialization;

namespace DiscordRPC.Message;

/// <summary>
/// Called when some other person has requested access to this game. C -> D -> C.
/// </summary>
public sealed class JoinRequestMessage : BaseMessage
{
	/// <summary>
	/// The type of message received from discord
	/// </summary>
	[JsonIgnore]
	public override MessageType Type => MessageType.JoinRequest;

	/// <summary>
	/// The discord user that is requesting access.
	/// </summary>
	[JsonPropertyName("user"), JsonInclude]
	public User User { get; internal set; }
}
