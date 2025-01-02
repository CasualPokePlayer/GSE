using System.Text.Json.Serialization;

namespace DiscordRPC.RPC.Payload;

/// <summary>
/// Base Payload that is received by both client and server
/// </summary>
internal abstract class BasePayload
{
	/// <summary>
	/// The type of payload
	/// </summary>
	[JsonPropertyName("cmd"), JsonConverter(typeof(JsonStringEnumConverter<Command>))]
	public Command Command { get; set; }

	/// <summary>
	/// A incremental value to help identify payloads
	/// </summary>
	[JsonPropertyName("nonce")]
	public string Nonce { get; set; }

	protected BasePayload()
	{
	}

	protected BasePayload(long nonce)
	{
		Nonce = nonce.ToString();
	}

	public override string ToString()
	{
		return $"Payload || Command: {Command}, Nonce: {Nonce}";
	}
}
