using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SteamToolsBot.Helpers;

internal sealed class CurrencyJsonConverter : JsonConverter<decimal>
{
	public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.TokenType switch
		{
			JsonTokenType.String => uint.Parse(
					new string(reader.GetString()!.Where(char.IsAsciiDigit).ToArray()), CultureInfo.InvariantCulture) /
				(decimal) 100.0,
			JsonTokenType.Number => reader.GetDecimal(),
			_ => throw new NotImplementedException()
		};
	}

	public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
	{
		throw new NotImplementedException();
	}
}
