using System.Text.Json.Serialization;
using SteamToolsBot.Helpers;

namespace SteamToolsBot.Models;

public record OrderRecord
{
	[JsonConverter(typeof(CurrencyJsonConverter))]
	[JsonPropertyName("price")]
	public decimal Price { get; init; }

	[JsonConverter(typeof(DecimalSeparatorJsonConverter))]
	[JsonPropertyName("quantity")]
	public uint Quantity { get; init; }
}
