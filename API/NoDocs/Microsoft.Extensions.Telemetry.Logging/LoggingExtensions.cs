// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Microsoft.Extensions.Telemetry.Logging;

public static class LoggingExtensions
{
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder builder, IConfigurationSection section);
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder builder, Action<LoggingOptions> configure);
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder builder);
    public static ILoggingBuilder AddProcessor(this ILoggingBuilder builder, BaseProcessor<LogRecord> processor);
    public static ILoggingBuilder AddProcessor<T>(this ILoggingBuilder builder) where T : BaseProcessor<LogRecord>;
}
