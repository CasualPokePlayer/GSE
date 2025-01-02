using System;
using System.Text;
using System.Text.Json.Serialization;

using DiscordRPC.Exceptions;
using DiscordRPC.Helper;

namespace DiscordRPC;

/// <summary>
/// The base rich presence structure
/// </summary>
public class BaseRichPresence
{
	/// <summary>
	/// The user's current <see cref="Party"/> status. For example, "Playing Solo" or "With Friends".
	/// <para>Max 128 bytes</para>
	/// </summary>
	[JsonPropertyName("state"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string State
	{
		get => _state;
		set
		{
			if (!ValidateString(value, out _state, 128, Encoding.UTF8))
				throw new StringOutOfRangeException("State", 0, 128);
		}
	}

	/// <summary>Inernal inner state string</summary>
	protected string _state;

	/// <summary>
	/// What the user is currently doing. For example, "Competitive - Total Mayhem".
	/// <para>Max 128 bytes</para>
	/// </summary>
	[JsonPropertyName("details"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string Details
	{
		get => _details;
		set
		{
			if (!ValidateString(value, out _details, 128, Encoding.UTF8))
				throw new StringOutOfRangeException(128);
		}
	}
	/// <summary>Inernal inner detail string</summary>
	protected string _details;

	/// <summary>
	/// The time elapsed / remaining time data.
	/// </summary>
	[JsonPropertyName("timestamps"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Timestamps Timestamps { get; set; }

	/// <summary>
	/// The names of the images to use and the tooltips to give those images.
	/// </summary>
	[JsonPropertyName("assets"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Assets Assets { get; set; }

	/// <summary>
	/// The party the player is currently in. The <see cref="Party.ID"/> must be set for this to be included in the RichPresence update.
	/// </summary>
	[JsonPropertyName("party"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Party Party { get; set; }

	/// <summary>
	/// The secrets used for Join / Spectate. Secrets are obfuscated data of your choosing. They could be match ids, player ids, lobby ids, etc. Make this object null if you do not wish too / unable too implement the Join / Request feature.
	/// <para>To keep security on the up and up, Discord requires that you properly hash/encode/encrypt/put-a-padlock-on-and-swallow-the-key-but-wait-then-how-would-you-open-it your secrets.</para>
	/// <para>Visit the <see href="https://discordapp.com/developers/docs/rich-presence/how-to#secrets">Rich Presence How-To</see> for more information.</para>
	/// </summary>
	[JsonPropertyName("secrets"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Secrets Secrets { get; set; }

	/// <summary>
	/// The activity type
	/// </summary>
	[JsonPropertyName("type")]
	public ActivityType Type { get; set; }

	/// <summary>
	/// Marks the <see cref="Secrets.MatchSecret"/> as a game session with a specific beginning and end. It was going to be used as a form of notification, but was replaced with the join feature. It may potentially have use in the future, but it currently has no use.
	/// <para>
	/// "TLDR it marks the matchSecret field as an instance, that is to say a context in game that’s not like a lobby state/not in game state. It was gonna he used for notify me, but we scrapped that for ask to join. We may put it to another use in the future. For now, don’t worry about it" - Mason (Discord API Server 14 / 03 / 2018)
	/// </para>
	/// </summary>
	[JsonPropertyName("instance"), JsonInclude]
	[Obsolete("This was going to be used, but was replaced by JoinSecret instead")]
	internal bool Instance { get; set; }

	#region Has Checks
	/// <summary>
	/// Does the Rich Presence have valid timestamps?
	/// </summary>
	/// <returns></returns>
	public bool HasTimestamps()
	{
		return Timestamps != null && (Timestamps.Start != null || Timestamps.End != null);
	}

	/// <summary>
	/// Does the Rich Presence have valid assets?
	/// </summary>
	/// <returns></returns>
	public bool HasAssets()
	{
		return Assets != null;
	}

	/// <summary>
	/// Does the Rich Presence have a valid party?
	/// </summary>
	/// <returns></returns>
	public bool HasParty()
	{
		return Party is { ID: not null };
	}

	/// <summary>
	/// Does the Rich Presence have valid secrets?
	/// </summary>
	/// <returns></returns>
	public bool HasSecrets()
	{
		return Secrets != null && (Secrets.JoinSecret != null || Secrets.SpectateSecret != null);
	}

	#endregion

	/// <summary>
	/// Attempts to call <see cref="StringTools.GetNullOrString(string)"/> on the string and return the result, if its within a valid length.
	/// </summary>
	/// <param name="str">The string to check</param>
	/// <param name="result">The formatted string result</param>
	/// <param name="bytes">The maximum number of bytes the string can take up</param>
	/// <param name="encoding">The encoding to count the bytes with</param>
	/// <returns>True if the string fits within the number of bytes</returns>
	internal static bool ValidateString(string str, out string result, int bytes, Encoding encoding)
	{
		result = str;
		if (str == null)
			return true;

		// Trim the string, for the best chance of fitting
		var s = str.Trim();

		// Make sure it fits
		if (!s.WithinLength(bytes, encoding))
			return false;

		// Make sure its not empty
		result = s.GetNullOrString();
		return true;
	}

	/// <summary>
	/// Operator that converts a presence into a boolean for null checks.
	/// </summary>
	/// <param name="presesnce"></param>
	public static implicit operator bool(BaseRichPresence presesnce)
	{
		return presesnce != null;
	}

	/// <summary>
	/// Checks if the other rich presence differs from the current one
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	internal virtual bool Matches(RichPresence other)
	{
#pragma warning disable CS0618 // Type or member is obsolete
		if (other == null)
			return false;

		if (State != other.State || Details != other.Details || Type != other.Type)
			return false;

		// Checks if the timestamps are different
		if (Timestamps != null)
		{
			if (other.Timestamps == null ||
			    other.Timestamps.StartUnixMilliseconds != Timestamps.StartUnixMilliseconds ||
			    other.Timestamps.EndUnixMilliseconds != Timestamps.EndUnixMilliseconds)
				return false;
		}
		else if (other.Timestamps != null)
		{
			return false;
		}

		// Checks if the secrets are different
		if (Secrets != null)
		{
			if (other.Secrets == null ||
			    other.Secrets.JoinSecret != Secrets.JoinSecret ||
			    other.Secrets.MatchSecret != Secrets.MatchSecret ||
			    other.Secrets.SpectateSecret != Secrets.SpectateSecret)
				return false;
		}
		else if (other.Secrets != null)
		{
			return false;
		}

		// Checks if the timestamps are different
		if (Party != null)
		{
			if (other.Party == null ||
			    other.Party.ID != Party.ID ||
			    other.Party.Max != Party.Max ||
			    other.Party.Size != Party.Size ||
			    other.Party.Privacy != Party.Privacy)
				return false;
		}
		else if (other.Party != null)
		{
			return false;
		}

		// Checks if the assets are different
		if (Assets != null)
		{
			if (other.Assets == null ||
			    other.Assets.LargeImageKey != Assets.LargeImageKey ||
			    other.Assets.LargeImageText != Assets.LargeImageText ||
			    other.Assets.SmallImageKey != Assets.SmallImageKey ||
			    other.Assets.SmallImageText != Assets.SmallImageText)
				return false;
		}
		else if (other.Assets != null)
		{
			return false;
		}

		return Instance == other.Instance;
#pragma warning restore CS0618 // Type or member is obsolete
	}

	/// <summary>
	/// Converts this BaseRichPresence to RichPresence
	/// </summary>
	/// <returns></returns>
	public RichPresence ToRichPresence()
	{
		var presence = new RichPresence
		{
			State = State,
			Details = Details,
			Type = Type,
			Party = !HasParty() ? Party : null,
			Secrets = !HasSecrets() ? Secrets : null
		};

		if (HasAssets())
		{
			presence.Assets = new Assets()
			{
				SmallImageKey = Assets.SmallImageKey,
				SmallImageText = Assets.SmallImageText,

				LargeImageKey = Assets.LargeImageKey,
				LargeImageText = Assets.LargeImageText
			};
		}

		if (HasTimestamps())
		{
			presence.Timestamps = new Timestamps();
			if (Timestamps.Start.HasValue) presence.Timestamps.Start = Timestamps.Start;
			if (Timestamps.End.HasValue) presence.Timestamps.End = Timestamps.End;
		}

		return presence;
	}
}

/// <summary>
/// The secrets used for Join / Spectate. Secrets are obfuscated data of your choosing. They could be match ids, player ids, lobby ids, etc.
/// <para>To keep security on the up and up, Discord requires that you properly hash/encode/encrypt/put-a-padlock-on-and-swallow-the-key-but-wait-then-how-would-you-open-it your secrets.</para>
/// <para>You should send discord data that someone else's game client would need to join or spectate their friend. If you can't or don't want to support those actions, you don't need to send secrets.</para>
/// <para>Visit the <see href="https://discordapp.com/developers/docs/rich-presence/how-to#secrets">Rich Presence How-To</see> for more information.</para>
/// </summary>
public class Secrets
{
	/// <summary>
	/// The unique match code to distinguish different games/lobbies. Use <see cref="Secrets.CreateSecret(Random)"/> to get an appropriately sized secret. 
	/// <para>This cannot be null and must be supplied for the  Join / Spectate feature to work.</para>
	/// <para>Max Length of 128 Bytes</para>
	/// </summary>
	[Obsolete("This feature has been deprecated my Mason in issue #152 on the offical library. Was originally used as a Notify Me feature, it has been replaced with Join / Spectate.")]
	[JsonPropertyName("match"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string MatchSecret
	{
		get => _matchSecret;
		set
		{
			if (!BaseRichPresence.ValidateString(value, out _matchSecret, 128, Encoding.UTF8))
				throw new StringOutOfRangeException(128);
		}
	}
	private string _matchSecret;

	/// <summary>
	/// The secret data that will tell the client how to connect to the game to play. This could be a unique identifier for a fancy match maker or player id, lobby id, etc.
	/// <para>It is recommended to encrypt this information so its hard for people to replicate it. 
	/// Do <b>NOT</b> just use the IP address in this. That is a bad practice and can leave your players vulnerable!
	/// </para>
	/// <para>Max Length of 128 Bytes</para>
	/// </summary>
	[JsonPropertyName("join"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string JoinSecret
	{
		get => _joinSecret;
		set
		{
			if (!BaseRichPresence.ValidateString(value, out _joinSecret, 128, Encoding.UTF8))
				throw new StringOutOfRangeException(128);
		}
	}
	private string _joinSecret;

	/// <summary>
	/// The secret data that will tell the client how to connect to the game to spectate. This could be a unique identifier for a fancy match maker or player id, lobby id, etc.
	/// <para>It is recommended to encrypt this information so its hard for people to replicate it. 
	/// Do <b>NOT</b> just use the IP address in this. That is a bad practice and can leave your players vulnerable!
	/// </para>
	/// <para>Max Length of 128 Bytes</para>
	/// </summary>
	[JsonPropertyName("spectate"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string SpectateSecret
	{
		get => _spectateSecret;
		set
		{
			if (!BaseRichPresence.ValidateString(value, out _spectateSecret, 128, Encoding.UTF8))
				throw new StringOutOfRangeException(128);
		}
	}
	private string _spectateSecret;


	#region Statics

	/// <summary>
	/// The encoding the secret generator is using
	/// </summary>
	public static Encoding Encoding => Encoding.UTF8;

	/// <summary>
	/// The length of a secret in bytes.
	/// </summary>
	public static int SecretLength => 128;

	/// <summary>
	/// Creates a new secret. This is NOT a cryptographic function and should NOT be used for sensitive information. This is mainly provided as a way to generate quick IDs.
	/// </summary>
	/// <param name="random">The random to use</param>
	/// <returns>Returns a <see cref="SecretLength"/> sized string with random characters from <see cref="Encoding"/></returns>
	public static string CreateSecret(Random random)
	{
		// Prepare an array and fill it with random bytes
		// THIS IS NOT SECURE! DO NOT USE THIS FOR PASSWORDS!
		var bytes = new byte[SecretLength];
		random.NextBytes(bytes);

		// Return the encoding. Probably should remove invalid characters but cannot be fucked.
		return Encoding.GetString(bytes);
	}

	/// <summary>
	/// Creates a secret word using more readable friendly characters. Useful for debugging purposes. This is not a cryptographic function and should NOT be used for sensitive information.
	/// </summary>
	/// <param name="random">The random used to generate the characters</param>
	/// <returns></returns>
	public static string CreateFriendlySecret(Random random)
	{
		const string charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

		var builder = new StringBuilder();
		for (var i = 0; i < SecretLength; i++)
			builder.Append(charset[random.Next(charset.Length)]);

		return builder.ToString();
	}
	#endregion
}

/// <summary>
/// Information about the pictures used in the Rich Presence.
/// </summary>
public class Assets
{
	/// <summary>
	/// Name of the uploaded image for the large profile artwork.
	/// <para>Max 256 Bytes.</para>
	/// </summary>
	/// <remarks>Allows URL to directly link to images.</remarks>
	[JsonPropertyName("large_image"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string LargeImageKey
	{
		get => _largeimagekey;
		set
		{
			if (!BaseRichPresence.ValidateString(value, out _largeimagekey, 256, Encoding.UTF8))
				throw new StringOutOfRangeException(256);

			// Get if this is a external link
			IsLargeImageKeyExternal = _largeimagekey?.StartsWith("mp:external/") ?? false;
				
			// Reset the large image ID
			LargeImageID = null;
		}
	}
	private string _largeimagekey;

	/// <summary>
	/// Gets if the large square image is from an external link
	/// </summary>
	[JsonIgnore]
	public bool IsLargeImageKeyExternal { get; private set; }

	/// <summary>
	/// The tooltip for the large square image. For example, "Summoners Rift" or "Horizon Lunar Colony".
	/// <para>Max 128 Bytes.</para>
	/// </summary>
	[JsonPropertyName("large_text"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string LargeImageText
	{
		get => _largeimagetext;
		set
		{
			if (!BaseRichPresence.ValidateString(value, out _largeimagetext, 128, Encoding.UTF8))
				throw new StringOutOfRangeException(128);
		}
	}
	private string _largeimagetext;


	/// <summary>
	/// Name of the uploaded image for the small profile artwork.
	/// <para>Max 256 Bytes.</para>
	/// </summary>
	/// <remarks>Allows URL to directly link to images.</remarks>
	[JsonPropertyName("small_image"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string SmallImageKey
	{
		get => _smallimagekey;
		set
		{
			if (!BaseRichPresence.ValidateString(value, out _smallimagekey, 256, Encoding.UTF8))
				throw new StringOutOfRangeException(256);

			//Get if this is a external link
			IsSmallImageKeyExternal = _smallimagekey?.StartsWith("mp:external/") ?? false;

			//Reset the small image id
			SmallImageID = null;
		}
	}
	private string _smallimagekey;

	/// <summary>
	/// Gets if the small profile artwork is from an external link
	/// </summary>
	[JsonIgnore]
	public bool IsSmallImageKeyExternal { get; private set; }

	/// <summary>
	/// The tooltip for the small circle image. For example, "LvL 6" or "Ultimate 85%".
	/// <para>Max 128 Bytes.</para>
	/// </summary>
	[JsonPropertyName("small_text"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string SmallImageText
	{
		get => _smallimagetext;
		set
		{
			if (!BaseRichPresence.ValidateString(value, out _smallimagetext, 128, Encoding.UTF8))
				throw new StringOutOfRangeException(128);
		}
	}
	private string _smallimagetext;

	/// <summary>
	/// The ID of the large image. This is only set after Update Presence and will automatically become null when <see cref="LargeImageKey"/> is changed.
	/// </summary>
	[JsonIgnore]
	public ulong? LargeImageID { get; private set; }

	/// <summary>
	/// The ID of the small image. This is only set after Update Presence and will automatically become null when <see cref="SmallImageKey"/> is changed.
	/// </summary>
	[JsonIgnore]
	public ulong? SmallImageID { get; private set; }

	/// <summary>
	/// Merges this asset with the other, taking into account for ID's instead of keys.
	/// </summary>
	/// <param name="other"></param>
	internal void Merge(Assets other)
	{
		// Copy over the names
		_smallimagetext = other._smallimagetext;
		_largeimagetext = other._largeimagetext;

		// Convert large ID
		if (ulong.TryParse(other._largeimagekey, out var largeID))
		{
			LargeImageID = largeID;
		}
		else
		{
			_largeimagekey = other._largeimagekey;
			LargeImageID = null;
		}

		// Convert the small ID
		if (ulong.TryParse(other._smallimagekey, out var smallID))
		{
			SmallImageID = smallID;
		}
		else
		{
			_smallimagekey = other._smallimagekey;
			SmallImageID = null;
		}
	}
}

/// <summary>
/// Structure representing the start and endtimes of a match.
/// </summary>
public class Timestamps
{
	/// <summary>A new timestamp that starts from the current time.</summary>
	public static Timestamps Now => new(DateTime.UtcNow);

	/// <summary>
	/// Creates a new timestamp starting at the current time and ending in the supplied timespan
	/// </summary>
	/// <param name="seconds">How long the Timestamp will last for in seconds.</param>
	/// <returns>Returns a new timestamp with given duration.</returns>
	public static Timestamps FromTimeSpan(double seconds) { return FromTimeSpan(TimeSpan.FromSeconds(seconds)); }

	/// <summary>
	/// Creates a new timestamp starting at current time and ending in the supplied timespan
	/// </summary>
	/// <param name="timespan">How long the Timestamp will last for.</param>
	/// <returns>Returns a new timestamp with given duration.</returns>
	public static Timestamps FromTimeSpan(TimeSpan timespan)
	{
		return new Timestamps()
		{
			Start = DateTime.UtcNow,
			End = DateTime.UtcNow + timespan
		};
	}

	/// <summary>
	/// The time that match started. When included (not-null), the time in the rich presence will be shown as "00:01 elapsed".
	/// </summary>
	[JsonIgnore]
	public DateTime? Start { get; set; }

	/// <summary>
	/// The time the match will end. When included (not-null), the time in the rich presence will be shown as "00:01 remaining". This will override the "elapsed" to "remaining".
	/// </summary>
	[JsonIgnore]
	public DateTime? End { get; set; }

	/// <summary>
	/// Creates a empty timestamp object
	/// </summary>
	public Timestamps()
	{
		Start = null;
		End = null;
	}

	/// <summary>
	/// Creates a timestamp with the set start time
	/// </summary>
	/// <param name="start"></param>
	public Timestamps(DateTime start)
	{
		Start = start;
		End = null;
	}

	/// <summary>
	/// Creates a timestamp with a set duration
	/// </summary>
	/// <param name="start">The start time</param>
	/// <param name="end">The end time</param>
	public Timestamps(DateTime start, DateTime end)
	{
		Start = start;
		End = end;
	}

	/// <summary>
	/// Converts between DateTime and Milliseconds to give the Unix Epoch Time for the <see cref="Timestamps.Start"/>.
	/// </summary>
	[JsonPropertyName("start"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ulong? StartUnixMilliseconds
	{
		get => Start.HasValue ? ToUnixMilliseconds(Start.Value) : null;
		set => Start = value.HasValue ? FromUnixMilliseconds(value.Value) : null;
	}

	/// <summary>
	/// Converts between DateTime and Milliseconds to give the Unix Epoch Time  for the <see cref="Timestamps.End"/>.
	/// <seealso cref="End"/>
	/// </summary>
	[JsonPropertyName("end"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ulong? EndUnixMilliseconds
	{
		get => End.HasValue ? ToUnixMilliseconds(End.Value) : null;
		set => End = value.HasValue ? FromUnixMilliseconds(value.Value) : null;
	}

	/// <summary>
	/// Converts a Unix Epoch time into a <see cref="DateTime"/>.
	/// </summary>
	/// <param name="unixTime">The time in milliseconds since 1970 / 01 / 01</param>
	/// <returns></returns>
	public static DateTime FromUnixMilliseconds(ulong unixTime)
	{
		var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		return epoch.AddMilliseconds(Convert.ToDouble(unixTime));
	}

	/// <summary>
	/// Converts a <see cref="DateTime"/> into a Unix Epoch time (in milliseconds).
	/// </summary>
	/// <param name="date">The datetime to convert</param>
	/// <returns></returns>
	public static ulong ToUnixMilliseconds(DateTime date)
	{
		var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		return Convert.ToUInt64((date - epoch).TotalMilliseconds);
	}
}

/// <summary>
/// Structure representing the part the player is in.
/// </summary>
public class Party
{
	/// <summary>
	/// Privacy of the party
	/// </summary>
	public enum PrivacySetting
	{
		/// <summary>
		/// The party is private, invites only.
		/// </summary>
		Private = 0,

		/// <summary>
		/// The party is public, anyone can join.
		/// </summary>
		Public = 1
	}

	/// <summary>
	/// A unique ID for the player's current party / lobby / group. If this is not supplied, they player will not be in a party and the rest of the information will not be sent. 
	/// <para>Max 128 Bytes</para>
	/// </summary>
	[JsonPropertyName("id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string ID
	{
		get => _partyid;
		set => _partyid = value.GetNullOrString();
	}
	private string _partyid;

	/// <summary>
	/// The current size of the players party / lobby / group.
	/// </summary>
	[JsonIgnore]
	public int Size { get; set; }

	/// <summary>
	/// The maxium size of the party / lobby / group. This is required to be larger than <see cref="Size"/>. If it is smaller than the current party size, it will automatically be set too <see cref="Size"/> when the presence is sent.
	/// </summary>
	[JsonIgnore]
	public int Max { get; set; }

	/// <summary>
	/// The privacy of the party
	/// </summary>
	[JsonPropertyName("privacy")]
	public PrivacySetting Privacy { get; set; }

	[JsonPropertyName("size"), JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#pragma warning disable IDE1006
	internal int[] _size
#pragma warning restore IDE1006
	{
		get
		{
			// see issue https://github.com/discordapp/discord-rpc/issues/111
			var size = Math.Max(1, Size);
			return [size, Math.Max(size, Max)];
		}

		set
		{
			if (value.Length != 2)
			{
				Size = 0;
				Max = 0;
			}
			else
			{
				Size = value[0];
				Max = value[1];
			}
		}
	}
}

/// <summary>
/// A Rich Presence button.
/// </summary>
public class Button
{
	/// <summary>
	/// Text shown on the button
	/// <para>Max 32 bytes.</para>
	/// </summary>
	[JsonPropertyName("label")]
	public string Label
	{
		get => _label;
		set
		{
			if (!BaseRichPresence.ValidateString(value, out _label, 32, Encoding.UTF8))
				throw new StringOutOfRangeException(32);
		}
	}
	private string _label;

	/// <summary>
	/// The URL opened when clicking the button.
	/// <para>Max 512 bytes.</para>
	/// </summary>
	[JsonPropertyName("url")]
	public string Url
	{
		get => _url;
		set
		{
			if (!BaseRichPresence.ValidateString(value, out _url, 512, Encoding.UTF8))
				throw new StringOutOfRangeException(512);

			if (!Uri.TryCreate(_url, UriKind.Absolute, out _))
				throw new ArgumentException("Url must be a valid URI");
		}
	}
	private string _url;
}

/// <summary>
/// Rich Presence activity type
/// </summary>
public enum ActivityType
{
	// Streaming (1) and Custom (4) are not supported via RPC
	/// <summary>
	/// Playing status type. Displays as "Playing ..."
	/// </summary>
	Playing = 0,
	/// <summary>
	/// Listening status type. Displays as "Listening to ..."
	/// </summary>
	Listening = 2,
	/// <summary>
	/// Watching status type. Displays as "Watching ..."
	/// </summary>
	Watching = 3,
	/// <summary>
	/// Competing status type. Displays as "Competing in ..."
	/// </summary>
	Competing = 5
}

/// <summary>
/// The Rich Presence structure that will be sent and received by Discord. Use this class to build your presence and update it appropriately.
/// </summary>
// This is broken up in this way because the response inherits the BaseRichPresence.
public sealed class RichPresence : BaseRichPresence
{
	/// <summary>
	/// The buttons to display in the presence. 
	/// <para>Max of 2</para>
	/// </summary>
	[JsonPropertyName("buttons"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Button[] Buttons { get; set; }

	/// <summary>
	/// Does the Rich Presence have any buttons?
	/// </summary>
	/// <returns></returns>
	public bool HasButtons()
	{
		return Buttons is { Length: > 0 };
	}

	#region Builder

	/// <summary>
	/// Sets the state of the Rich Presence. See also <seealso cref="BaseRichPresence.State"/>.
	/// </summary>
	/// <param name="state">The user's current <see cref="Party"/> status.</param>
	/// <returns>The modified Rich Presence.</returns>
	public RichPresence WithState(string state)
	{
		State = state;
		return this;
	}

	/// <summary>
	/// Sets the details of the Rich Presence. See also <seealso cref="BaseRichPresence.Details"/>.
	/// </summary>
	/// <param name="details">What the user is currently doing.</param>
	/// <returns>The modified Rich Presence.</returns>
	public RichPresence WithDetails(string details)
	{
		Details = details;
		return this;
	}

	/// <summary>
	/// Sets the type of the Rich Presence. See also <seealso cref="ActivityType"/>.
	/// </summary>
	/// <param name="type">The status type</param>
	/// <returns>The modified Rich Presence.</returns>
	public RichPresence WithType(ActivityType type)
	{
		Type = type;
		return this;
	}

	/// <summary>
	/// Sets the timestamp of the Rich Presence. See also <seealso cref="Timestamps"/>.
	/// </summary>
	/// <param name="timestamps">The time elapsed / remaining time data.</param>
	/// <returns>The modified Rich Presence.</returns>
	public RichPresence WithTimestamps(Timestamps timestamps)
	{
		Timestamps = timestamps;
		return this;
	}

	/// <summary>
	/// Sets the assets of the Rich Presence. See also <seealso cref="Assets"/>.
	/// </summary>
	/// <param name="assets">The names of the images to use and the tooltips to give those images.</param>
	/// <returns>The modified Rich Presence.</returns>
	public RichPresence WithAssets(Assets assets)
	{
		Assets = assets;
		return this;
	}

	/// <summary>
	/// Sets the Rich Presence's party. See also <seealso cref="Party"/>.
	/// </summary>
	/// <param name="party">The party the player is currently in.</param>
	/// <returns>The modified Rich Presence.</returns>
	public RichPresence WithParty(Party party)
	{
		Party = party;
		return this;
	}

	/// <summary>
	/// Sets the Rich Presence's secrets. See also <seealso cref="Secrets"/>.
	/// </summary>
	/// <param name="secrets">The secrets used for Join / Spectate.</param>
	/// <returns>The modified Rich Presence.</returns>
	public RichPresence WithSecrets(Secrets secrets)
	{
		Secrets = secrets;
		return this;
	}
	#endregion

	#region Cloning and Merging

	/// <summary>
	/// Clones the presence into a new instance. Used for thread safe writing and reading. This function will ignore properties if they are in a invalid state.
	/// </summary>
	/// <returns></returns>
	public RichPresence Clone()
	{
		return new RichPresence
		{
			State = _state?.Clone() as string,
			Details = _details?.Clone() as string,
			Type = Type,

			Buttons = !HasButtons() ? null : Buttons.Clone() as Button[],
			Secrets = !HasSecrets() ? null : new Secrets
			{
				JoinSecret = Secrets.JoinSecret?.Clone() as string,
				SpectateSecret = Secrets.SpectateSecret?.Clone() as string
			},

			Timestamps = !HasTimestamps() ? null : new Timestamps
			{
				Start = Timestamps.Start,
				End = Timestamps.End
			},

			Assets = !HasAssets() ? null : new Assets
			{
				LargeImageKey = Assets.LargeImageKey?.Clone() as string,
				LargeImageText = Assets.LargeImageText?.Clone() as string,
				SmallImageKey = Assets.SmallImageKey?.Clone() as string,
				SmallImageText = Assets.SmallImageText?.Clone() as string
			},

			Party = !HasParty() ? null : new Party
			{
				ID = Party.ID,
				Size = Party.Size,
				Max = Party.Max,
				Privacy = Party.Privacy,
			},
		};
	}

	/// <summary>
	/// Merges the passed presence with this one, taking into account the image key to image id annoyance.
	/// </summary>
	/// <param name="presence"></param>
	/// <returns>self</returns>
	internal RichPresence Merge(BaseRichPresence presence)
	{
		_state = presence.State;
		_details = presence.Details;
		Type = presence.Type;
		Party = presence.Party;
		Timestamps = presence.Timestamps;
		Secrets = presence.Secrets;

		// If they have assets, we should merge them
		if (presence.HasAssets())
		{
			// Make sure we actually have assets too
			if (!HasAssets())
			{
				// We dont, so we will just use theirs
				Assets = presence.Assets;
			}
			else
			{
				// We do, so we better merge them!
				Assets.Merge(presence.Assets);
			}
		}
		else
		{
			// They dont have assets, so we will just set ours to null
			Assets = null;
		}

		return this;
	}

	internal override bool Matches(RichPresence other)
	{
		if (!base.Matches(other)) return false;

		// Check buttons
		if (Buttons == null ^ other.Buttons == null) return false;
		if (Buttons != null)
		{
			if (Buttons.Length != other.Buttons!.Length) return false;
			for (var i = 0; i < Buttons.Length; i++)
			{
				var a = Buttons[i];
				var b = other.Buttons[i];
				if (a.Label != b.Label || a.Url != b.Url)
					return false;
			}
		}

		return true;
	}

	#endregion

	/// <summary>
	/// Operator that converts a presence into a boolean for null checks.
	/// </summary>
	/// <param name="presence"></param>
	public static implicit operator bool(RichPresence presence)
	{
		return presence != null;
	}
}

/// <summary>
/// A rich presence that has been parsed from the pipe as a response.
/// </summary>
internal sealed class RichPresenceResponse : BaseRichPresence
{
	/// <summary>
	/// ID of the client
	/// </summary>
	[JsonPropertyName("application_id"), JsonInclude]
	public string ClientID { get; internal set; }

	/// <summary>
	/// Name of the bot
	/// </summary>
	[JsonPropertyName("name"), JsonInclude]
	public string Name { get; internal set; }
}
