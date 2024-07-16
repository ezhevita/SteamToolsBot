using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace SteamToolsBot.Helpers;

internal static class ExceptionHandler
{
	[SuppressMessage("Design", "CA1031:Do not catch general exception types",
		Justification = "Intentionally silencing everything")]
	internal static async Task Silence(Func<Task> func)
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
