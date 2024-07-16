using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Options;
using SteamToolsBot.Exceptions;
using SteamToolsBot.Options;

namespace SteamToolsBot.Services;

internal sealed class FarmWebClient
{
	private readonly HttpClient _client;

	public FarmWebClient(HttpClient client, IOptionsMonitor<FarmWebOptions> config)
	{
		_client = client;

		_client.BaseAddress = new Uri(config.CurrentValue.IPCAddress.TrimEnd('/') + "/Api/");

		if (!string.IsNullOrEmpty(config.CurrentValue.IPCPassword))
			_client.DefaultRequestHeaders.Add("Authentication", config.CurrentValue.IPCPassword);
	}

	public async Task<IDocument> GetBuyItemPage(string targetBotName, uint appID, ulong itemID, uint amount,
		CancellationToken cancellationToken)
	{
		var page = await _client.GetAsync(
			new Uri(
				string.Join('/', "Web", targetBotName, "https://checkout.steampowered.com", "buyitem", appID, itemID, amount),
				UriKind.Relative), cancellationToken);

		if (!page.IsSuccessStatusCode)
		{
			page.Dispose();

			throw new RequestFailedException();
		}

#pragma warning disable CA2000 // context only disposes the underlying document, document should be disposed by the caller
		var browsingContext = new BrowsingContext();
#pragma warning restore CA2000 // context only disposes the underlying document, document should be disposed by the caller

		var stream = await page.Content.ReadAsStreamAsync(cancellationToken);

		return await browsingContext.OpenAsync(x => x.Content(stream, true), cancellationToken);
	}

	public async Task SendCommand(string command, CancellationToken cancellationToken)
	{
		using var response = await _client.PostAsJsonAsync(
			new Uri("Command", UriKind.Relative), new {Command = command}, cancellationToken);
	}
}
