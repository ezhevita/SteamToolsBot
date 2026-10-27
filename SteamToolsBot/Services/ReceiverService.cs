using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamToolsBot.Commands;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SteamToolsBot.Services;

internal sealed class ReceiverService
{
	private readonly ITelegramBotClient _botClient;
	private readonly IEnumerable<ICommand> _commands;
	private readonly IUpdateHandler _updateHandlers;

	public ReceiverService(
		ITelegramBotClient botClient,
		IUpdateHandler updateHandler,
		IEnumerable<ICommand> commands)
	{
		_botClient = botClient;
		_updateHandlers = updateHandler;
		_commands = commands;
	}

	public async Task ReceiveAsync(CancellationToken stoppingToken)
	{
		var receiverOptions = new ReceiverOptions
		{
			AllowedUpdates = [UpdateType.Message],
			DropPendingUpdates = false
		};

		var me = await _botClient.GetMe(stoppingToken);
		if (string.IsNullOrEmpty(me.Username))
		{
			throw new InvalidOperationException();
		}

		var scope = new BotCommandScopeAllPrivateChats();
		await _botClient.SetMyCommands(
			_commands.Select(static command => new BotCommand
				{Command = command.Command, Description = command.EnglishDescription}), scope, cancellationToken: stoppingToken);

		string[] russianLocales = ["ru", "uk", "be"];

		await Task.WhenAll(
			russianLocales.Select(locale => _botClient.SetMyCommands(
				_commands.Select(static command => new BotCommand
					{Command = command.Command, Description = command.RussianDescription}), scope, locale, stoppingToken)));

		await _botClient.ReceiveAsync(_updateHandlers, receiverOptions, stoppingToken);
	}
}
