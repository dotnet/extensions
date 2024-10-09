// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Logging.Buffering;

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
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddGlobalBuffering(this ILoggingBuilder builder, Func<string?, EventId?, LogLevel?, bool> filter)
    {
        _ = Throw.IfNull(builder);

        return builder
            .AddGlobalBufferProvider()
            .AddBuffering(filter);
    }

    /// <summary>
    /// Adds global logging buffering.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the buffer to.</param>
    /// <param name="category">The category to filter.</param>
    /// <param name="filter">The filter to be used to decide what to buffer.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddGlobalBuffering(this ILoggingBuilder builder, string? category, Func<string?, EventId?, LogLevel?, bool> filter)
    {
        _ = Throw.IfNull(builder);

        return builder
            .AddGlobalBufferProvider()
            .AddBuffering(category, filter);
    }

    /// <summary>
    /// Adds global logging buffering.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the buffer to.</param>
    /// <param name="category">The category to filter.</param>
    /// <param name="eventId">The event ID to filter.</param>
    /// <param name="level">The level to filter.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddGlobalBuffering(this ILoggingBuilder builder, string? category, EventId? eventId, LogLevel? level)
    {
        _ = Throw.IfNull(builder);

        return builder
            .AddGlobalBufferProvider()
            .AddBuffering(category, eventId, level);
    }

    /// <summary>
    /// Adds global logging buffer provider.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the buffer to.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddGlobalBufferProvider(this ILoggingBuilder builder)
    {
        _ = Throw.IfNull(builder);

        builder.Services.AddActivatedSingleton<ILoggingBufferProvider, GlobalBufferProvider>();

        return builder;
    }

    /// <summary>
    /// Adds a log buffer to the factory.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the buffer to.</param>
    /// <param name="filter">The filter to be used to decide what to buffer.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddBuffering(this ILoggingBuilder builder, Func<string?, EventId?, LogLevel?, bool> filter)
    {
        _ = Throw.IfNull(builder);

        return builder.ConfigureFilter(options => options.AddFilter(null, filter));
    }

    /// <summary>
    /// Adds a log buffer to the factory.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the buffer to.</param>
    /// <param name="category">The category to filter.</param>
    /// <param name="filter">The filter to be used to decide what to buffer.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddBuffering(this ILoggingBuilder builder, string? category, Func<string?, EventId?, LogLevel?, bool> filter)
    {
        _ = Throw.IfNull(builder);

        return builder.ConfigureFilter(options => options.AddFilter(category, filter));
    }

    /// <summary>
    /// Adds a log buffer to the factory.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the buffer to.</param>
    /// <param name="category">The category to filter.</param>
    /// <param name="eventId">The event ID to filter.</param>
    /// <param name="level">The level to filter.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddBuffering(this ILoggingBuilder builder, string? category, EventId? eventId, LogLevel? level)
    {
        _ = Throw.IfNull(builder);

        return builder.ConfigureFilter(options => options.AddFilter(category, eventId, level));
    }

    /// <summary>
    /// Adds a log buffer to the factory.
    /// </summary>
    /// <returns>The <see cref="LoggerFilterOptions"/> so that additional calls can be chained.</returns>
    public static GlobalBufferingOptions AddFilter(this GlobalBufferingOptions options, string? category, EventId? eventId, LogLevel? level) =>
        AddRule(options, category, eventId, level);

    /// <summary>
    /// Adds a log buffer to the factory.
    /// </summary>
    /// <returns>The <see cref="LoggerFilterOptions"/> so that additional calls can be chained.</returns>
    public static GlobalBufferingOptions AddFilter(this GlobalBufferingOptions options, string? category, Func<string?, EventId?, LogLevel?, bool> filter) =>
        AddRule(options, category: category, filter: filter);

    private static ILoggingBuilder ConfigureFilter(this ILoggingBuilder builder, Action<GlobalBufferingOptions> configureOptions)
    {
        _ = builder.Services.Configure(configureOptions);

        return builder;
    }

    private static GlobalBufferingOptions AddRule(GlobalBufferingOptions options,
        string? category = null,
        EventId? eventId = null,
        LogLevel? level = null,
        Func<string?, EventId?, LogLevel?, bool>? filter = null)
    {
        _ = Throw.IfNull(options);

        options.Rules.Add(new Microsoft.Extensions.Diagnostics.Logging.Buffering.LoggerFilterRule(category, eventId, level, filter));
        return options;
    }
}
