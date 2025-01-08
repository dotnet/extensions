// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Lets you register log buffers in a dependency injection container.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public static class GlobalBufferLoggerBuilderExtensions
{
    /// <summary>
    /// Adds global buffer to the logging infrastructure.
    /// Matched logs will be buffered and can optionally be flushed and emitted./>.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <param name="configuration">The <see cref="IConfiguration" /> to add.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ILoggingBuilder AddGlobalBuffer(this ILoggingBuilder builder, IConfiguration configuration)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configuration);

        return builder
            .AddGlobalBufferConfiguration(configuration)
            .AddGlobalBufferManager();
    }

    /// <summary>
    /// Adds global buffer to the logging infrastructure.
    /// Matched logs will be buffered and can optionally be flushed and emitted./>.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <param name="level">The log level (and below) to apply the buffer to.</param>
    /// <param name="configure">Configure buffer options.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ILoggingBuilder AddGlobalBuffer(this ILoggingBuilder builder, LogLevel? level = null, Action<GlobalBufferOptions>? configure = null)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services
            .Configure<GlobalBufferOptions>(options => options.Rules.Add(new BufferFilterRule(null, level, null)))
            .Configure(configure ?? new Action<GlobalBufferOptions>(_ => { }));

        return builder.AddGlobalBufferManager();
    }

    /// <summary>
    /// Adds global logging buffer manager.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    internal static ILoggingBuilder AddGlobalBufferManager(this ILoggingBuilder builder)
    {
        _ = Throw.IfNull(builder);

        builder.Services.TryAddSingleton<GlobalBufferManager>();
        builder.Services.TryAddSingleton<IBufferManager>(static sp => sp.GetRequiredService<GlobalBufferManager>());
        builder.Services.TryAddSingleton<IGlobalBufferManager>(static sp => sp.GetRequiredService<GlobalBufferManager>());

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerFactory, ExtendedLoggerFactory>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, GlobalBufferManager>(static sp => sp.GetRequiredService<GlobalBufferManager>()));

        return builder;
    }

    /// <summary>
    /// Configures <see cref="GlobalBufferOptions" /> from an instance of <see cref="IConfiguration" />.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <param name="configuration">The <see cref="IConfiguration" /> to add.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    internal static ILoggingBuilder AddGlobalBufferConfiguration(this ILoggingBuilder builder, IConfiguration configuration)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services.AddSingleton<IConfigureOptions<GlobalBufferOptions>>(new GlobalBufferConfigureOptions(configuration));

        return builder;
    }
}
