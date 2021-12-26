using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SteamToolsBot.Helpers;

public class CurrencyJsonConverter : JsonConverter<decimal>
{
	private readonly Regex currencyRegex = new(@"\$|[\u20A0-\u20CF]|(\s*pуб\.)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
	public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.TokenType switch
		{
			JsonTokenType.String => decimal.Parse(
				currencyRegex.Replace(
					reader.GetString()!.Replace(
						" or more", "", StringComparison.Ordinal
					), ""
				).Replace(',', '.'), CultureInfo.InvariantCulture
			),
			JsonTokenType.Number => reader.GetDecimal(),
			_ => throw new NotImplementedException()
		};
	}

	public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
	{
		throw new NotImplementedException();
	}
}
