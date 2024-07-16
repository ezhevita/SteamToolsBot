using FluentValidation;

namespace SteamToolsBot.FarmWebOptionsValidator;

internal sealed record FiveDollarCommandOptions
{
	public required uint ItemAppID { get; init; }
	public required ulong ItemID { get; init; }
	public required string TargetBotName { get; init; }
}

internal sealed class FiveDollarCommandOptionsValidator : AbstractValidator<FiveDollarCommandOptions>
{
	public FiveDollarCommandOptionsValidator()
	{
		RuleFor(op => op.ItemAppID).GreaterThan(0U);
		RuleFor(op => op.ItemID).GreaterThan(0UL);
		RuleFor(op => op.TargetBotName).NotEmpty();
	}
}
