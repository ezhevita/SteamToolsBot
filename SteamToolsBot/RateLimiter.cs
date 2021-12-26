using System;
using System.Globalization;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace SteamToolsBot;

public class RateLimiter
{
	private readonly ConnectionMultiplexer redis;

	public RateLimiter(ConnectionMultiplexer redis) => this.redis = redis;

	public async Task<bool> ShouldUserBeRejected(long id)
	{
		var database = redis.GetDatabase();
		var key = new RedisKey(id.ToString(CultureInfo.InvariantCulture));

		if (await database.KeyExistsAsync(key))
			return true;

		await database.StringSetAsync(key, new RedisValue("1"), TimeSpan.FromSeconds(1));

		return false;
	}
}
