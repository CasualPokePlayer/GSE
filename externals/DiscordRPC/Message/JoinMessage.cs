﻿using System.Text.Json.Serialization;

namespace DiscordRPC.Message;

/// <summary>
/// Called when the Discord Client wishes for this process to join a game. D -> C.
/// </summary>
public class JoinMessage : BaseMessage
{
	/// <summary>
	/// The type of message received from discord
	/// </summary>
	[JsonIgnore]
	public override MessageType Type => MessageType.Join;

	/// <summary>
	/// The <see cref="Secrets.JoinSecret" /> to connect with. 
	/// </summary>
	[JsonPropertyName("secret"), JsonInclude]
	public string Secret { get; internal set; }		
}
