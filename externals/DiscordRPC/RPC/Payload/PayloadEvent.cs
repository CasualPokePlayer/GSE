using System.Text.Json;
using System.Text.Json.Serialization;

using DiscordRPC.Helper;

namespace DiscordRPC.RPC.Payload;

/// <summary>
/// Used for Discord IPC Events
/// </summary>
internal sealed class EventPayload : BasePayload
{
	/// <summary>
	/// The data the server sent too us
	/// </summary>
	[JsonPropertyName("data"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public JsonDocument Data { get; set; }

	/// <summary>
	/// The type of event the server sent
	/// </summary>
	[JsonPropertyName("evt"), JsonConverter(typeof(JsonStringSnakeCaseEnumConverter<ServerEvent>))]
	public ServerEvent? Event { get; set; }

	/// <summary>
	/// Creates a payload with empty data
	/// </summary>
	public EventPayload()
	{
		Data = null;
	}

	/// <summary>
	/// Creates a payload with empty data and a set nonce
	/// </summary>
	/// <param name="nonce"></param>
	public EventPayload(long nonce)
		: base(nonce)
	{
		Data = null;
	}

	/// <summary>
	/// Gets the object stored within the Data
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public T GetObject<T>()
	{
		if (Data == null) return default;
		return (T)Data.Deserialize(typeof(T), JsonSerializationContext.Default);
	}

	/// <summary>
	/// Converts the object into a human readable string
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		return "Event " + base.ToString() + ", Event: " + (Event.HasValue ? Event.ToString() : "N/A");
	}
}
