using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using SteamToolsBot.Enums;

namespace SteamToolsBot.Models;

internal record ItemPriceInfo(uint AppID, string Name, IReadOnlyDictionary<ECurrencyCode, OrderRecord> PricesPerCurrency, uint? Volume)
{
	private const string steamCommunityMarketURL = "https://steamcommunity.com/market/listings/753/";

	private static string CurrencyNumberToString(ECurrencyCode currency)
	{
		return currency switch
		{
			ECurrencyCode.USD => "$",
			ECurrencyCode.EUR => "€",
			ECurrencyCode.RUB => "₽",
			_ => throw new ArgumentOutOfRangeException(nameof(currency))
		};
	}

	public override string ToString()
	{
		return
			$"*[{Name}]({steamCommunityMarketURL}/{AppID}-{WebUtility.UrlEncode(Name)})*\n" +
			string.Join(
				"", PricesPerCurrency.Select(
					priceCurrency => $"\t{priceCurrency.Value.Quantity} sell orders at {priceCurrency.Value.Price:F2}"
						+ CurrencyNumberToString(priceCurrency.Key) + "\n")) +
			$"\tVolume: {Volume?.ToString(CultureInfo.InvariantCulture) ?? "unknown"}\n";
	}
}
