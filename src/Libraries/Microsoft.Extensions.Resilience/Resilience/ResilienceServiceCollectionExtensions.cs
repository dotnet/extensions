// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using Polly.Telemetry;

namespace Microsoft.Extensions.Resilience;

/// <summary>
/// Extension class for the Service Collection DI container.
/// </summary>
[Experimental(diagnosticId: Experiments.Resilience, UrlFormat = Experiments.UrlFormat)]
public static class ResilienceServiceCollectionExtensions
{
    /// <summary>
    /// Adds resilience enrichers.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns>The input <paramref name="services"/>.</returns>
    /// <remarks>
    /// This method adds additional dimensions on top of the default ones that are built-in to the Polly library. These include:
    /// <list type="bullet">
    /// <item><description>
    /// Exception enrichment based on <see cref="IExceptionSummarizer"/>.</description>
    /// </item>
    /// <item><description>
    /// Result enrichment based on <see cref="ConfigureFailureResultContext"/> and <see cref="FailureResultContext"/>.</description>
    /// </item>
    /// <item><description>
    /// Request metadata enrichment based on <see cref="RequestMetadata"/>.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddResilienceEnrichment(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        // let's make this call idempotent by checking if ResilienceEnricher is already added
        if (services.Any(s => s.ServiceType == typeof(ResilienceMetricsEnricher)))
        {
            return services;
        }

        services.TryAddActivatedSingleton<ResilienceMetricsEnricher>();

        _ = services
            .AddOptions<TelemetryOptions>()
            .Configure<ResilienceMetricsEnricher>((options, enricher) => options.MeteringEnrichers.Add(enricher));

        return services;
    }

    /// <summary>
    /// Configures the failure result dimensions.
    /// </summary>
    /// <typeparam name="TResult">The type of the policy result.</typeparam>
    /// <param name="services">The services.</param>
    /// <param name="configure">The configure result dimensions.</param>
    /// <returns>The input <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection ConfigureFailureResultContext<TResult>(
       this IServiceCollection services,
       Func<TResult, FailureResultContext> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return services.Configure<FailureEventMetricsOptions>(options => options.ConfigureFailureResultContext(configure));
    }
}
