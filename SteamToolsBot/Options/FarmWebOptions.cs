using FluentValidation;
using SteamToolsBot.Helpers;

namespace SteamToolsBot.Options;

internal sealed record FarmWebOptions
{
	public string? IPCPassword { get; init; }

	public required string IPCAddress { get; init; }
}

internal sealed class FarmWebOptionsValidator : AbstractValidator<FarmWebOptions>
{
	public FarmWebOptionsValidator()
	{
		RuleFor(op => op.IPCAddress).Url();
	}
}
