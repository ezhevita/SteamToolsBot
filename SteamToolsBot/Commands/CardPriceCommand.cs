using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using SteamToolsBot.Exceptions;
using SteamToolsBot.Models;
using SteamToolsBot.Options;
using SteamToolsBot.Services;
using File = System.IO.File;

namespace SteamToolsBot.Commands;

internal sealed class CardPriceCommand : ICommand, IDisposable
{
	private readonly IOptionsMonitor<CardPriceCommandOptions> _commandOptions;
	private readonly AsyncPolicy<uint?> _marketVolumePolicy;
	private readonly AsyncPolicy _policy;
	private readonly IServiceProvider _serviceProvider;
	private readonly SemaphoreSlim _updateSemaphore = new(1, 1);

	private IReadOnlyDictionary<uint, string>? _cards;

	public CardPriceCommand(IOptionsMonitor<CardPriceCommandOptions> commandOptions, IServiceProvider serviceProvider)
	{
		_commandOptions = commandOptions;
		_serviceProvider = serviceProvider;

		_policy = Policy
			.Handle<RequestFailedException>()
			.WaitAndRetryAsync(3, static attempt => TimeSpan.FromSeconds(attempt));

		_marketVolumePolicy = Policy<uint?>
			.Handle<HttpRequestException>(static e => e.StatusCode == HttpStatusCode.InternalServerError)
			.FallbackAsync((uint?) null)
			.WrapAsync(_policy);
	}

	public string Command => "cardprice";
	public string EnglishDescription => "Get prices and volumes of the sale cards";
	public string RussianDescription => "Получить стоимость и объём торгов распродажных карт";

	public async Task<string> Execute(CancellationToken cancellationToken)
	{
		if (_commandOptions.CurrentValue.SaleAppID == 0)
		{
			_cards = null;

			return "There is no sale right now!";
		}

		if (_cards == null)
		{
			await _updateSemaphore.WaitAsync(cancellationToken);
			try
			{
				_cards = await _policy.ExecuteAsync(LoadSaleCards, cancellationToken);
			} finally
			{
				_updateSemaphore.Release();
			}
		}

		var tasksItemInfo = _cards.Select(card => GetItemPriceInformation(card.Key, card.Value, cancellationToken));
		var itemsInfo = await Task.WhenAll(tasksItemInfo);
		var response = string.Join('\n', itemsInfo.Select(static itemInfo => itemInfo.ToString()).OrderBy(x => x));

		return response;
	}

	public void Dispose()
	{
		_updateSemaphore.Dispose();
	}

	private MarketWebClient CreateClient() => _serviceProvider.GetRequiredService<MarketWebClient>();

	private async Task<ItemPriceInfo> GetItemPriceInformation(uint itemID, string name, CancellationToken cancellationToken)
	{
		var appID = _commandOptions.CurrentValue.SaleAppID;
		var currencies = _commandOptions.CurrentValue.Currencies;
		var tasks = currencies.Select(
			currency => _policy.ExecuteAsync(
				token => CreateClient().GetNLowestOrders(itemID, currency, 2, token), cancellationToken));

		var results = await Task.WhenAll(tasks);
		var hashName = $"{appID}-{name}";

		var volume = await _marketVolumePolicy.ExecuteAsync(
			token => CreateClient().GetItemMarketVolume(hashName, token), cancellationToken);

		return new ItemPriceInfo(
			appID, name, results.Zip(currencies).ToDictionary(static x => x.Second, static x => x.First), volume);
	}

	private async Task<IReadOnlyDictionary<uint, string>> LoadSaleCards(CancellationToken cancellationToken)
	{
		var saleAppID = _commandOptions.CurrentValue.SaleAppID;
		var cacheFileName = Path.Join("cache", saleAppID + ".json");
		if (File.Exists(cacheFileName))
		{
			await using var cardsFile = File.OpenRead(cacheFileName);
			var cachedCards = await JsonSerializer.DeserializeAsync<Dictionary<uint, string>>(
				cardsFile, cancellationToken: cancellationToken);

			if (cachedCards != null)
			{
				return cachedCards;
			}
		}

		var marketCards = await CreateClient().GetCardsOnMarketForAppID(saleAppID, cancellationToken);

		var internalCards = new Dictionary<uint, string>(marketCards.Count);
		var cardsToScan = marketCards.Any(static card => !card.Name.Contains("Mystery", StringComparison.InvariantCulture))
			? marketCards.Where(static x => !x.Name.Contains("Mystery", StringComparison.InvariantCulture))
			: marketCards;

		foreach (var card in cardsToScan)
		{
			var itemMarketID = await CreateClient().GetItemMarketID(753, card.HashName, cancellationToken);
			internalCards.Add(itemMarketID, card.Name);
			await Task.Delay(1000, cancellationToken);
		}

		await using var cardsCache = File.OpenWrite(cacheFileName);
		await JsonSerializer.SerializeAsync(cardsCache, internalCards, cancellationToken: cancellationToken);

		return internalCards;
	}
}
