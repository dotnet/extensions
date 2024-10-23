// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// Lets you register log samplers in a dependency injection container.
/// </summary>
public static class SamplingLoggerBuilderExtensions
{
    /// <summary>
    /// Adds a log sampler to the factory.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the sampler to.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddTraceBasedSampling(this ILoggingBuilder builder)
    {
        _ = Throw.IfNull(builder);

        return builder.AddSampler<TraceBasedSampler>();
    }

    /// <summary>
    /// Adds a log sampler to the factory.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the sampler to.</param>
    /// <param name="ratio">the ratio.</param>
    /// <param name="level">The level to filter.</param>
    /// <param name="category">The category to filter.</param>
    /// <param name="eventId">The event ID to filter.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddRatioBasedSampler(
        this ILoggingBuilder builder, double ratio,
        LogLevel? level = null,
        string? category = null,
        EventId? eventId = null)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services.AddSingleton<LoggerSampler, RatioBasedSampler>(
                sampler => new RatioBasedSampler(ratio, level, category, eventId));

        return builder;
    }

    /// <summary>
    /// Adds a log sampler to the factory.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the sampler to.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddSampler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this ILoggingBuilder builder)
        where T : LoggerSampler
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services.AddSingleton<LoggerSampler, T>();

        return builder;
    }

    /// <summary>
    /// Adds a log sampler to the factory.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the sampler to.</param>
    /// <param name="sampler">The sampler to be added.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddSampler(this ILoggingBuilder builder, LoggerSampler sampler)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services.AddSingleton(sampler);

        return builder;
    }

    /// <summary>
    /// Adds a log sampler to the factory.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the sampler to.</param>
    /// <param name="filter">The filter to be used to decide what to sample.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddSampler(this ILoggingBuilder builder, Func<SamplingParameters, bool> filter)
    {
        _ = Throw.IfNull(builder);

        return builder.AddSampler(new FuncBasedSampler(filter));
    }
}
