using System;
using System.Threading.Tasks;

namespace SteamToolsBot.Helpers;

public static class ExceptionHandler
{
	public static async Task Silence(Func<Task> func)
	{
		try
		{
			await func();
		} catch
		{
			// ignored
		}
	}
}
