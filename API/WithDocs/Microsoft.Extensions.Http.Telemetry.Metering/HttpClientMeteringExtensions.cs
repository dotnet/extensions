// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Telemetry.Metering;

/// <summary>
/// Extension methods for HttpClient.Metering package. /&gt;.
/// </summary>
/// <seealso cref="T:System.Net.Http.DelegatingHandler" />
public static class HttpClientMeteringExtensions
{
    /// <summary>
    /// Adds Http client diagnostics listener to capture metrics for requests from all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request metrics auto collection
    /// globally for all http clients regardless of how the http client is created.
    /// </remarks>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</param>
    /// <returns>
    /// <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> instance for chaining.
    /// </returns>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddHttpClientMeteringForAllHttpClients(this IServiceCollection services);

    /// <summary>
    /// Adds a <see cref="T:System.Net.Http.DelegatingHandler" /> to collect and emit metrics for outgoing requests from all http clients created using IHttpClientFactory.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request metrics auto collection
    /// for all http clients created using IHttpClientFactory.
    /// </remarks>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</param>
    /// <returns>
    /// <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> instance for chaining.
    /// </returns>
    public static IServiceCollection AddDefaultHttpClientMetering(this IServiceCollection services);

    /// <summary>
    /// Adds a <see cref="T:System.Net.Http.DelegatingHandler" /> to collect and emit metrics for outgoing requests.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.</param>
    /// <returns>
    /// An <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    public static IHttpClientBuilder AddHttpClientMetering(this IHttpClientBuilder builder);

    /// <summary>
    /// Adds an enricher instance of <typeparamref name="T" /> to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to enrich outgoing request metrics.
    /// </summary>
    /// <typeparam name="T">Type of enricher.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the instance of <typeparamref name="T" /> to.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection AddOutgoingRequestMetricEnricher<T>(this IServiceCollection services) where T : class, IOutgoingRequestMetricEnricher;

    /// <summary>
    /// Adds <paramref name="enricher" /> to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to enrich outgoing request metrics.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add <paramref name="enricher" /> to.</param>
    /// <param name="enricher">The instance of <paramref name="enricher" /> to add to <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection AddOutgoingRequestMetricEnricher(this IServiceCollection services, IOutgoingRequestMetricEnricher enricher);
}
