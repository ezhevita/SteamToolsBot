using System.Text.Json.Serialization;
using SteamToolsBot.Enums;

namespace SteamToolsBot.Models.Responses;

public record ResultResponse
{
	[JsonPropertyName("success")]
	public EResult Success { get; init; }
}
