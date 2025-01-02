// based on https://github.com/HerrMaroni/discord-rpc-csharp/blob/0e53ac1/DiscordRPC/Helper/JsonSerializationContext.cs

using System.Text.Json.Serialization;

using DiscordRPC.IO;
using DiscordRPC.Message;
using DiscordRPC.RPC.Commands;
using DiscordRPC.RPC.Payload;

namespace DiscordRPC.Helper;

#region Commands

[JsonSerializable(typeof(PresenceCommand))]
[JsonSerializable(typeof(RespondCommand))]

#endregion

#region Payload

[JsonSerializable(typeof(ArgumentPayload<PresenceCommand>))]
[JsonSerializable(typeof(ArgumentPayload<RespondCommand>))]
[JsonSerializable(typeof(BasePayload))]
[JsonSerializable(typeof(ClosePayload))]
[JsonSerializable(typeof(EventPayload))]

#endregion

#region Response

[JsonSerializable(typeof(RichPresenceResponse))]

#endregion

#region IO

[JsonSerializable(typeof(Handshake))]

#endregion

#region Message

[JsonSerializable(typeof(ErrorMessage))]
[JsonSerializable(typeof(JoinMessage))]
[JsonSerializable(typeof(JoinRequestMessage))]
[JsonSerializable(typeof(ReadyMessage))]
[JsonSerializable(typeof(SpectateMessage))]

#endregion

internal sealed partial class JsonSerializationContext : JsonSerializerContext;
