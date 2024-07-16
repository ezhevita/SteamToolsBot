using System.Text.Json.Serialization;

namespace SteamToolsBot.Models.Responses;

internal sealed record MarketItem
{
	[JsonInclude]
	[JsonPropertyName("name")]
	public required string Name { get; init; }

	[JsonInclude]
	[JsonPropertyName("hash_name")]
	public required string HashName { get; init; }
}
