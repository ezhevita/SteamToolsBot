using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Serialization.TextJson;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using SteamToolsBot.Commands;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace SteamToolsBot;

internal static class Program
{
	private static readonly SemaphoreSlim MainThreadSemaphore = new(0, 1);

	private static async Task Main()
	{
		if (!File.Exists("config.json"))
		{
			Console.WriteLine("Config file does not exist, shutting down...");

			return;
		}

		BotConfiguration? config;
		await using (var configFile = File.OpenRead("config.json"))
		{
			config = await JsonSerializer.DeserializeAsync<BotConfiguration>(
				configFile, new JsonSerializerOptions
				{
					Converters = {new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)}
				}
			);

			if (config == null)
			{
				Console.WriteLine("Config file is invalid!");

				return;
			}
		}

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.WriteTo.Console()
			.WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
			.WriteTo.Telegram(config.TelegramToken, config.TelegramOwnerID.ToString(CultureInfo.InvariantCulture), restrictedToMinimumLevel: LogEventLevel.Information)
			.CreateLogger();

		HashSet<long>? bannedUsers;
		if (File.Exists("banned.json"))
		{
			await using var bannedFile = File.OpenRead("banned.json");
			bannedUsers = await JsonSerializer.DeserializeAsync<HashSet<long>>(bannedFile) ?? new HashSet<long>();
		} else
		{
			bannedUsers = new HashSet<long>();
		}

		var botClient = new TelegramBotClient(config.TelegramToken);
		Log.Debug("{Name} started", nameof(SteamToolsBot));
		var botUser = await botClient.GetMeAsync();
		Log.Information("Hello, I'm {Username}!", botUser.Username);

		var commands = new ICommand[]
		{
			new FiveDollarCommand(),
			new CardPriceCommand()
		};

		var farmClient = new FlurlClient(
			new HttpClient(
				new HttpClientHandler
				{
					UseCookies = true,
					CookieContainer = new CookieContainer(),
					AllowAutoRedirect = true,
					CheckCertificateRevocationList = true
				}
			)
			{
				BaseAddress = new Uri(config.IPCAddress),
				DefaultRequestHeaders =
				{
					{"Authentication", config.IPCPassword}
				}
			}
		).Configure(settings => settings.WithTextJsonSerializer());

		var redis = await ConnectionMultiplexer.ConnectAsync("redis");
		var rateLimiter = new RateLimiter(redis, TimeSpan.FromSeconds(config.CooldownSeconds));
		var steamClient = new FlurlClient("https://steamcommunity.com").Configure(settings => settings.WithTextJsonSerializer());

		try
		{
			await Task.WhenAll(commands.Select(x => x.Initialize(config, farmClient, steamClient)));
		} catch (Exception e)
		{
			Log.Fatal(e, "Unhandled exception occurred!");
		}

		var russianCommands = commands.Select(x => new BotCommand {Command = x.Command, Description = x.RussianDescription}).ToArray();
		await botClient.SetMyCommandsAsync(russianCommands, languageCode: "ru");
		await botClient.SetMyCommandsAsync(russianCommands, languageCode: "uk");
		await botClient.SetMyCommandsAsync(russianCommands, languageCode: "be");

		var englishCommands = commands.Select(x => new BotCommand {Command = x.Command, Description = x.EnglishDescription}).ToArray();
		await botClient.SetMyCommandsAsync(englishCommands);

		var bot = new Bot(bannedUsers, commands, botUser.Username!, rateLimiter);
		botClient.StartReceiving(bot.HandleUpdate, Bot.HandleError, new ReceiverOptions {AllowedUpdates = new[] {UpdateType.Message}});

		await MainThreadSemaphore.WaitAsync();
	}
}
