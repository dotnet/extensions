// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using static System.Collections.Specialized.BitVector32;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// Extensions for configuring logging sampling.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public static class SamplingLoggerBuilderExtensions
{
    /// <summary>
    /// Adds Trace-based logging sampler to the logging infrastructure. Sampling decisions
    /// for logs match exactly the sampling decisions for the underlying <see cref="System.Diagnostics.Activity"/>.
    /// </summary>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <remarks>Please configure Tracing Sampling separately as part of OpenTelemetry .NET.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ILoggingBuilder AddTraceBasedSampler(this ILoggingBuilder builder)
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
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ILoggingBuilder AddRatioBasedSampler(this ILoggingBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        _ = builder.Services.AddOptions<RatioBasedSamplerOptions>().Bind(section);

        return builder.AddSampler<RatioBasedSampler>();
    }

    /// <summary>
    /// Adds Ratio-based sampler to the logging infrastructure. Matched logs will be sampled
    /// according to the provided <paramref name="probability"/>.
    /// Higher the probability value, higher is the probability of a given log record to be sampled in.
    /// </summary>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ILoggingBuilder AddRatioBasedSampler(
        this ILoggingBuilder builder,
        double probability,
        Func<string?, LogLevel, EventId?, bool>? filter)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(filter);

        _ = builder.ConfigureRatioBasedSamplerOptions(options => options.AddRule(probability, filter));

        return builder.AddSampler<RatioBasedSampler>();
    }

    public static RatioBasedSamplerOptions AddRule(
        this RatioBasedSamplerOptions options,
        double probability,
        Func<string?, LogLevel, EventId?, bool> filter)
    {
        return AddRule(options, probability, filter: filter);
    }

    private static ILoggingBuilder ConfigureRatioBasedSamplerOptions(
        this ILoggingBuilder builder,
        Action<RatioBasedSamplerOptions> configureOptions)
    {
        _ = builder.Services.Configure(configureOptions);
        return builder;
    }

    private static RatioBasedSamplerOptions AddRule(RatioBasedSamplerOptions options,
        double probability,
        string? category = null,
        LogLevel? level = null,
        EventId? eventId = null,
        Func<string?, LogLevel, EventId?, bool>? filter = null)
    {
        options.Rules.Add(new RatioBasedSamplerFilterRule(probability, category, level, eventId, filter));
        return options;
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
