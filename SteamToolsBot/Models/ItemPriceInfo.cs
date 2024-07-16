using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SteamToolsBot.Enums;
using SteamToolsBot.Helpers;
using SteamToolsBot.Models.Responses;

namespace SteamToolsBot.Models;

internal sealed record ItemPriceInfo(
	uint AppID,
	string Name,
	IReadOnlyDictionary<ECurrencyCode, (IEnumerable<OrderRecord> Sell, IEnumerable<OrderRecord> Buy)> PricesPerCurrency,
	uint? Volume)
{
	private const string steamCommunityMarketURL = "https://steamcommunity.com/market/listings/753/";

	private static string CurrencyNumberToString(ECurrencyCode currency)
	{
		return currency switch
		{
			ECurrencyCode.USD => "\x24",
			ECurrencyCode.EUR => "\u20ac",
			ECurrencyCode.RUB => "\u20bd",
			ECurrencyCode.KZT => "\u20b8",
			_ => throw new ArgumentOutOfRangeException(nameof(currency))
		};
	}

	public override string ToString()
	{
		var sb = new StringBuilder();
		var html = new HtmlStringBuilderGenerator(sb);
		using (html.Link($"{steamCommunityMarketURL}/{AppID}-{Uri.EscapeDataString(Name)}"))
		{
			using (html.Bold())
			{
				sb.Append(Name);
			}
		}

		sb.AppendLine();
		foreach (var (currencyCode, orderRecords) in PricesPerCurrency)
		{
			var currencySymbol = CurrencyNumberToString(currencyCode);
			foreach (var buyOrderRecord in orderRecords.Buy)
			{
				sb.AppendLine(
					CultureInfo.InvariantCulture,
					$"\t{buyOrderRecord.Quantity} <b>buy</b> orders @ {buyOrderRecord.Price:F2}{currencySymbol}");
			}

			foreach (var sellOrderRecord in orderRecords.Sell)
			{
				sb.AppendLine(
					CultureInfo.InvariantCulture,
					$"\t{sellOrderRecord.Quantity} <b>sell</b> orders @ {sellOrderRecord.Price:F2}{currencySymbol}");
			}
		}

		if (Volume != null)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"\t<b>Volume</b>: {Volume.Value}");
		} else
		{
			sb.AppendLine("\tVolume: unknown");
		}

		return sb.ToString();
	}
}
