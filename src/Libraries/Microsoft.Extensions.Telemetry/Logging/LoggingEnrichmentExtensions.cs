// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Extensions for configuring logging enrichment features.
/// </summary>
[Experimental(diagnosticId: Experiments.Telemetry, UrlFormat = Experiments.UrlFormat)]
public static class LoggingEnrichmentExtensions
{
    /// <summary>
    /// Enables enrichment functionality within the logging infrastructure.
    /// </summary>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILoggingBuilder EnableEnrichment(this ILoggingBuilder builder)
        => EnableEnrichment(builder, _ => { });

    /// <summary>
    /// Enables enrichment functionality within the logging infrastructure.
    /// </summary>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <param name="configure">Delegate the fine-tune the options.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILoggingBuilder EnableEnrichment(this ILoggingBuilder builder, Action<LoggerEnrichmentOptions> configure)
    {
        _ = Throw.IfNull(builder);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerFactory, ExtendedLoggerFactory>());
        _ = builder.Services.Configure(configure);
        _ = builder.Services.AddOptionsWithValidateOnStart<LoggerEnrichmentOptions, LoggerEnrichmentOptionsValidator>();

        return builder;
    }

    /// <summary>
    /// Enables enrichment functionality within the logging infrastructure.
    /// </summary>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <param name="section">Configuration section that contains <see cref="LoggerEnrichmentOptions"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILoggingBuilder EnableEnrichment(this ILoggingBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerFactory, ExtendedLoggerFactory>());
        _ = builder.Services.AddOptionsWithValidateOnStart<LoggerEnrichmentOptions, LoggerEnrichmentOptionsValidator>().Bind(section);

        return builder;
    }
}
