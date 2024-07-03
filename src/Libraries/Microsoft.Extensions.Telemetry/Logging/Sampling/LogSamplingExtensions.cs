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
public static class LogSamplingExtensions
{
    /// <summary>
    /// Enable log sampling.
    /// </summary>
    /// <param name="builder">An instance of <see cref="ILoggingBuilder"/> to enable sampling in.</param>
    /// <param name="configure">A delegate to fine-tune the sampling.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILoggingBuilder EnableSampling(this ILoggingBuilder builder, Action<ILogSamplingBuilder> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        configure(new LogSamplingBuilder(builder.Services));

        return builder;
    }

    /// <summary>
    /// Add the built-in simple sampling filter.
    /// </summary>
    /// <param name="builder">An instance of <see cref="ILogSamplingBuilder"/> to set the simple sampling in.</param>
    /// <param name="configure">A delegate to fine-tune the sampling.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILogSamplingBuilder EnableSimpleSamplingFilter(this ILogSamplingBuilder builder, Action<SamplingFilterOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        builder.Services
            .Configure(configure)
            .TryAddActivatedSingleton<ILogSampler, SimpleSamplingFilter>();

        return builder;
    }

    /// <summary>
    /// Add the built-in simple buffering filter.
    /// </summary>
    /// <param name="builder">An instance of <see cref="ILogSamplingBuilder"/> to set the simple buffering filter in.</param>
    /// <param name="configure">A delegate to fine-tune the sampling.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILogSamplingBuilder EnableSimpleBufferingFilter(this ILogSamplingBuilder builder, Action<BufferingFilterOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        builder.Services
            .Configure(configure)
            .TryAddActivatedSingleton<ILogSampler, SimpleBufferingFilter>();

        return builder;
    }

    /// <summary>
    /// Add a log sampler.
    /// </summary>
    /// <typeparam name="T">A sampler type.</typeparam>
    /// <param name="builder">An instance of <see cref="ILogSamplingBuilder"/> to set the log sampler in.</param>
    /// <param name="configure">A delegate to fine-tune the sampling.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILogSamplingBuilder EnableSamplingFilter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this ILogSamplingBuilder builder,
        Action<SamplingFilterOptions> configure)
        where T : class, ILogSampler
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        builder.Services
            .Configure(configure)
            .TryAddActivatedSingleton<ILogSampler, T>();

        return builder;
    }
}
