using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.Caching.Memory;
using SteamToolsBot.Enums;
using SteamToolsBot.Exceptions;
using SteamToolsBot.Models;
using SteamToolsBot.Models.Responses;
using File = System.IO.File;

namespace SteamToolsBot.Commands;

public partial class CardPriceCommand : ICommand
{
	private readonly AsyncPolicy<string> executePolicy;
	private readonly Regex itemIDRegex = ItemIDRegex();
	private readonly AsyncPolicy<uint?> marketVolumePolicy;
	private readonly AsyncPolicy<OrderRecord> pricePolicy;

	private IReadOnlyDictionary<uint, string> cards = null!;
	private IList<ECurrencyCode> currencyCodes = null!;
	private Func<IFlurlClient> httpClientFactory = null!;

	private uint saleAppID;

	public CardPriceCommand()
	{
		executePolicy = Policy.CacheAsync<string>(
#pragma warning disable CA2000
			new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions())),
#pragma warning restore CA2000
			TimeSpan.FromMinutes(5)
		);

		marketVolumePolicy = Policy.WrapAsync(
			Policy<uint?>
				.Handle<HttpRequestException>(e => e.StatusCode == HttpStatusCode.InternalServerError)
				.FallbackAsync((uint?) null),
			Policy<uint?>
				.Handle<RequestFailedException>()
				.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(attempt)),
			Policy<uint?>
				.Handle<FlurlHttpTimeoutException>()
				.RetryAsync(5)
		);

		pricePolicy = Policy.WrapAsync(
			Policy<OrderRecord>
				.Handle<RequestFailedException>()
				.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(attempt)),
			Policy<OrderRecord>
				.Handle<FlurlHttpTimeoutException>()
				.RetryAsync(5)
		);
	}

	public string Command => "cardprice";
	public string EnglishDescription => "Get prices and volumes of the sale cards";
	public string RussianDescription => "Получить стоимость и объём торгов распродажных карт";

	public async Task<string> Execute()
	{
		if (saleAppID == 0)
		{
			return "There is no sale right now!";
		}

		return await executePolicy.ExecuteAsync(_ => GetResponse(), new Context(Command));
	}

	public async Task Initialize(BotConfiguration config, IFlurlClient farmClient, Func<IFlurlClient> steamClientFactory)
	{
		ArgumentNullException.ThrowIfNull(config);

		currencyCodes = config.Currencies;
		saleAppID = config.SaleAppID;
		httpClientFactory = steamClientFactory;
		if (saleAppID == 0)
		{
			return;
		}

		var cacheFileName = Path.Join("cache", saleAppID + ".json");
		if (File.Exists(cacheFileName))
		{
			await using var cardsFile = File.OpenRead(cacheFileName);
			var cachedCards = await JsonSerializer.DeserializeAsync<Dictionary<uint, string>>(cardsFile);
			if (cachedCards != null)
			{
				cards = cachedCards;
				return;
			}
		}

		using var client = httpClientFactory();
		var marketCards = await client
			.Request("market", "search", "render")
			.SetQueryParams(
				new Dictionary<string, object>(7)
				{
					{"count", 100},
					{"norender", "1"},
					{"sort_column", "price"},
					{"sord_dir", "desc"},
					{"appid", 753},
					{"category_753_Game[]", "tag_app_" + config.SaleAppID},
					{"category_753_cardborder[]", "tag_cardborder_0"}
				}
			)
			.GetJsonAsync<SearchRenderResponse>();

		if (!marketCards.Success)
		{
			throw new RequestFailedException("Could not retrieve card information!");
		}

		var internalCards = new Dictionary<uint, string>(marketCards.Results.Count);
		foreach (var card in marketCards.Results.Where(x => !x.Name.Contains("Mystery", StringComparison.InvariantCulture)))
		{
			var itemMarketID = await GetItemMarketID(753, card.HashName);
			internalCards.Add(itemMarketID, card.Name);
			await Task.Delay(1000);
		}

		cards = internalCards;
		await using var cardsCache = File.OpenWrite(cacheFileName);
		await JsonSerializer.SerializeAsync(cardsCache, cards);
	}

	private async Task<uint> GetItemMarketID(uint appID, string marketHashName)
	{
		using var client = httpClientFactory();
		var response = await client
			.Request("market", "listings", appID, marketHashName)
			.GetStringAsync();

		return uint.Parse(itemIDRegex.Match(response).Groups[1].ValueSpan, CultureInfo.InvariantCulture);
	}

	private async Task<uint?> GetItemMarketVolume(string hashName)
	{
		using var client = httpClientFactory();
		var response = await client.Request("market", "priceoverview")
			.SetQueryParams(
				new
				{
					currency = 1,
					appid = 753,
					market_hash_name = hashName
				}
			)
			.GetJsonAsync<PriceOverviewResponse>();

		if (!response.Success)
		{
			throw new RequestFailedException();
		}

		return response.Volume;
	}

	private async Task<ItemPriceInfo> GetItemPriceInformation(uint appID, uint itemID, string name, IList<ECurrencyCode> currencies)
	{
		var tasks = currencies.Select(currency => pricePolicy.ExecuteAsync(() => GetPriceAndQuantityOfItem(itemID, currency)));

		var results = await Task.WhenAll(tasks);
		var hashName = $"{appID}-{name}";

		var volume = await marketVolumePolicy.ExecuteAsync(() => GetItemMarketVolume(hashName));

		return new ItemPriceInfo(name, results.Zip(currencies).ToDictionary(x => x.Second, x => x.First), volume);
	}

	private async Task<OrderRecord> GetPriceAndQuantityOfItem(uint itemID, ECurrencyCode currency)
	{
		using var client = httpClientFactory();
		var response = await client
			.Request("market", "itemordershistogram")
			.SetQueryParams(
				new
				{
					norender = 1,
					currency = (byte) currency,
					item_nameid = itemID,
					language = "english"
				}
			).GetJsonAsync<ItemOrdersHistogramResponse>();

		if (response.Success != EResult.OK)
			throw new RequestFailedException(response.Success);

		if (response.SellOrderTable.Count == 0)
			throw new RequestFailedException();

		return response.SellOrderTable.OrderBy(x => x.Price).First();
	}

	private async Task<string> GetResponse()
	{
		var tasksItemInfo = cards.Select(card => GetItemPriceInformation(saleAppID, card.Key, card.Value, currencyCodes));
		var itemsInfo = await Task.WhenAll(tasksItemInfo);
		var response = string.Join("\n", itemsInfo.Select(itemInfo => itemInfo.ToString()).OrderBy(x => x));

		return response;
	}

    [GeneratedRegex(@"Market_LoadOrderSpread\(\s*(\d+)\s*\)", RegexOptions.CultureInvariant)]
    private static partial Regex ItemIDRegex();
}
