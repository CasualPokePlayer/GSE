using System.Text.Json.Serialization;

namespace DiscordRPC.Message;

/// <summary>
/// Called when the Discord Client wishes for this process to spectate a game. D -> C. 
/// </summary>
public sealed class SpectateMessage : JoinMessage
{
	/// <summary>
	/// The type of message received from discord
	/// </summary>
	[JsonIgnore]
	public override MessageType Type => MessageType.Spectate;
}
