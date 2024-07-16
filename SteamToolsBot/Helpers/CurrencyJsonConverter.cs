using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SteamToolsBot.Helpers;

internal sealed partial class CurrencyJsonConverter : JsonConverter<decimal>
{
	[GeneratedRegex(@"\$|[\u20A0-\u20CF]|(\s*pуб\.)", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
	private static partial Regex CurrencyRegex();

	public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.TokenType switch
		{
			JsonTokenType.String => decimal.Parse(
				CurrencyRegex().Replace(reader.GetString()!.Replace(" or more", "", StringComparison.Ordinal), "")
					.Replace(',', '.'), CultureInfo.InvariantCulture),
			JsonTokenType.Number => reader.GetDecimal(),
			_ => throw new NotImplementedException()
		};
	}

	public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
	{
		throw new NotImplementedException();
	}
}
