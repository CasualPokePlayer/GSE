using System.Text.Json.Serialization;

using DiscordRPC.RPC.Payload;

namespace DiscordRPC.RPC.Commands;

internal sealed class PresenceCommand : ICommand
{
	/// <summary>
	/// The process ID
	/// </summary>
	[JsonPropertyName("pid")]
	public int PID { get; set; }

	/// <summary>
	/// The rich presence to be set. Can be null.
	/// </summary>
	[JsonPropertyName("activity"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public RichPresence Presence { get; set; }

	public BasePayload PreparePayload(long nonce)
	{
		return new ArgumentPayload<PresenceCommand>(this, nonce)
		{
			Command = Command.SetActivity
		};
	}
}
