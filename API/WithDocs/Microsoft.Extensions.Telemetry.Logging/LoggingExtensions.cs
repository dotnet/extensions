// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Extensions for configuring logging.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Configure logging.
    /// </summary>
    /// <param name="builder">Logging builder.</param>
    /// <param name="section">Configuration section that contains <see cref="T:Microsoft.Extensions.Telemetry.Logging.LoggingOptions" />.</param>
    /// <returns>Logging <paramref name="builder" />.</returns>
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder builder, IConfigurationSection section);

    /// <summary>
    /// Configure logging.
    /// </summary>
    /// <param name="builder">Logging builder.</param>
    /// <param name="configure">Logging configuration options.</param>
    /// <returns>Logging <paramref name="builder" />.</returns>
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder builder, Action<LoggingOptions> configure);

    /// <summary>
    /// Configure logging with default options.
    /// </summary>
    /// <param name="builder">Logging builder.</param>
    /// <returns>Logging <paramref name="builder" />.</returns>
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder builder);

    /// <summary>
    /// Adds a logging processor to the builder.
    /// </summary>
    /// <param name="builder">The builder to add the processor to.</param>
    /// <param name="processor">Log processor to add.</param>
    /// <returns>Returns <see cref="T:Microsoft.Extensions.Logging.ILoggingBuilder" /> for chaining.</returns>
    public static ILoggingBuilder AddProcessor(this ILoggingBuilder builder, BaseProcessor<LogRecord> processor);

    /// <summary>
    /// Adds a logging processor to the builder.
    /// </summary>
    /// <typeparam name="T">Type of processor to add.</typeparam>
    /// <param name="builder">The builder to add the processor to.</param>
    /// <returns>Returns <see cref="T:Microsoft.Extensions.Logging.ILoggingBuilder" /> for chaining.</returns>
    public static ILoggingBuilder AddProcessor<T>(this ILoggingBuilder builder) where T : BaseProcessor<LogRecord>;
}
