using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.Caching.Memory;
using SteamToolsBot.Exceptions;

namespace SteamToolsBot.Commands;

public partial class FiveDollarCommand : ICommand
{
	private readonly AsyncPolicy<string> executePolicy;

	private BotConfiguration botConfiguration = null!;
	private IFlurlClient httpClient = null!;

	public FiveDollarCommand()
	{
		executePolicy = Policy.WrapAsync(
			Policy.CacheAsync<string>(
#pragma warning disable CA2000
				new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions())),
#pragma warning restore CA2000
				TimeSpan.FromMinutes(5)),
			Policy<string>.HandleResult(string.IsNullOrEmpty).WaitAndRetryAsync(
				3, attempt => TimeSpan.FromSeconds(attempt), (_, _, _) => RefreshAuth()));
	}

	public string Command => "5dollar";
	public string EnglishDescription => "Convert Steam $5 to Russian roubles";
	public string RussianDescription => "Конвертация $5 в Steam в рубли";

	public async Task<string> Execute()
	{
		var result = await executePolicy.ExecuteAsync(_ => GetPrice(), new Context(Command));

		if (string.IsNullOrEmpty(result))
			throw new RequestFailedException();

		return result;
	}

	public Task Initialize(BotConfiguration config, IFlurlClient farmClient, Func<IFlurlClient> steamClientFactory)
	{
		httpClient = farmClient;
		botConfiguration = config;

		return Task.CompletedTask;
	}

	private async Task<string> GetPrice()
	{
		var response = await httpClient.Request(
			"Api", "Web", botConfiguration.BotName, "https://store.steampowered.com", "buyitem",
			botConfiguration.FiveDollarItemAppID, botConfiguration.FiveDollarItemID, 1).GetStringAsync();

		return PriceRegex().Match(response).Groups[1].Value;
	}

	[GeneratedRegex(
		"<div id=\"review_total_value\" class=\"price\">(.+?)</div>", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
	private static partial Regex PriceRegex();

	private Task<IFlurlResponse> RefreshAuth() => httpClient.Request("Api", "Command")
		.PostJsonAsync(new {Command = "farm " + botConfiguration.BotName});
}
