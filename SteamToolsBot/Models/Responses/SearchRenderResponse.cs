using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SteamToolsBot.Models.Responses;

internal sealed record SearchRenderResponse : BooleanResponse
{
	[JsonInclude]
	[JsonPropertyName("results")]
	public IReadOnlyCollection<MarketItem>? Results { get; init; }
}
