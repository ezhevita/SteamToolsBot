using SteamToolsBot.Enums;

namespace SteamToolsBot;

public class BotConfiguration
{
	public string BotName { get; init; } = null!;

	public ECurrencyCode[] Currencies { get; init; } = null!;

	public ulong FiveDollarItemID { get; init; }

	public uint FiveDollarItemAppID { get; init; }

	public string IPCPassword { get; init; } = null!;

	public string IPCAddress { get; init; } = null!;

	public uint SaleAppID { get; init; }

	public long TelegramOwnerID { get; init; }

	public string TelegramToken { get; init; } = null!;

	public ushort CooldownSeconds { get; init; }

	public string? SteamProxy { get; init; }
}
