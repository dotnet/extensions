// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Diagnostics.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    public static ILoggingBuilder AddHttpRequestBuffer(this ILoggingBuilder builder, IConfiguration configuration)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configuration);

        return builder
            .AddHttpRequestBufferConfiguration(configuration)
            .AddHttpRequestBufferProvider();
    }

    /// <summary>
    /// Adds HTTP request-aware buffer to the logging infrastructure. Matched logs will be buffered in
    /// a buffer specific to each HTTP request and can optionally be flushed and emitted during the request lifetime./>.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <param name="level">The log level (and below) to apply the buffer to.</param>
    /// <param name="configure">The buffer configuration options.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ILoggingBuilder AddHttpRequestBuffer(this ILoggingBuilder builder, LogLevel? level = null, Action<HttpRequestBufferOptions>? configure = null)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services
            .Configure<HttpRequestBufferOptions>(options => options.Rules.Add(new BufferFilterRule(null, level, null)))
            .Configure(configure ?? new Action<HttpRequestBufferOptions>(_ => { }));

        return builder.AddHttpRequestBufferProvider();
    }

    /// <summary>
    /// Adds HTTP request buffer provider to the logging infrastructure.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ILoggingBuilder AddHttpRequestBufferProvider(this ILoggingBuilder builder)
    {
        _ = Throw.IfNull(builder);

        builder.Services.TryAddScoped<HttpRequestBuffer>();
        builder.Services.TryAddScoped<ILoggingBuffer>(sp => sp.GetRequiredService<HttpRequestBuffer>());
        builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.TryAddActivatedSingleton<ILoggingBufferProvider, HttpRequestBufferProvider>();

        return builder.AddGlobalBufferProvider();
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
#endif
