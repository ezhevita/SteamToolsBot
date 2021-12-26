using System.Text.Json.Serialization;

namespace SteamToolsBot.Models.Responses;

public record SearchRenderResponse : BooleanResponse
{
	[JsonPropertyName("results")]
	public MarketItem[] Results { get; init; } = null!;
}
