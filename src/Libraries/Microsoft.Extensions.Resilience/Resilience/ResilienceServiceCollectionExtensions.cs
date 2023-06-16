// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Resilience.Internal;
using Microsoft.Shared.Diagnostics;
using Polly.Extensions.Telemetry;

namespace Microsoft.Extensions.Resilience;

/// <summary>
/// Extension class for the Service Collection DI container.
/// </summary>
public static class ResilienceServiceCollectionExtensions
{
    /// <summary>
    /// Adds resilience enrichers.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns>The input <paramref name="services"/>.</returns>
    /// <remarks>
    /// This method adds additional dimensions on top of the default ones that are built-in to Polly library. These include:
    /// <list type="bullet">
    /// <item>
    /// Exception enrichment based on <see cref="IExceptionSummarizer"/>.
    /// </item>
    /// <item>
    /// Result enrichment based on <see cref="ConfigureFailureResultContext"/> and <see cref="FailureResultContext"/>.
    /// </item>
    /// <item>
    /// Request metadata enrichment based on <see cref="RequestMetadata"/>.
    /// </item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddResilienceEnrichment(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        // let's make this call idempotent by checking if ResilienceEnricher is already added
        if (services.Any(s => s.ServiceType == typeof(ResilienceEnricher)))
        {
            return services;
        }

        services.TryAddActivatedSingleton<ResilienceEnricher>();

        _ = services
            .AddOptions<TelemetryResilienceStrategyOptions>()
            .Configure<IServiceProvider>((options, serviceProvider) =>
            {
                var enricher = serviceProvider.GetRequiredService<ResilienceEnricher>();
                options.Enrichers.Add(enricher.Enrich);
            });

        return services;
    }

    /// <summary>
    /// Configures the failure result dimensions.
    /// </summary>
    /// <typeparam name="TResult">The type of the policy result.</typeparam>
    /// <param name="services">The services.</param>
    /// <param name="configure">The configure result dimensions.</param>
    /// <returns>The input <paramref name="services"/>.</returns>
    public static IServiceCollection ConfigureFailureResultContext<TResult>(
       this IServiceCollection services,
       Func<TResult, FailureResultContext> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return services.Configure<FailureEventMetricsOptions>(options =>
        {
            options.Factories[typeof(TResult)] = value => configure((TResult)value);
        });
    }
}
