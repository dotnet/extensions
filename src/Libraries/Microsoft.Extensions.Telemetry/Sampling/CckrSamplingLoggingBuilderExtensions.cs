// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Registers the CCKR adaptive log sampler, which reuses the existing logging pipeline seams: the
/// <see cref="LoggingSampler"/> for the admit/drop decision and the <see cref="LogBuffer"/> for
/// holding admitted records and emitting them &#8212; weighted &#8212; at each period flush.
/// </summary>
public static class CckrSamplingLoggingBuilderExtensions
{
    /// <summary>
    /// Adds the CCKR adaptive reservoir sampler to the logging infrastructure with default options.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILoggingBuilder AddCckrLogSampling(this ILoggingBuilder builder)
    {
        _ = Throw.IfNull(builder);

        return builder.AddCckrLogSamplingCore();
    }

    /// <summary>
    /// Adds the CCKR adaptive reservoir sampler to the logging infrastructure.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="configure">The delegate used to configure CCKR.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILoggingBuilder AddCckrLogSampling(
        this ILoggingBuilder builder,
        Action<ReservoirSamplingConfig> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services.Configure(configure);

        return builder.AddCckrLogSamplingCore();
    }

    /// <summary>
    /// Adds the CCKR adaptive reservoir sampler to the logging infrastructure.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="section">The configuration section used to configure CCKR.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILoggingBuilder AddCckrLogSampling(
        this ILoggingBuilder builder,
        IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        _ = builder.Services.Configure<ReservoirSamplingConfig>(section);

        return builder.AddCckrLogSamplingCore();
    }

    private static ILoggingBuilder AddCckrLogSamplingCore(this ILoggingBuilder builder)
    {
        bool cckrRegistered = builder.Services.Any(static descriptor =>
            descriptor.ServiceType == typeof(CckrLogBuffer));
        bool logBufferRegistered = builder.Services.Any(static descriptor =>
            descriptor.ServiceType == typeof(LogBuffer));

        if (cckrRegistered)
        {
            return builder;
        }

        if (logBufferRegistered)
        {
            Throw.InvalidOperationException(
                "CCKR log sampling cannot be combined with another log buffer in the same logging pipeline.");
        }

        _ = builder.Services
            .AddOptionsWithValidateOnStart<ReservoirSamplingConfig, ReservoirSamplingConfigValidator>()
            .Services.AddOptionsWithValidateOnStart<ReservoirSamplingConfig, ReservoirSamplingConfigCustomValidator>();

        builder.Services.TryAddSingleton<CckrLogBuffer>(static services => new CckrLogBuffer(
            services.GetRequiredService<IOptionsMonitor<ReservoirSamplingConfig>>(),
            TimeProvider.System));
        builder.Services.TryAddSingleton<LogBuffer>(static sp => sp.GetRequiredService<CckrLogBuffer>());

        return builder.AddSampler<CckrLoggingSampler>();
    }
}
#endif
