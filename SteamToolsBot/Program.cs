using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Caching;
using Polly.Caching.Distributed;
using StackExchange.Redis;
using SteamToolsBot.Commands;
using SteamToolsBot.FarmWebOptionsValidator;
using SteamToolsBot.Helpers;
using SteamToolsBot.Models;
using SteamToolsBot.Options;
using SteamToolsBot.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddValidatableOptions<BotOptions, BotOptionsValidator>();
builder.Services.AddValidatableOptions<CardPriceCommandOptions, CardPriceCommandOptionsValidator>();
builder.Services.AddValidatableOptions<FarmWebOptions, FarmWebOptionsValidator>();
builder.Services.AddValidatableOptions<FiveDollarCommandOptions, FiveDollarCommandOptionsValidator>();

builder.Services.AddOptions<ConfigurationOptions>()
	.Configure<IOptions<BotOptions>>(
		static (redisOptions, botOptions) => redisOptions.EndPoints.Add(botOptions.Value.RedisHostname));
builder.Services.AddOptions<RedisCacheOptions>()
	.Configure<IOptions<ConfigurationOptions>>(
		static (cacheOptions, redisOptions) => cacheOptions.ConfigurationOptions = redisOptions.Value);

builder.Services.AddSingleton<IDistributedCache, RedisCache>();
builder.Services.AddSingleton<IAsyncCacheProvider<string>, NetStandardIDistributedCacheStringProvider>();
builder.Services.AddSingleton<AsyncCachePolicy<string>>(
	static sp => Policy.CacheAsync(
		sp.GetRequiredService<IAsyncCacheProvider<string>>(),
		TimeSpan.FromSeconds(sp.GetRequiredService<IOptions<BotOptions>>().Value.CommandResponseCacheSeconds)));

builder.Services.AddHttpClient("telegram_bot_client")
	.AddTypedClient<ITelegramBotClient>(
		static (httpClient, sp) =>
		{
			var botConfig = sp.GetRequiredService<IOptions<BotOptions>>().Value;
			var options = new TelegramBotClientOptions(botConfig.TelegramToken);

			return new TelegramBotClient(options, httpClient);
		});

builder.Services.AddScoped<IUpdateHandler, UpdateHandler>();
builder.Services.AddScoped<ReceiverService>();
builder.Services.AddSingleton<BotUserData>();
builder.Services.AddSingleton<IRateLimiter, RedisRateLimiter>();

builder.Services.AddHttpClient<FarmWebClient>()
	.ConfigurePrimaryHttpMessageHandler(
		static _ => new HttpClientHandler
		{
			AllowAutoRedirect = true,
			AutomaticDecompression = DecompressionMethods.All,
			CookieContainer = new CookieContainer(),
			UseCookies = true
		});

builder.Services.AddHttpClient<CardPriceCommand>(
		static client =>
		{
			client.BaseAddress = new Uri("https://steamcommunity.com/market/");
			client.Timeout = TimeSpan.FromSeconds(15);
		})
	.AddTypedClient<MarketWebClient>()
	.ConfigurePrimaryHttpMessageHandler(
		static sp => new HttpClientHandler
			{Proxy = sp.GetRequiredService<IOptions<CardPriceCommandOptions>>().Value.SteamParsedProxy})
	.AddResilienceHandler(
		nameof(MarketWebClient), static pipelineBuilder => pipelineBuilder
			.AddRetry(
				new HttpRetryStrategyOptions
					{BackoffType = DelayBackoffType.Linear, Delay = TimeSpan.FromSeconds(1), MaxRetryAttempts = 3}));

builder.Services.AddSingleton<ICommand, FiveDollarCommand>();
builder.Services.AddSingleton<ICommand, CardPriceCommand>();
builder.Services.AddSingleton<CommandHandler>();

builder.Services.AddHostedService<PollingService>();

var host = builder.Build();
await host.RunAsync();
