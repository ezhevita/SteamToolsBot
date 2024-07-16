using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SteamToolsBot.Enums;
using SteamToolsBot.Exceptions;
using SteamToolsBot.Models.Responses;

namespace SteamToolsBot.Services;

internal sealed partial class MarketWebClient
{
	private readonly HttpClient _httpClient;

	public MarketWebClient(HttpClient httpClient) => _httpClient = httpClient;

	public async Task<IReadOnlyCollection<MarketItem>> GetCardsOnMarketForAppID(uint appID, CancellationToken cancellationToken)
	{
		var marketCards = await _httpClient
			.GetFromJsonAsync<SearchRenderResponse>(
				new Uri(
					$"search/render?count=100&norender=1&sort_column=price&sort_dir=desc&appid=753&" +
					$"category_753_Game[]=tag_app_{appID}&category_753_cardborder[]=tag_cardborder_0",
					UriKind.Relative),
				cancellationToken);

		if (marketCards is not {Success: true, Results: not null})
			throw new RequestFailedException();

		return marketCards.Results;
	}

	public async Task<uint> GetItemMarketID(uint appID, string marketHashName, CancellationToken cancellationToken)
	{
		var response = await _httpClient
			.GetStringAsync(
				new Uri($"listings/{appID}/{Uri.EscapeDataString(marketHashName)}", UriKind.Relative), cancellationToken);

		return uint.Parse(ItemIDRegex().Match(response).Groups[1].ValueSpan, CultureInfo.InvariantCulture);
	}

	public async Task<uint?> GetItemMarketVolume(string hashName, CancellationToken cancellationToken)
	{
		var response = await _httpClient.GetFromJsonAsync<PriceOverviewResponse>(
			new Uri(
				$"priceoverview?currency=1&appid=753&market_hash_name={Uri.EscapeDataString(hashName)}",
				UriKind.Relative), cancellationToken);

		if (response is not {Success: true})
			throw new RequestFailedException();

		return response.Volume;
	}

	public async Task<(IEnumerable<OrderRecord> Sell, IEnumerable<OrderRecord> Buy)> GetNLowestOrders(uint itemID,
		ECurrencyCode currency, int n,
		CancellationToken cancellationToken)
	{
		var response = await _httpClient
			.GetFromJsonAsync<ItemOrdersHistogramResponse>(
				new Uri(
					$"itemordershistogram?norender=1&currency={(byte) currency}&item_nameid={itemID}&language=english",
					UriKind.Relative), cancellationToken);

		if (response == null)
			throw new RequestFailedException();

		if (response.Success != EResult.OK)
			throw new RequestFailedException(response.Success);

		if (response.SellOrderTable == null)
			throw new RequestFailedException();

		if (response.SellOrderTable.Count == 0)
			throw new RequestFailedException();

		return (response.SellOrderTable.OrderBy(static x => x.Price).Take(n),
			response.BuyOrderTable?.OrderBy(static x => x.Price).Take(n) ?? []);
	}

	[GeneratedRegex(@"Market_LoadOrderSpread\(\s*(\d+)\s*\)", RegexOptions.CultureInvariant)]
	private static partial Regex ItemIDRegex();
}
