using System;
using System.Threading.Tasks;
using Flurl.Http;

namespace SteamToolsBot.Commands;

public interface ICommand
{
	string Command { get; }
	string EnglishDescription { get; }
	string RussianDescription { get; }
	Task<string> Execute();
	Task Initialize(BotConfiguration config, IFlurlClient farmClient, Func<IFlurlClient> steamClientFactory);
}
