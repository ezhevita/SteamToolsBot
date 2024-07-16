using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SteamToolsBot.Commands;
using SteamToolsBot.Models;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SteamToolsBot.Services;

internal sealed partial class UpdateHandler : IUpdateHandler
{
	private readonly BotUserData _botUserData;
	private readonly CommandHandler _commandHandler;
	private readonly ILogger<UpdateHandler> _logger;

	public UpdateHandler(BotUserData botUserData, CommandHandler commandHandler, ILogger<UpdateHandler> logger)
	{
		_botUserData = botUserData;
		_commandHandler = commandHandler;
		_logger = logger;
	}

	public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		if (update is not
		    {
			    Type: UpdateType.Message,
			    Message: {Chat.Type: ChatType.Private, Type: MessageType.Text, ForwardFrom: null} message
		    })
			return;

		var messageText = message.Text;

		if (string.IsNullOrEmpty(messageText))
			return;

		if (!messageText.StartsWith('/'))
			return;

		if (messageText.Length < 2)
			return;

		// as of now none of the commands accept arguments
		if (messageText.Contains(' ', StringComparison.Ordinal))
			return;

		var usernameSeparator = messageText.IndexOf('@', StringComparison.Ordinal);
		string command;
		if (usernameSeparator != -1)
		{
			if (messageText[(usernameSeparator + 1)..] != _botUserData.Username)
				return;

			command = messageText[1..usernameSeparator];
		} else
		{
			command = messageText[1..];
		}

		await _commandHandler.Execute(botClient, command, message, cancellationToken);
	}

	public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
	{
		LogPollingError(exception);

		return Task.CompletedTask;
	}

	[LoggerMessage(LogLevel.Error, "Polling error occurred!")]
	private partial void LogPollingError(Exception exception);
}
