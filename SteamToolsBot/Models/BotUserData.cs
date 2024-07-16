using System;

namespace SteamToolsBot.Models;

internal sealed record BotUserData
{
	private string? _username;

	public string Username
	{
		get => _username ?? throw new InvalidOperationException();
		set => _username = _username == null ? value : throw new InvalidOperationException();
	}
}
