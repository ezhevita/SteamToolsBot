using System.Text.Json.Serialization;
using SteamToolsBot.Enums;

namespace SteamToolsBot.Models.Responses;

internal record ResultResponse
{
	[JsonInclude]
	[JsonPropertyName("success")]
	public required EResult Success { get; init; }
}
