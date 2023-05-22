// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#endif
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;
using Microsoft.Shared.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Microsoft.Extensions.Telemetry.Console;

/// <summary>
/// Console exporter logging extensions for R9 logger.
/// </summary>
public static class LoggingConsoleExtensions
{
    /// <summary>
    /// Adds console exporter as a configuration to the OpenTelemetry ILoggingBuilder.
    /// </summary>
    /// <param name="builder">Logging builder where the exporter will be added.</param>
    /// <returns>The instance of <see cref="ILoggingBuilder"/> to chain the calls.</returns>
    public static ILoggingBuilder AddConsoleExporter(this ILoggingBuilder builder)
    {
        _ = Throw.IfNull(builder);

        builder.Services.TryAddSingleton<BaseExporter<LogRecord>, LoggingConsoleExporter>();

#if NET5_0_OR_GREATER
        builder.Services.AddOptions<LoggingConsoleOptions>();
#endif

        return builder.AddProcessor<SimpleLogRecordExportProcessor>();
    }

#if NET5_0_OR_GREATER
    /// <summary>
    /// Adds console exporter as a configuration to the OpenTelemetry ILoggingBuilder.
    /// </summary>
    /// <param name="builder">Logging builder where the exporter will be added.</param>
    /// <param name="configure">An action to configure the <see cref="LoggingConsoleOptions"/> for console output customization.</param>
    /// <returns>The instance of <see cref="ILoggingBuilder"/> to chain the calls.</returns>
    [Experimental]
    public static ILoggingBuilder AddConsoleExporter(this ILoggingBuilder builder, Action<LoggingConsoleOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        builder.Services.TryAddSingleton<BaseExporter<LogRecord>, LoggingConsoleExporter>();
        _ = builder.Services.Configure(configure);

        return builder.AddProcessor<SimpleLogRecordExportProcessor>();
    }

    /// <summary>
    /// Adds console exporter as a configuration to the OpenTelemetry ILoggingBuilder.
    /// </summary>
    /// <param name="builder">Logging builder where the exporter will be added.</param>
    /// <param name="section">The configuration section to bind <see cref="LoggingConsoleOptions"/> for customization of the console output.</param>
    /// <returns>The instance of <see cref="ILoggingBuilder"/> to chain the calls.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(LoggingConsoleOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    [Experimental]
    public static ILoggingBuilder AddConsoleExporter(this ILoggingBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        builder.Services.TryAddSingleton<BaseExporter<LogRecord>, LoggingConsoleExporter>();
        _ = builder.Services
            .AddOptions<LoggingConsoleOptions>()
            .Bind(section);

        return builder.AddProcessor<SimpleLogRecordExportProcessor>();
    }
#endif
}
