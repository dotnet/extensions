// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// Lets you register logging samplers in a dependency injection container.
/// </summary>
public static class LoggingSamplingExtensions
{
    /// <summary>
    /// Enable logging sampling.
    /// </summary>
    /// <param name="builder">An instance of <see cref="ILoggingBuilder"/> to enable sampling in.</param>
    /// <param name="configure">A delegate to fine-tune the sampling.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILoggingBuilder EnableSampling(this ILoggingBuilder builder, Action<ILoggingSamplingBuilder> configure)
    {
        configure(new LoggingSamplingBuilder(builder.Services));

        return builder;
    }

    /// <summary>
    /// Set the built-in simple sampler.
    /// </summary>
    /// <param name="builder">An instance of <see cref="ILoggingSamplingBuilder"/> to set the simple sampler in.</param>
    /// <param name="configure">A delegate to fine-tune the sampling.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILoggingSamplingBuilder SetSimpleSampler(this ILoggingSamplingBuilder builder, Action<LogSamplingOptions> configure)
    {
        builder.Services
            .Configure(configure)
            .TryAddActivatedSingleton<ILoggingSampler, SimpleSampler>();

        return builder;
    }

    /// <summary>
    /// Set a logging sampler.
    /// </summary>
    /// <typeparam name="T">A sampler type</typeparam>
    /// <param name="builder">An instance of <see cref="ILoggingSamplingBuilder"/> to set the logging sampler in.</param>
    /// <param name="configure">A delegate to fine-tune the sampling.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILoggingSamplingBuilder SetSampler<T>(this ILoggingSamplingBuilder builder, Action<LogSamplingOptions> configure)
        where T : class, ILoggingSampler
    {
        builder.Services
            .Configure(configure)
            .TryAddActivatedSingleton<ILoggingSampler, T>();

        return builder;
    }
}
