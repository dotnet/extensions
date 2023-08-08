// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Resilience;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Text;
using Polly;
using Polly.Registry;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Extensions for <see cref="IHttpClientBuilder"/>.
/// </summary>
public static partial class HttpClientBuilderExtensions
{
    /// <summary>
    /// Adds a resilience strategy handler that uses a named inline resilience strategy.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="strategyName">The custom identifier for the resilience strategy, used in the name of the strategy.</param>
    /// <param name="configure">The callback that configures the strategy.</param>
    /// <returns>The HTTP strategy builder instance.</returns>
    /// <remarks>
    /// The final strategy name is combination of <see cref="IHttpClientBuilder.Name"/> and <paramref name="strategyName"/>.
    /// Use strategy name identifier if your HTTP client contains multiple resilience handlers.
    /// </remarks>
    public static IHttpResilienceStrategyBuilder AddResilienceHandler(
        this IHttpClientBuilder builder,
        string strategyName,
        Action<CompositeStrategyBuilder<HttpResponseMessage>> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrEmpty(strategyName);
        _ = Throw.IfNull(configure);

        return builder.AddResilienceHandler(strategyName, ConfigureBuilder);

        void ConfigureBuilder(CompositeStrategyBuilder<HttpResponseMessage> builder, ResilienceHandlerContext context) => configure(builder);
    }

    /// <summary>
    /// Adds a resilience strategy handler that uses a named inline resilience strategy.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="strategyName">The custom identifier for the resilience strategy, used in the name of the strategy.</param>
    /// <param name="configure">The callback that configures the strategy.</param>
    /// <returns>The HTTP strategy builder instance.</returns>
    /// <remarks>
    /// The final strategy name is combination of <see cref="IHttpClientBuilder.Name"/> and <paramref name="strategyName"/>.
    /// Use strategy name identifier if your HTTP client contains multiple resilience handlers.
    /// </remarks>
    public static IHttpResilienceStrategyBuilder AddResilienceHandler(
        this IHttpClientBuilder builder,
        string strategyName,
        Action<CompositeStrategyBuilder<HttpResponseMessage>, ResilienceHandlerContext> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrEmpty(strategyName);
        _ = Throw.IfNull(configure);

        var strategyBuilder = builder.AddHttpResilienceStrategy(strategyName, configure);

        _ = builder.AddHttpMessageHandler(serviceProvider =>
        {
            var selector = CreateStrategySelector(serviceProvider, strategyBuilder.StrategyName);
            var provider = serviceProvider.GetRequiredService<ResilienceStrategyProvider<HttpKey>>();

            return new ResilienceHandler(selector);
        });

        return strategyBuilder;
    }

    private static Func<HttpRequestMessage, ResilienceStrategy<HttpResponseMessage>> CreateStrategySelector(IServiceProvider serviceProvider, string strategyName)
    {
        var resilienceProvider = serviceProvider.GetRequiredService<ResilienceStrategyProvider<HttpKey>>();
        var strategyKeyProvider = serviceProvider.GetStrategyKeyProvider(strategyName);

        if (strategyKeyProvider == null)
        {
            var strategy = resilienceProvider.GetStrategy<HttpResponseMessage>(new HttpKey(strategyName, string.Empty));
            return _ => strategy;
        }
        else
        {
            TouchStrategyKey(strategyKeyProvider);

            return request =>
            {
                var key = strategyKeyProvider(request);
                return resilienceProvider.GetStrategy<HttpResponseMessage>(new HttpKey(strategyName, key));
            };
        }
    }

    private static void TouchStrategyKey(Func<HttpRequestMessage, string> provider)
    {
        // this piece of code eagerly checks that the strategy key provider is correctly configured
        // combined with HttpClient auto-activation we can detect any issues on startup
#pragma warning disable S1075 // URIs should not be hardcoded - this URL is not used for any real request, nor in any telemetry
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:123");
#pragma warning restore S1075 // URIs should not be hardcoded
        _ = provider(request);
    }

    private static HttpResilienceStrategyBuilder AddHttpResilienceStrategy(
        this IHttpClientBuilder builder,
        string name,
        Action<CompositeStrategyBuilder<HttpResponseMessage>, ResilienceHandlerContext> configure)
    {
        var strategyName = StrategyNameHelper.GetName(builder.Name, name);
        var key = new HttpKey(strategyName, string.Empty);

        _ = builder.Services.AddResilienceStrategy<HttpKey, HttpResponseMessage>(key, (builder, context) => configure(builder, new ResilienceHandlerContext(context)));

        ConfigureHttpServices(builder.Services);

        return new(strategyName, builder.Services);
    }

    private static void ConfigureHttpServices(IServiceCollection services)
    {
        // don't add any new service if this method is called multiple times
        if (services.Contains(Marker.ServiceDescriptor))
        {
            return;
        }

        services.Add(Marker.ServiceDescriptor);

        // This code configure the multi-instance support of the registry
        _ = services.Configure<ResilienceStrategyRegistryOptions<HttpKey>>(options =>
        {
            options.BuilderNameFormatter = key => key.Name;
            options.InstanceNameFormatter = key => key.InstanceName;
            options.BuilderComparer = HttpKey.BuilderComparer;
        });

        _ = services
            .AddExceptionSummarizer(b => b.AddHttpProvider())
            .ConfigureFailureResultContext<HttpResponseMessage>((response) =>
            {
                if (response != null)
                {
                    return FailureResultContext.Create(
                        failureReason: ((int)response.StatusCode).ToInvariantString(),
                        additionalInformation: response.StatusCode.ToInvariantString());
                }

                return FailureResultContext.Create();
            });
    }

    private sealed class Marker
    {
        public static readonly ServiceDescriptor ServiceDescriptor = ServiceDescriptor.Singleton<Marker, Marker>();
    }

    private record HttpResilienceStrategyBuilder(string StrategyName, IServiceCollection Services) : IHttpResilienceStrategyBuilder;
}
