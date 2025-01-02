namespace DiscordRPC.RPC.Payload;

/// <summary>
/// See https://discordapp.com/developers/docs/topics/rpc#rpc-server-payloads-rpc-events for documentation
/// </summary>
internal enum ServerEvent
{
	/// <summary>
	/// Sent when the server is ready to accept messages
	/// </summary>
	//[JsonStringEnumMemberName("READY")]
	Ready,

	/// <summary>
	/// Sent when something bad has happened
	/// </summary>
	//[JsonStringEnumMemberName("ERROR")]
	Error,

	/// <summary>
	/// Join Event 
	/// </summary>
	//[JsonStringEnumMemberName("ACTIVITY_JOIN")]
	ActivityJoin,

	/// <summary>
	/// Spectate Event
	/// </summary>
	//[JsonStringEnumMemberName("ACTIVITY_SPECTATE")]
	ActivitySpectate,

	/// <summary>
	/// Request Event
	/// </summary>
	//[JsonStringEnumMemberName("ACTIVITY_JOIN_REQUEST")]
	ActivityJoinRequest,
}
