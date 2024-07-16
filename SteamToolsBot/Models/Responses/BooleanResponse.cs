using System.Text.Json.Serialization;

namespace SteamToolsBot.Models.Responses;

public record BooleanResponse
{
	[JsonInclude]
	[JsonPropertyName("success")]
	public required bool Success { get; init; }
}
