// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Latency;
using Microsoft.Extensions.Http.Telemetry.Latency.Internal;
using Microsoft.Extensions.Http.Telemetry.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Telemetry.Latency;

/// <summary>
/// Extension methods to add http client latency telemetry.
/// </summary>
public static class HttpClientLatencyTelemetryExtensions
{
    /// <summary>
    /// Adds a <see cref="DelegatingHandler" /> to collect latency information and enrich outgoing request log for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures latency information collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <returns>
    /// <see cref="IServiceCollection" /> instance for chaining.
    /// </returns>
    public static IServiceCollection AddDefaultHttpClientLatencyTelemetry(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        _ = services.RegisterCheckpointNames(HttpCheckpoints.Checkpoints);
        _ = services.AddOptions<HttpClientLatencyTelemetryOptions>();
        _ = services.AddActivatedSingleton<HttpRequestLatencyListener>();
        _ = services.AddActivatedSingleton<HttpClientLatencyContext>();
        _ = services.AddTransient<HttpLatencyTelemetryHandler>();
        _ = services.AddHttpClientLogEnricher<HttpClientLatencyLogEnricher>();

        return services.ConfigureAll<HttpClientFactoryOptions>(
            httpClientOptions =>
            {
                httpClientOptions
                .HttpMessageHandlerBuilderActions.Add(httpMessageHandlerBuilder =>
                {
                    var handler = httpMessageHandlerBuilder.Services.GetRequiredService<HttpLatencyTelemetryHandler>();
                    httpMessageHandlerBuilder.AdditionalHandlers.Add(handler);
                });
            });
    }

    /// <summary>
    /// Adds a <see cref="DelegatingHandler" /> to collect latency information and enrich outgoing request log for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request logs auto collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="HttpClientLatencyTelemetryOptions"/>.</param>
    /// <returns>
    /// <see cref="IServiceCollection" /> instance for chaining.
    /// </returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(LoggingOptions))]
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IServiceCollection AddDefaultHttpClientLatencyTelemetry(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(section);

        _ = services
            .Configure<HttpClientLatencyTelemetryOptions>(section);

        return services.AddDefaultHttpClientLatencyTelemetry();
    }

    /// <summary>
    /// Adds a <see cref="DelegatingHandler" /> to collect latency information and enrich outgoing request log for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request logs auto collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <param name="configure">The delegate to configure <see cref="HttpClientLatencyTelemetryOptions"/> with.</param>
    /// <returns>
    /// <see cref="IServiceCollection" /> instance for chaining.
    /// </returns>
    public static IServiceCollection AddDefaultHttpClientLatencyTelemetry(this IServiceCollection services, Action<HttpClientLatencyTelemetryOptions> configure)
    {
        _ = Throw.IfNull(configure);

        _ = services
            .Configure(configure);

        return services.AddDefaultHttpClientLatencyTelemetry();
    }
}
