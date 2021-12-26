using System.Text.Json.Serialization;

namespace SteamToolsBot.Models.Responses;

public record BooleanResponse
{
	[JsonPropertyName("success")]
	public bool Success { get; init; }
}
