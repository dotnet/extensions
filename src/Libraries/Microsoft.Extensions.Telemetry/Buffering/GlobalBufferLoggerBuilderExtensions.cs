// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            .AddGlobalBufferProvider();
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

        return builder.AddGlobalBufferProvider();
    }

    /// <summary>
    /// Adds global logging buffer provider.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddGlobalBufferProvider(this ILoggingBuilder builder)
    {
        _ = Throw.IfNull(builder);

        builder.Services.TryAddSingleton<GlobalBufferProvider>();
        builder.Services.TryAddSingleton<ILoggingBufferProvider>(static sp => sp.GetRequiredService<GlobalBufferProvider>());

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerFactory, ExtendedLoggerFactory>());

        builder.Services.TryAddSingleton<GlobalBuffer>();
        builder.Services.TryAddSingleton<ILoggingBuffer>(static sp => sp.GetRequiredService<GlobalBuffer>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, GlobalBuffer>(static sp => sp.GetRequiredService<GlobalBuffer>()));

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
#endif
