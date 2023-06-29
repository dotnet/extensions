// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
#if !NETFRAMEWORK
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Telemetry.Metering.Internal;
#endif
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Shared.Collections;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Telemetry.Metering;

/// <summary>
/// Extension methods for HttpClient.Metering package. />.
/// </summary>
/// <seealso cref="DelegatingHandler" />
public static class HttpClientMeteringExtensions
{
#if !NETFRAMEWORK
    /// <summary>
    /// Adds Http client diagnostics listener to capture metrics for requests from all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request metrics auto collection
    /// globally for all http clients regardless of how the http client is created.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <returns>
    /// <see cref="IServiceCollection" /> instance for chaining.
    /// </returns>
    [Experimental(diagnosticId: "TBD", UrlFormat = WarningDefinitions.SharedUrlFormat)]
    public static IServiceCollection AddHttpClientMeteringForAllHttpClients(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        _ = services
            .RegisterMetering()
            .AddOutgoingRequestContext();

        services.TryAddSingleton(sp =>
        {
            var meter = sp.GetRequiredService<Meter<HttpMeteringHandler>>();
            var outgoingRequestMetricEnrichers = sp.GetService<IEnumerable<IOutgoingRequestMetricEnricher>>().EmptyIfNull();
            var requestMetadataContext = sp.GetService<IOutgoingRequestContext>();
            var downstreamDependencyMetadataManager = sp.GetService<IDownstreamDependencyMetadataManager>();
            return new HttpMeteringHandler(meter, outgoingRequestMetricEnrichers, requestMetadataContext, downstreamDependencyMetadataManager);
        });

        services.TryAddSingleton<HttpClientRequestAdapter>();
        services.TryAddSingleton<HttpClientDiagnosticObserver>();
        services.TryAddActivatedSingleton<HttpClientMeteringListener>();

        return services;
    }
#endif

    /// <summary>
    /// Adds a <see cref="DelegatingHandler" /> to collect and emit metrics for outgoing requests from all http clients created using IHttpClientFactory.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request metrics auto collection
    /// for all http clients created using IHttpClientFactory.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <returns>
    /// <see cref="IServiceCollection" /> instance for chaining.
    /// </returns>
    public static IServiceCollection AddDefaultHttpClientMetering(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        return services
            .RegisterMetering()
            .AddOutgoingRequestContext()
            .ConfigureAll<HttpClientFactoryOptions>(
            httpClientOptions =>
            {
                httpClientOptions
                .HttpMessageHandlerBuilderActions.Add(httpMessageHandlerBuilder =>
                {
                    var meter = httpMessageHandlerBuilder.Services.GetRequiredService<Meter<HttpMeteringHandler>>();
                    var outgoingRequestMetricEnrichers = httpMessageHandlerBuilder.Services.GetService<IEnumerable<IOutgoingRequestMetricEnricher>>().EmptyIfNull();
                    var requestMetadataContext = httpMessageHandlerBuilder.Services.GetService<IOutgoingRequestContext>();
                    var downstreamDependencyMetadataManager = httpMessageHandlerBuilder.Services.GetService<IDownstreamDependencyMetadataManager>();
                    httpMessageHandlerBuilder.AdditionalHandlers.Add(new HttpMeteringHandler(meter, outgoingRequestMetricEnrichers, requestMetadataContext, downstreamDependencyMetadataManager));
                });
            });
    }

    /// <summary>
    /// Adds a <see cref="DelegatingHandler" /> to collect and emit metrics for outgoing requests.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    public static IHttpClientBuilder AddHttpClientMetering(this IHttpClientBuilder builder)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services
            .RegisterMetering()
            .AddOutgoingRequestContext();
        return builder.AddHttpMessageHandler(services =>
        {
            var meter = services.GetRequiredService<Meter<HttpMeteringHandler>>();
            var outgoingRequestMetricEnrichers = services.GetService<IEnumerable<IOutgoingRequestMetricEnricher>>().EmptyIfNull();
            var requestMetadataContext = services.GetService<IOutgoingRequestContext>();
            var downstreamDependencyMetadataManager = services.GetService<IDownstreamDependencyMetadataManager>();

            return new HttpMeteringHandler(meter, outgoingRequestMetricEnrichers, requestMetadataContext!, downstreamDependencyMetadataManager);
        });
    }

    /// <summary>
    /// Adds an enricher instance of <typeparamref name="T"/> to the <see cref="IServiceCollection"/> to enrich outgoing request metrics.
    /// </summary>
    /// <typeparam name="T">Type of enricher.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the instance of <typeparamref name="T"/> to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddOutgoingRequestMetricEnricher<T>(this IServiceCollection services)
        where T : class, IOutgoingRequestMetricEnricher
    {
        _ = Throw.IfNull(services);

        _ = services.AddSingleton<IOutgoingRequestMetricEnricher, T>();

        return services;
    }

    /// <summary>
    /// Adds <paramref name="enricher"/> to the <see cref="IServiceCollection"/> to enrich outgoing request metrics.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add <paramref name="enricher"/> to.</param>
    /// <param name="enricher">The instance of <paramref name="enricher"/> to add to <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddOutgoingRequestMetricEnricher(
        this IServiceCollection services,
        IOutgoingRequestMetricEnricher enricher)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(enricher);

        _ = services.AddSingleton(enricher);

        return services;
    }
}
