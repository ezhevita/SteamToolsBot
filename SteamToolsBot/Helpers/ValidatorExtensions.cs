using System;
using System.Buffers;
using FluentValidation;

namespace SteamToolsBot.Helpers;

internal static class ValidatorExtensions
{
	private const long telegramMaxUserID = (1L << 52) - 1;

	private static readonly SearchValues<char> TelegramHashAllowedSymbols =
		SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-");

	public static IRuleBuilderOptions<T, string> TelegramBotToken<T>(this IRuleBuilder<T, string> ruleBuilder)
	{
		return ruleBuilder
			.NotEmpty()
			.Must(
				static (_, token, context) =>
				{
					var separator = token.IndexOf(':', StringComparison.Ordinal);
					if (separator == -1)
					{
						context.AddFailure("Token must have a ':' separator.");

						return false;
					}

					var isSuccess = true;
					var userIdSpan = token.AsSpan()[..separator];
					const string UserIdDescription = "Bot user ID (part of the token before ':' separator)";
					if (!long.TryParse(userIdSpan, out var userID))
					{
						context.AddFailure($"{UserIdDescription} must be a number.");
						isSuccess = false;
					} else
					{
						var failureReason = userID switch
						{
							< 0 => "negative",
							0 => "equal to zero",
							> telegramMaxUserID => "larger than the max user ID (52-bit)",
							_ => null
						};

						if (failureReason != null)
						{
							context.AddFailure(
								$"{UserIdDescription} is {failureReason}. It must be between 1 and {telegramMaxUserID} inclusively.");
							isSuccess = false;
						}
					}

					var hashSpan = token.AsSpan()[(separator + 1)..];
					const string HashDescription = "Hash (part of the token after ':' separator)";
					const int HashLength = 35;
					if (hashSpan.Length != HashLength)
					{
						context.AddFailure($"{HashDescription} must be exactly {HashLength} chars.");
						isSuccess = false;
					}

					var currentIndex = 0;
					while (true)
					{
						var invalidIndex = hashSpan[currentIndex..].IndexOfAnyExcept(TelegramHashAllowedSymbols);

						if (invalidIndex == -1)
							break;

						context.AddFailure(
							$"{HashDescription} has an invalid character at position {currentIndex + invalidIndex}. " +
							$"Only Latin alphanumerical characters ('A'-'Z', 'a'-'z', '0'-'9'), '-' and '_' are allowed.");
						isSuccess = false;
						currentIndex += invalidIndex + 1;
					}

					return isSuccess;
				});
	}

	public static IRuleBuilderOptions<T, string?> Url<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
		ruleBuilder.NotEmpty()
			.Must(static url => Uri.TryCreate(url, UriKind.Absolute, out _));
}
