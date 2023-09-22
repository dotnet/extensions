// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Logging;

public static class LoggingEnrichmentExtensions
{
    public static ILoggingBuilder EnableEnrichment(this ILoggingBuilder builder);
    public static ILoggingBuilder EnableEnrichment(this ILoggingBuilder builder, Action<LoggerEnrichmentOptions> configure);
    public static ILoggingBuilder EnableEnrichment(this ILoggingBuilder builder, IConfigurationSection section);
}
