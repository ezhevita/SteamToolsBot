using System;
using System.Threading.Tasks;

namespace SteamToolsBot.Helpers;

public static class ExceptionHandler
{
	internal static async Task Silence(Func<Task> func)
	{
		try
		{
			await func();
#pragma warning disable CA1031
		} catch
#pragma warning restore CA1031
		{
			// ignored
		}
	}
}
