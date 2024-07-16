using System;
using System.Collections.Generic;
using System.Net;
using FluentValidation;
using SteamToolsBot.Enums;
using SteamToolsBot.Helpers;

namespace SteamToolsBot.Options;

public sealed record CardPriceCommandOptions
{
	public required IReadOnlyCollection<ECurrencyCode> Currencies { get; init; }

	public required uint SaleAppID { get; init; }

	public string? SteamProxy { get; init; }

	public IWebProxy? SteamParsedProxy
	{
		get
		{
			if (SteamProxy != null)
			{
				var proxyUri = new Uri(SteamProxy);

				NetworkCredential? credentials = null;
				if (!string.IsNullOrEmpty(proxyUri.UserInfo))
				{
					credentials = proxyUri.UserInfo.Split(':') switch
					{
						[var user, var password] => new NetworkCredential(user, password),
						_ => throw new InvalidOperationException(nameof(proxyUri))
					};
				}

				return new WebProxy(proxyUri, true, null, credentials);
			}

			return null;
		}
	}
}

internal sealed class CardPriceCommandOptionsValidator : AbstractValidator<CardPriceCommandOptions>
{
	public CardPriceCommandOptionsValidator()
	{
		RuleFor(op => op.Currencies).NotEmpty();
		RuleForEach(op => op.Currencies).IsInEnum();
		RuleFor(op => op.SaleAppID).NotEqual(0U);
		RuleFor(op => op.SteamProxy)
			.Url()
			.When(static op => op.SteamProxy != null);
	}
}
