using System.Threading;
using System.Threading.Tasks;

namespace SteamToolsBot.Commands;

public interface ICommand
{
	string Command { get; }
	string EnglishDescription { get; }
	string RussianDescription { get; }
	Task<string> Execute(CancellationToken cancellationToken);
}
