// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.Diagnostics;
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
    /// <param name="section">Configuration section that contains <see cref="LoggingOptions"/>.</param>
    /// <returns>Logging <paramref name="builder"/>.</returns>
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        builder.Services.TryAddLoggerProvider();
        _ = builder.Services.AddValidatedOptions<LoggingOptions, LoggingOptionsValidator>().Bind(section);

        return builder;
    }

    /// <summary>
    /// Configure logging.
    /// </summary>
    /// <param name="builder">Logging builder.</param>
    /// <param name="configure">Logging configuration options.</param>
    /// <returns>Logging <paramref name="builder"/>.</returns>
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder builder, Action<LoggingOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        builder.Services.TryAddLoggerProvider();

        _ = builder.Services.AddValidatedOptions<LoggingOptions, LoggingOptionsValidator>().Configure(configure);

        return builder;
    }

    /// <summary>
    /// Configure logging with default options.
    /// </summary>
    /// <param name="builder">Logging builder.</param>
    /// <returns>Logging <paramref name="builder"/>.</returns>
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder builder) => builder.AddOpenTelemetryLogging(_ => { });

    /// <summary>
    /// Adds a logging processor to the builder.
    /// </summary>
    /// <param name="builder">The builder to add the processor to.</param>
    /// <param name="processor">Log processor to add.</param>
    /// <returns>Returns <see cref="ILoggingBuilder"/> for chaining.</returns>
    public static ILoggingBuilder AddProcessor(this ILoggingBuilder builder, BaseProcessor<LogRecord> processor)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(processor);

        _ = builder.Services.AddSingleton(processor);

        return builder;
    }

    /// <summary>
    /// Adds a logging processor to the builder.
    /// </summary>
    /// <typeparam name="T">Type of processor to add.</typeparam>
    /// <param name="builder">The builder to add the processor to.</param>
    /// <returns>Returns <see cref="ILoggingBuilder"/> for chaining.</returns>
    public static ILoggingBuilder AddProcessor<T>(this ILoggingBuilder builder)
        where T : BaseProcessor<LogRecord>
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services.AddSingleton<BaseProcessor<LogRecord>, T>();

        return builder;
    }

    private static void TryAddLoggerProvider(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, LoggerProvider>(
            sp => new LoggerProvider(
                sp.GetRequiredService<IOptions<LoggingOptions>>(),
                sp.GetServices<ILogEnricher>(),
                sp.GetServices<BaseProcessor<LogRecord>>())));
    }
}
