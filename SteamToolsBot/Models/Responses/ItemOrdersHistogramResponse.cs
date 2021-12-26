using System.Text.Json.Serialization;

namespace SteamToolsBot.Models.Responses;

public record ItemOrdersHistogramResponse : ResultResponse
{
	[JsonPropertyName("sell_order_table")]
	public OrderRecord[] SellOrderTable { get; init; } = null!;
}
