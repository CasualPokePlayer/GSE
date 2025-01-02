namespace DiscordRPC.RPC.Payload;

/// <summary>
/// The possible commands that can be sent and received by the server.
/// </summary>
internal enum Command
{
	/// <summary>
	/// event dispatch
	/// </summary>
	//[JsonStringEnumMemberName("DISPATCH")]
	Dispatch,

	/// <summary>
	/// Called to set the activity
	/// </summary>
	//[JsonStringEnumMemberName("SET_ACTIVITY")]
	SetActivity,

	/// <summary>
	/// used to subscribe to an RPC event
	/// </summary>
	//[JsonStringEnumMemberName("SUBSCRIBE")]
	Subscribe,

	/// <summary>
	/// used to unsubscribe from an RPC event
	/// </summary>
	//[JsonStringEnumMemberName("UNSUBSCRIBE")]
	Unsubscribe,

	/// <summary>
	/// Used to accept join requests.
	/// </summary>
	//[JsonStringEnumMemberName("SEND_ACTIVITY_JOIN_INVITE")]
	SendActivityJoinInvite,

	/// <summary>
	/// Used to reject join requests.
	/// </summary>
	//[JsonStringEnumMemberName("CLOSE_ACTIVITY_JOIN_REQUEST")]
	CloseActivityJoinRequest,
}
