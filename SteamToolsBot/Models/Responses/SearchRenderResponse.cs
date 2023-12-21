using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SteamToolsBot.Models.Responses;

public record SearchRenderResponse : BooleanResponse
{
	[JsonPropertyName("results")]
	public IList<MarketItem> Results { get; init; } = null!;
}
