using System;
using System.Diagnostics.CodeAnalysis;

namespace SteamToolsBot.Exceptions;

[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Unnecessary")]
internal sealed class RequestFailedException : Exception;
