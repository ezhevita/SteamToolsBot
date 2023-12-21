using System;
using SteamToolsBot.Enums;

namespace SteamToolsBot.Exceptions;

public class RequestFailedException : Exception
{
	public RequestFailedException(EResult resultCode) => ResultCode = resultCode;

	public RequestFailedException()
	{
		ResultCode = EResult.Fail;
	}

	public RequestFailedException(string message) : base(message)
	{
		ResultCode = EResult.Fail;
	}

	public EResult ResultCode { get; }

    public RequestFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
