// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// Extensions for configuring logging sampling.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public static class SamplingLoggerBuilderExtensions
{
    /// <summary>
    /// Adds Trace-based logging sampling to the logging infrastructure. Sampling decisions
    /// for logs match exactly the sampling decisions for the underlying <see cref="System.Diagnostics.Activity"/>.
    /// </summary>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <remarks>Please configure Tracing Sampling separately as part of OpenTelemetry .NET.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ILoggingBuilder AddTraceBasedSampling(this ILoggingBuilder builder)
    {
        _ = Throw.IfNull(builder);

        return builder.AddSampler<TraceBasedSampler>();
    }

    /// <summary>
    /// Adds Ratio-based sampler to the logging infrastructure. Matched logs will be sampled
    /// according to the provided <paramref name="probability"/>.
    /// Higher the probability value, higher is the probability of a given log record to be sampled in.
    /// </summary>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <param name="probability">Probability from 0.0 to 1.0.</param>
    /// <param name="level">The log level (and below) to apply the sampler to.</param>
    /// <param name="category">The log category to apply the sampler to.</param>
    /// <param name="eventId">The event ID to apply the sampler to.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ILoggingBuilder AddRatioBasedSampler(
        this ILoggingBuilder builder,
        double probability,
        LogLevel? level = null,
        string? category = null,
        EventId? eventId = null)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfOutOfRange(probability, 0, 1, nameof(probability));

        return builder.AddSampler(new RatioBasedSampler(probability, level, category, eventId));
    }

    /// <summary>
    /// Adds a lambda logging sampler to the logging infrastructure.
    /// </summary>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <param name="samplingDecision">The delegate to be used to decide what to sample.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="samplingDecision"/> is <see langword="null"/>.</exception>
    public static ILoggingBuilder AddSampler(this ILoggingBuilder builder, Func<SamplingParameters, bool> samplingDecision)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(samplingDecision);

        return builder.AddSampler(new FuncBasedSampler(samplingDecision));
    }

    /// <summary>
    /// Adds a logging sampler type to the logging infrastructure.
    /// </summary>
    /// <typeparam name="T">Logging sampler type.</typeparam>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ILoggingBuilder AddSampler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this ILoggingBuilder builder)
        where T : LoggerSampler
    {
        _ = Throw.IfNull(builder);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerFactory, ExtendedLoggerFactory>());
        _ = builder.Services.AddSingleton<LoggerSampler, T>();

        return builder;
    }

    /// <summary>
    /// Adds a logging sampler instance to the logging infrastructure.
    /// </summary>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <param name="sampler">The sampler instance to add.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="sampler"/> is <see langword="null"/>.</exception>    
    public static ILoggingBuilder AddSampler(this ILoggingBuilder builder, LoggerSampler sampler)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(sampler);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerFactory, ExtendedLoggerFactory>());
        _ = builder.Services.AddSingleton(sampler);

        return builder;
    }
}
