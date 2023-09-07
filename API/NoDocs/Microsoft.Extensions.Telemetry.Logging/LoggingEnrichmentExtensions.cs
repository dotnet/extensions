// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Logging;

[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class LoggingEnrichmentExtensions
{
    public static ILoggingBuilder EnableEnrichment(this ILoggingBuilder builder);
    public static ILoggingBuilder EnableEnrichment(this ILoggingBuilder builder, Action<LoggerEnrichmentOptions> configure);
    public static ILoggingBuilder EnableEnrichment(this ILoggingBuilder builder, IConfigurationSection section);
}
