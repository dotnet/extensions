// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// Lets you register log buffers in a dependency injection container.
/// </summary>
public static class GlobalBufferingLoggerBuilderExtensions
{
    /// <summary>
    /// Adds global logging buffering.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the buffer to.</param>
    /// <param name="filter">The filter to be used to decide what to buffer.</param>
    /// <param name="options">Options for the buffering.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddGlobalBuffering(
        this ILoggingBuilder builder,
        Func<string?, EventId?, LogLevel?, bool> filter,
        Action<GlobalBufferingOptions>? options = null)
    {
        _ = Throw.IfNull(builder);

        return builder
            .AddGlobalBufferProvider()
            .ConfigureBuffering(filter, options);
    }

    /// <summary>
    /// Adds global logging buffer provider.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the buffer to.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddGlobalBufferProvider(this ILoggingBuilder builder)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services.AddActivatedSingleton<ILoggingBufferProvider, GlobalBufferProvider>();

        return builder;
    }

    /// <summary>
    /// Adds a log buffer to the factory.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the buffer to.</param>
    /// <param name="filter">The filter to be used to decide what to buffer.</param>
    /// <param name="configureOptions">The delegate to configure <see cref="GlobalBufferingOptions"/>.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder ConfigureBuffering(
        this ILoggingBuilder builder,
        Func<string?, EventId?, LogLevel?, bool> filter,
        Action<GlobalBufferingOptions>? configureOptions = null)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services.Configure(configureOptions ?? new Action<GlobalBufferingOptions>((_) => { }));
        _ = builder.Services.Configure<GlobalBufferingOptions>(opts => opts.AddFilter(filter));

        return builder;
    }

    /// <summary>
    /// Adds a log buffer to the factory.
    /// </summary>
    public static void AddFilter(
        this GlobalBufferingOptions options,
        Func<string?, EventId?, LogLevel?, bool> filter)
    {
        _ = Throw.IfNull(options);
        _ = Throw.IfNull(filter);

        options.Filter = filter;
    }
}
