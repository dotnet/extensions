// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Telemetry.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Extension methods to register http metering and metric enrichers with the service.
/// </summary>
public static class HttpMeteringExtensions
{
    /// <summary>
    /// Adds an enricher instance of <typeparamref name="T"/> to the <see cref="IServiceCollection"/> to enrich incoming request metrics.
    /// </summary>
    /// <typeparam name="T">Type of enricher.</typeparam>
    /// <param name="builder">The <see cref="IServiceCollection"/> to add the instance of <typeparamref name="T"/> to.</param>
    /// <returns>The <see cref="HttpMeteringBuilder"/> so that additional calls can be chained.</returns>
    public static HttpMeteringBuilder AddMetricEnricher<T>(this HttpMeteringBuilder builder)
        where T : class, IIncomingRequestMetricEnricher
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services.AddSingleton<IIncomingRequestMetricEnricher, T>();

        return builder;
    }

    /// <summary>
    /// Adds <paramref name="enricher"/> to the <see cref="IServiceCollection"/> to enrich incoming request metrics.
    /// </summary>
    /// <param name="builder">The <see cref="IServiceCollection"/> to add <paramref name="enricher"/> to.</param>
    /// <param name="enricher">The instance of <paramref name="enricher"/> to add to <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="HttpMeteringBuilder"/> so that additional calls can be chained.</returns>
    public static HttpMeteringBuilder AddMetricEnricher(
        this HttpMeteringBuilder builder,
        IIncomingRequestMetricEnricher enricher)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(enricher);

        _ = builder.Services.AddSingleton(enricher);

        return builder;
    }

    /// <summary>
    /// Adds a <see cref="HttpMeteringMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> so that additional calls can be chained.</returns>
    public static IApplicationBuilder UseHttpMetering(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HttpMeteringMiddleware>(Array.Empty<object>());
    }

    /// <summary>
    /// Adds incoming request metric auto-collection to <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">Collection of services.</param>
    /// <returns>Enriched collection of services.</returns>
    public static IServiceCollection AddHttpMetering(this IServiceCollection services) => services.AddHttpMetering(null);

    /// <summary>
    /// Adds incoming request metric auto-collection to <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">Collection of services.</param>
    /// <param name="build">Function to configure http metering options.</param>
    /// <returns>Enriched collection of services.</returns>
    public static IServiceCollection AddHttpMetering(this IServiceCollection services, Action<HttpMeteringBuilder>? build)
    {
        var builder = new HttpMeteringBuilder(services);

        // Invoke passed redact builder func if not null.
        build?.Invoke(builder);

        _ = services.RegisterMetering();
        services.TryAddSingleton<HttpMeteringMiddleware>();

        return services;
    }
}
