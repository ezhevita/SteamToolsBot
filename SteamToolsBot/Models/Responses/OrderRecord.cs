using System.Text.Json.Serialization;
using SteamToolsBot.Helpers;

namespace SteamToolsBot.Models.Responses;

public sealed record OrderRecord
{
	[JsonConverter(typeof(CurrencyJsonConverter))]
	[JsonInclude]
	[JsonPropertyName("price")]
	public decimal Price { get; init; }

	[JsonConverter(typeof(DecimalSeparatorJsonConverter))]
	[JsonInclude]
	[JsonPropertyName("quantity")]
	public uint Quantity { get; init; }
}
