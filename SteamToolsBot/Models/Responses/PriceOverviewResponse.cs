using System.Text.Json.Serialization;
using SteamToolsBot.Helpers;

namespace SteamToolsBot.Models.Responses;

public record PriceOverviewResponse : BooleanResponse
{
	[JsonConverter(typeof(DecimalSeparatorJsonConverter))]
	[JsonPropertyName("volume")]
	public uint Volume { get; init; }
}
