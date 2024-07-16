using System;
using System.Diagnostics.CodeAnalysis;
using SteamToolsBot.Enums;

namespace SteamToolsBot.Exceptions;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal",
	Justification = "Conflicts with CA1064 : Exceptions should be public")]
public sealed class RequestFailedException : Exception
{
	public RequestFailedException(EResult resultCode) => ResultCode = resultCode;

	public RequestFailedException() => ResultCode = EResult.Fail;

	public RequestFailedException(string message) : base(message) => ResultCode = EResult.Fail;

	public RequestFailedException(string message, Exception innerException) : base(message, innerException)
	{
	}

	public EResult ResultCode { get; }
}
