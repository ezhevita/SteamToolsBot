using FluentValidation;
using SteamToolsBot.Helpers;

namespace SteamToolsBot.Options;

public sealed record BotOptions
{
	public ushort CommandResponseCacheSeconds { get; init; }
	public ushort CooldownSeconds { get; init; }
	public required string RedisHostname { get; init; } = null!;
	public required string TelegramToken { get; init; }
}

internal sealed class BotOptionsValidator : AbstractValidator<BotOptions>
{
	public BotOptionsValidator()
	{
		RuleFor(op => op.RedisHostname).NotEmpty();
		RuleFor(op => op.TelegramToken)
			.TelegramBotToken();
	}
}
