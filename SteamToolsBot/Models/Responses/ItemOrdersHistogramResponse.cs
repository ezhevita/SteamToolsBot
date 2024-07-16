using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SteamToolsBot.Models.Responses;

public sealed record ItemOrdersHistogramResponse : ResultResponse
{
	[JsonInclude]
	[JsonPropertyName("sell_order_table")]
	public IList<OrderRecord>? SellOrderTable { get; init; }

	[JsonInclude]
	[JsonPropertyName("buy_order_table")]
	public IList<OrderRecord>? BuyOrderTable { get; init; }
}
