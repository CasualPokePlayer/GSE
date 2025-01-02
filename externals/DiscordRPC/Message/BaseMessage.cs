using System;
using System.Text.Json.Serialization;

namespace DiscordRPC.Message;

/// <summary>
/// Messages received from discord.
/// </summary>
public abstract class BaseMessage
{
	/// <summary>
	/// The type of message received from discord
	/// </summary>
	[JsonIgnore]
	public abstract MessageType Type { get; }

	/// <summary>
	/// The time the message was created
	/// </summary>
	[JsonIgnore]
	public DateTime TimeCreated { get; }

	/// <summary>
	/// Creates a new instance of the message
	/// </summary>
	protected BaseMessage()
	{
		TimeCreated = DateTime.Now;
	}
}
