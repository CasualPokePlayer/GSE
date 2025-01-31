using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscordRPC.Helper;

internal sealed class JsonStringSnakeCaseEnumConverter<TEnum>()
	: JsonStringEnumConverter<TEnum>(namingPolicy: JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: true)
	where TEnum : struct, Enum;
