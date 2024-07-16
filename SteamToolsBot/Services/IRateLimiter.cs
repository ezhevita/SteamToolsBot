using System.Threading.Tasks;

namespace SteamToolsBot.Services;

internal interface IRateLimiter
{
	Task<bool> ShouldUserBeRejected(long id);
}
