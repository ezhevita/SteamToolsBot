using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SteamToolsBot.Helpers;

public class DecimalSeparatorJsonConverter : JsonConverter<uint>
{
	public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.TokenType switch
		{
			JsonTokenType.String => uint.Parse(
				reader.GetString()!.Replace(",", "", StringComparison.Ordinal), CultureInfo.InvariantCulture),
			JsonTokenType.Number => reader.GetUInt32(),
			_ => throw new NotImplementedException()
		};
	}

	public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options)
	{
		throw new NotImplementedException();
	}
}
