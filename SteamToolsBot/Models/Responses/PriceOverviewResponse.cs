using System.Text.Json.Serialization;
using SteamToolsBot.Helpers;

namespace SteamToolsBot.Models.Responses;

public sealed record PriceOverviewResponse : BooleanResponse
{
	[JsonConverter(typeof(DecimalSeparatorJsonConverter))]
	[JsonInclude]
	[JsonPropertyName("volume")]
	public uint Volume { get; init; }
}
