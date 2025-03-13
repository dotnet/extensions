// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

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
/// Lets you register HTTP request log buffering in a dependency injection container.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public static class PerIncomingRequestLoggingBuilderExtensions
{
    /// <summary>
    /// Adds HTTP request log buffering to the logging infrastructure. 
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <param name="configuration">The <see cref="IConfiguration" /> to add.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Matched logs will be buffered in a buffer specific to each HTTP request and can optionally be flushed and emitted during the request lifetime.
    /// </remarks>
    public static ILoggingBuilder AddPerIncomingRequestBuffer(this ILoggingBuilder builder, IConfiguration configuration)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configuration);

        _ = builder.Services.AddSingleton<IConfigureOptions<PerRequestLogBufferingOptions>>(new PerIncomingRequestLogBufferingConfigureOptions(configuration));

        return builder
            .AddPerIncomingRequestBufferManager()
            .AddGlobalBuffer(configuration);
    }

    /// <summary>
    /// Adds HTTP request log buffering to the logging infrastructure.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <param name="configure">The buffer configuration delegate.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Matched logs will be buffered in a buffer specific to each HTTP request and can optionally be flushed and emitted during the request lifetime.
    /// </remarks>
    public static ILoggingBuilder AddPerIncomingRequestBuffer(this ILoggingBuilder builder, Action<PerRequestLogBufferingOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services.Configure(configure);

        PerRequestLogBufferingOptions options = new PerRequestLogBufferingOptions();
        configure(options);

        return builder
            .AddPerIncomingRequestBufferManager()
            .AddGlobalBuffer(opts => opts.Rules = options.Rules);
    }

    /// <summary>
    /// Adds HTTP request log buffering to the logging infrastructure.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" />.</param>
    /// <param name="logLevel">The log level (and below) to apply the buffer to.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Matched logs will be buffered in a buffer specific to each HTTP request and can optionally be flushed and emitted during the request lifetime.
    /// </remarks>
    public static ILoggingBuilder AddPerIncomingRequestBuffer(this ILoggingBuilder builder, LogLevel? logLevel = null)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services.Configure<PerRequestLogBufferingOptions>(options => options.Rules.Add(new LogBufferingFilterRule(logLevel: logLevel)));

        return builder
            .AddPerIncomingRequestBufferManager()
            .AddGlobalBuffer(logLevel);
    }

    internal static ILoggingBuilder AddPerIncomingRequestBufferManager(this ILoggingBuilder builder)
    {
        builder.Services.TryAddScoped<PerIncomingRequestBufferHolder>();
        builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.TryAddSingleton(sp =>
        {
            var globalBufferManager = sp.GetRequiredService<GlobalBufferManager>();
            return ActivatorUtilities.CreateInstance<PerIncomingRequestLogBufferManager>(sp, globalBufferManager);
        });
        builder.Services.TryAddSingleton<LogBuffer>(sp => sp.GetRequiredService<PerIncomingRequestLogBufferManager>());
        builder.Services.TryAddSingleton<PerRequestLogBuffer>(sp => sp.GetRequiredService<PerIncomingRequestLogBufferManager>());

        return builder;
    }
}

#endif
