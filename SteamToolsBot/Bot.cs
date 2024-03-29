using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using SteamToolsBot.Commands;
using SteamToolsBot.Helpers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SteamToolsBot;

public class Bot
{
	private readonly HashSet<long> bannedUsers;
	private readonly Dictionary<string, ICommand> commands;
	private readonly RateLimiter rateLimiter;
	private readonly Dictionary<ICommand, SemaphoreSlim> semaphores;
	private readonly string username;

	public Bot(HashSet<long> bannedUsers, IList<ICommand> commands, string username, RateLimiter rateLimiter)
	{
		this.bannedUsers = bannedUsers;
		this.username = username;
		this.rateLimiter = rateLimiter;
		this.commands = commands.ToDictionary(x => x.Command, x => x, StringComparer.OrdinalIgnoreCase);
		semaphores = commands.ToDictionary(x => x, _ => new SemaphoreSlim(1, 1));
	}

	internal static Task HandleError(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
	{
		Log.Fatal(exception, "Unhandled exception occurred!");

		return Task.CompletedTask;
	}

	internal async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
	{
		var message = update.Message;
		if ((message?.From == null) || bannedUsers.Contains(message.From.Id))
		{
			return;
		}

		if (await rateLimiter.ShouldUserBeRejected(message.From.Id))
			return;

		if ((message.Type != MessageType.Text) || (message.ForwardFrom != null) || string.IsNullOrEmpty(message.Text) ||
		    (message.Text[0] != '/'))
		{
			return;
		}

		var firstWord = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

		if (string.IsNullOrEmpty(firstWord))
			return;

		var splitBotName = firstWord.Split('@', StringSplitOptions.RemoveEmptyEntries);

		if ((splitBotName.Length > 1) && (splitBotName[1] != username))
			return;

		var commandText = splitBotName[0].TrimStart('/');
		Log.Information("Got new message by {Sender} with command {Command}", message.From, commandText);

		if (commands.TryGetValue(commandText, out var command))
		{
			var semaphore = semaphores[command];

			try
			{
				var sentMessage = await client.SendTextMessageAsync(
					message.Chat.Id, "Please wait...", cancellationToken: cancellationToken);

				await semaphore.WaitAsync(cancellationToken);

				var response = await command.Execute();

				if (!string.IsNullOrEmpty(response))
				{
					await client.SendTextMessageAsync(
						message.Chat.Id, response.Replace(".", "\\.", StringComparison.Ordinal), parseMode: ParseMode.MarkdownV2,
						disableWebPagePreview: true, cancellationToken: cancellationToken);
				}

				await ExceptionHandler.Silence(
					() => client.DeleteMessageAsync(sentMessage.Chat.Id, sentMessage.MessageId, cancellationToken));
#pragma warning disable CA1031
			} catch (Exception e)
#pragma warning restore CA1031
			{
				Log.Error(e, "Got exception!");
				await ExceptionHandler.Silence(
					() => client.SendTextMessageAsync(
						message.Chat.Id, "An error occured, please try again", cancellationToken: cancellationToken));
			} finally
			{
				semaphore.Release();
			}
		}
	}
}
