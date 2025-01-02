using System.Text.Json.Serialization;

using DiscordRPC.RPC.Payload;

namespace DiscordRPC.RPC.Commands;

internal class RespondCommand : ICommand
{
	/// <summary>
	/// The user ID that we are accepting / rejecting
	/// </summary>
	[JsonPropertyName("user_id")]
	public string UserID { get; set; }

	/// <summary>
	/// If true, the user will be allowed to connect.
	/// </summary>
	[JsonIgnore]
	public bool Accept { get; set; }

	public BasePayload PreparePayload(long nonce)
	{
		return new ArgumentPayload<RespondCommand>(this, nonce)
		{
			Command = Accept
				? Command.SendActivityJoinInvite
				: Command.CloseActivityJoinRequest
		};
	}
}
