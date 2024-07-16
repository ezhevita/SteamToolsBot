using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Caching;
using SteamToolsBot.Helpers;
using SteamToolsBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SteamToolsBot.Commands;

internal sealed partial class CommandHandler
{
	private readonly AsyncCachePolicy<string> _cachePolicy;
	private readonly FrozenDictionary<string, ICommand> _commands;
	private readonly ILogger<CommandHandler> _logger;
	private readonly IRateLimiter _rateLimiter;

	public CommandHandler(IEnumerable<ICommand> commands, AsyncCachePolicy<string> cachePolicy, IRateLimiter rateLimiter,
		ILogger<CommandHandler> logger)
	{
		_cachePolicy = cachePolicy;
		_rateLimiter = rateLimiter;
		_logger = logger;
		_commands = commands.ToFrozenDictionary(static command => command.Command.ToUpperInvariant(), static command => command);
	}

	public async Task Execute(ITelegramBotClient botClient, string commandName, Message userMessage,
		CancellationToken cancellationToken)
	{
		if (!_commands.TryGetValue(commandName.ToUpperInvariant(), out var command))
			return;

		if (await _rateLimiter.ShouldUserBeRejected(userMessage.From!.Id))
			return;

		using var sendActionCancellationTokenSource = new CancellationTokenSource();
		var sendActionToken = sendActionCancellationTokenSource.Token;
		_ = Task.Run(() => SendTypingActionUntilCancelled(botClient, userMessage.Chat.Id, sendActionToken), sendActionToken);

		string response;
		try
		{
			response = await _cachePolicy.ExecuteAsync(
				(_, token) => command.Execute(token), new Context(command.Command), cancellationToken);

			if (string.IsNullOrEmpty(response))
				response = "Failed!";
		}
#pragma warning disable CA1031 // we don't want to stop the application in case of failure, catching everything here
		catch (Exception e)
#pragma warning restore CA1031 // we don't want to stop the application in case of failure, catching everything here
		{
			response = "Failed!";
			LogCommandError(command.Command, e);
		} finally
		{
			await sendActionCancellationTokenSource.CancelAsync();
		}

		await ExceptionHandler.Silence(
			() => botClient.SendTextMessageAsync(
				userMessage.Chat.Id, response, parseMode: ParseMode.Html, replyToMessageId: userMessage.MessageId,
				disableWebPagePreview: true, cancellationToken: cancellationToken));
	}

	[LoggerMessage(LogLevel.Error, "Failed executing command '{Command}'")]
	private partial void LogCommandError(string command, Exception exception);

	private static async Task SendTypingActionUntilCancelled(ITelegramBotClient botClient, long chatId,
		CancellationToken cancellationToken)
	{
		try
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				await ExceptionHandler.Silence(
					() => botClient.SendChatActionAsync(chatId, ChatAction.Typing, cancellationToken: cancellationToken));

				await Task.Delay(5000, cancellationToken);
			}
		} catch (TaskCanceledException)
		{
		}
	}
}
