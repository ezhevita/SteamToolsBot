using System.Text.Json.Serialization;

namespace SteamToolsBot.Models;

public record MarketItem
{
	[JsonPropertyName("name")]
	public string Name { get; init; } = null!;

	[JsonPropertyName("hash_name")]
	public string HashName { get; init; } = null!;
}
