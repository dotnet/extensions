// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Extension methods to register http metering and metric enrichers with the service.
/// </summary>
public static class HttpMeteringExtensions
{
    /// <summary>
    /// Adds an enricher instance of <typeparamref name="T" /> to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to enrich incoming request metrics.
    /// </summary>
    /// <typeparam name="T">Type of enricher.</typeparam>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the instance of <typeparamref name="T" /> to.</param>
    /// <returns>The <see cref="T:Microsoft.AspNetCore.Telemetry.HttpMeteringBuilder" /> so that additional calls can be chained.</returns>
    public static HttpMeteringBuilder AddMetricEnricher<T>(this HttpMeteringBuilder builder) where T : class, IIncomingRequestMetricEnricher;

    /// <summary>
    /// Adds <paramref name="enricher" /> to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to enrich incoming request metrics.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add <paramref name="enricher" /> to.</param>
    /// <param name="enricher">The instance of <paramref name="enricher" /> to add to <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</param>
    /// <returns>The <see cref="T:Microsoft.AspNetCore.Telemetry.HttpMeteringBuilder" /> so that additional calls can be chained.</returns>
    public static HttpMeteringBuilder AddMetricEnricher(this HttpMeteringBuilder builder, IIncomingRequestMetricEnricher enricher);

    /// <summary>
    /// Adds a <see cref="T:Microsoft.AspNetCore.Telemetry.Internal.HttpMeteringMiddleware" /> middleware to the specified <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" /> to add the middleware to.</param>
    /// <returns>The <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" /> so that additional calls can be chained.</returns>
    public static IApplicationBuilder UseHttpMetering(this IApplicationBuilder builder);

    /// <summary>
    /// Adds incoming request metric auto-collection to <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">Collection of services.</param>
    /// <returns>Enriched collection of services.</returns>
    public static IServiceCollection AddHttpMetering(this IServiceCollection services);

    /// <summary>
    /// Adds incoming request metric auto-collection to <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">Collection of services.</param>
    /// <param name="build">Function to configure http metering options.</param>
    /// <returns>Enriched collection of services.</returns>
    public static IServiceCollection AddHttpMetering(this IServiceCollection services, Action<HttpMeteringBuilder>? build);
}
