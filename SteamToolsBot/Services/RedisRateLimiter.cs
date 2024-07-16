using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using SteamToolsBot.Options;

namespace SteamToolsBot.Services;

internal sealed class RedisRateLimiter : IRateLimiter
{
	private readonly IOptionsMonitor<BotOptions> _options;
	private readonly IOptionsMonitor<ConfigurationOptions> _redisOptions;
	private ConnectionMultiplexer? _redis;

	public RedisRateLimiter(IOptionsMonitor<BotOptions> options, IOptionsMonitor<ConfigurationOptions> redisOptions)
	{
		_options = options;
		_redisOptions = redisOptions;
	}

	public async Task<bool> ShouldUserBeRejected(long id)
	{
		_redis ??= await ConnectionMultiplexer.ConnectAsync(_redisOptions.CurrentValue);

		var database = _redis.GetDatabase();
		var key = new RedisKey(id.ToString(CultureInfo.InvariantCulture));

		if (await database.KeyExistsAsync(key))
			return true;

		await database.StringSetAsync(key, new RedisValue("1"), TimeSpan.FromSeconds(_options.CurrentValue.CooldownSeconds));

		return false;
	}
}
