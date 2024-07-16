using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Polly;
using SteamToolsBot.Exceptions;
using SteamToolsBot.FarmWebOptionsValidator;
using SteamToolsBot.Services;

namespace SteamToolsBot.Commands;

internal sealed class FiveDollarCommand : ICommand
{
	private readonly IOptionsMonitor<FiveDollarCommandOptions> _config;
	private readonly AsyncPolicy<string> _executePolicy;
	private readonly FarmWebClient _farmWebClient;

	public FiveDollarCommand(IOptionsMonitor<FiveDollarCommandOptions> config, FarmWebClient farmWebClient)
	{
		_config = config;
		_farmWebClient = farmWebClient;
		_executePolicy = Policy<string>.Handle<RequestFailedException>()
			.WaitAndRetryAsync(
				3, static attempt => TimeSpan.FromSeconds(attempt),
				(_, _, _) => _farmWebClient.SendCommand("farm " + _config.CurrentValue.TargetBotName, CancellationToken.None));
	}

	public string Command => "5dollar";
	public string EnglishDescription => "Convert Steam $5 to Russian roubles";
	public string RussianDescription => "Конвертация $5 в Steam в рубли";

	public async Task<string> Execute(CancellationToken cancellationToken) =>
		await _executePolicy.ExecuteAsync(GetPrice, cancellationToken);

	private async Task<string> GetPrice(CancellationToken cancellationToken)
	{
		var response = await _farmWebClient.GetBuyItemPage(
			_config.CurrentValue.TargetBotName, _config.CurrentValue.ItemAppID, _config.CurrentValue.ItemID, 1,
			cancellationToken);

		if (response.Body == null)
			throw new RequestFailedException();

		var result = response.Body.QuerySelector("#review_total_value")?.TextContent;

		if (string.IsNullOrEmpty(result))
			throw new RequestFailedException();

		return result;
	}
}
