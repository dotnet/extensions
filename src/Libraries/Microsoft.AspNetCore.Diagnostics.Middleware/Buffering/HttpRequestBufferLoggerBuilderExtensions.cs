// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Diagnostics.Buffering;
using Microsoft.AspNetCore.Http;
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
/// Lets you register log buffers in a dependency injection container.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public static class HttpRequestBufferLoggerBuilderExtensions
{
    /// <summary>
    /// Adds HTTP request-aware buffer to the logging infrastructure. Matched logs will be buffered in
    /// a buffer specific to each HTTP request and can optionally be flushed and emitted during the request lifetime./>.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <param name="configuration">The <see cref="IConfiguration" /> to add.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ILoggingBuilder AddHttpRequestBuffering(this ILoggingBuilder builder, IConfiguration configuration)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configuration);

        return builder
            .AddHttpRequestBufferConfiguration(configuration)
            .AddHttpRequestBufferManager()
            .AddGlobalBuffer(configuration);
    }

    /// <summary>
    /// Adds HTTP request-aware buffering to the logging infrastructure. Matched logs will be buffered in
    /// a buffer specific to each HTTP request and can optionally be flushed and emitted during the request lifetime./>.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <param name="level">The log level (and below) to apply the buffer to.</param>
    /// <param name="configure">The buffer configuration options.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ILoggingBuilder AddHttpRequestBuffering(this ILoggingBuilder builder, LogLevel? level = null, Action<HttpRequestBufferOptions>? configure = null)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services
            .Configure<HttpRequestBufferOptions>(options => options.Rules.Add(new BufferFilterRule(null, level, null, null)))
            .Configure(configure ?? new Action<HttpRequestBufferOptions>(_ => { }));

        return builder
            .AddHttpRequestBufferManager()
            .AddGlobalBuffer(level);
    }

    /// <summary>
    /// Adds HTTP request buffer provider to the logging infrastructure.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    internal static ILoggingBuilder AddHttpRequestBufferManager(this ILoggingBuilder builder)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services.AddExtendedLoggerFeactory();

        builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.TryAddSingleton<HttpRequestBufferManager>();
        builder.Services.TryAddSingleton<IBufferManager>(static sp => sp.GetRequiredService<HttpRequestBufferManager>());
        builder.Services.TryAddSingleton<IHttpRequestBufferManager>(static sp => sp.GetRequiredService<HttpRequestBufferManager>());

        return builder;
    }

    /// <summary>
    /// Configures <see cref="HttpRequestBufferOptions" /> from an instance of <see cref="IConfiguration" />.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <param name="configuration">The <see cref="IConfiguration" /> to add.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    internal static ILoggingBuilder AddHttpRequestBufferConfiguration(this ILoggingBuilder builder, IConfiguration configuration)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services.AddSingleton<IConfigureOptions<HttpRequestBufferOptions>>(new HttpRequestBufferConfigureOptions(configuration));

        return builder;
    }
}
