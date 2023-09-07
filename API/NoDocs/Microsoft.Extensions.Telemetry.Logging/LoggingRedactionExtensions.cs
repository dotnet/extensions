// Assembly 'Microsoft.Extensions.Telemetry'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Logging;

[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class LoggingRedactionExtensions
{
    public static ILoggingBuilder EnableRedaction(this ILoggingBuilder builder);
}
