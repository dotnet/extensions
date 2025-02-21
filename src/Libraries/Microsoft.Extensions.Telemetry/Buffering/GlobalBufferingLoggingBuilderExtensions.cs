// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Lets you register log buffering in a dependency injection container.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public static class GlobalBufferingLoggingBuilderExtensions
{
    /// <summary>
    /// Adds global log buffering to the logging infrastructure.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <param name="configuration">The <see cref="IConfiguration" /> to add.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Matched logs will be buffered and can optionally be flushed and emitted.
    /// </remarks>
    public static ILoggingBuilder AddGlobalBuffering(this ILoggingBuilder builder, IConfiguration configuration)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configuration);

        _ = builder.Services.AddSingleton<IConfigureOptions<GlobalLogBufferingOptions>>(new GlobalLogBufferingConfigureOptions(configuration));

        return builder.AddGlobalBufferManager();
    }

    /// <summary>
    /// Adds global log buffering to the logging infrastructure.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <param name="configure">Configure buffer options.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Matched logs will be buffered and can optionally be flushed and emitted.
    /// </remarks>
    public static ILoggingBuilder AddGlobalBuffering(this ILoggingBuilder builder, Action<GlobalLogBufferingOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services.Configure(configure);

        return builder.AddGlobalBufferManager();
    }

    /// <summary>
    /// Adds global log buffering to the logging infrastructure.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <param name="logLevel">The log level (and below) to apply the buffer to.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Matched logs will be buffered and can optionally be flushed and emitted.
    /// </remarks>
    public static ILoggingBuilder AddGlobalBuffering(this ILoggingBuilder builder, LogLevel? logLevel = null)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services.Configure<GlobalLogBufferingOptions>(options => options.Rules.Add(new LogBufferingFilterRule(logLevel: logLevel)));

        return builder.AddGlobalBufferManager();
    }

    internal static ILoggingBuilder AddGlobalBufferManager(this ILoggingBuilder builder)
    {
        _ = builder.Services.AddExtendedLoggerFeactory();

        builder.Services.TryAddSingleton<GlobalBufferManager>();
        builder.Services.TryAddSingleton<LogBuffer>(static sp => sp.GetRequiredService<GlobalBufferManager>());

        return builder;
    }
}
