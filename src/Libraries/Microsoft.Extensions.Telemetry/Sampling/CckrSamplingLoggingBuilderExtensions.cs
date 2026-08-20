// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Diagnostics.Sampling;
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
    /// Adds the CCKR adaptive reservoir sampler to the logging infrastructure. Registers a single
    /// reservoir as both the pipeline's <see cref="LoggingSampler"/> and its <see cref="LogBuffer"/>.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="configure">An optional delegate to configure the reservoir.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILoggingBuilder AddCckrLogSampling(this ILoggingBuilder builder, Action<ReservoirSamplingConfig>? configure = null)
    {
        _ = Throw.IfNull(builder);

        var config = new ReservoirSamplingConfig();
        configure?.Invoke(config);

        // Register one reservoir instance and expose it through both pipeline seams. The DI container
        // owns its lifetime (and disposal); the LoggingSampler resolves the same instance.
        builder.Services.TryAddSingleton<CckrLogBuffer>(_ => new CckrLogBuffer(config, TimeProvider.System));
        builder.Services.TryAddSingleton<LogBuffer>(static sp => sp.GetRequiredService<CckrLogBuffer>());

        return builder.AddSampler<CckrLoggingSampler>();
    }
}
#endif
