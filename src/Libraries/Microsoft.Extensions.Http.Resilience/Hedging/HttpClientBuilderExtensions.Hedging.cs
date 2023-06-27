// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Internal.Validators;
using Microsoft.Extensions.Http.Resilience.Routing.Internal;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Http.Resilience;

public static partial class HttpClientBuilderExtensions
{
    internal const string StandardInnerHandlerPostfix = "standard-hedging-endpoint";

    private const string StandardHandlerPostfix = "standard-hedging";

    /// <summary>
    /// Adds a standard hedging handler which wraps the execution of the request with a standard hedging mechanism.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="configure">Configures the routing strategy associated with this handler.</param>
    /// <returns>
    /// A <see cref="IStandardHedgingHandlerBuilder"/> builder that can be used to configure the standard hedging behavior.
    /// </returns>
    /// <remarks>
    /// The standard hedging uses a pool of circuit breakers to ensure that unhealthy endpoints are not hedged against.
    /// By default, the selection from pool is based on the URL Authority (scheme + host + port).
    /// It is recommended that you configure the way the strategies are selected by calling
    /// <see cref="StandardHedgingHandlerBuilderExtensions.SelectStrategyByAuthority(IStandardHedgingHandlerBuilder, DataClassification)"/>
    /// extensions.
    /// <para>
    /// See <see cref="HttpStandardHedgingResilienceOptions"/> for more details about the used resilience strategies.
    /// </para>
    /// </remarks>
    public static IStandardHedgingHandlerBuilder AddStandardHedgingHandler(this IHttpClientBuilder builder, Action<IRoutingStrategyBuilder> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        var hedgingBuilder = builder.AddStandardHedgingHandler();

        configure(hedgingBuilder.RoutingStrategyBuilder);

        return hedgingBuilder;
    }

    /// <summary>
    /// Adds a standard hedging handler which wraps the execution of the request with a standard hedging mechanism.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <returns>
    /// A <see cref="IStandardHedgingHandlerBuilder"/> builder that can be used to configure the standard hedging behavior.
    /// </returns>
    /// <remarks>
    /// The standard hedging uses a pool of circuit breakers to ensure that unhealthy endpoints are not hedged against.
    /// By default, the selection from pool is based on the URL Authority (scheme + host + port).
    /// It is recommended that you configure the way the strategies are selected by calling
    /// <see cref="StandardHedgingHandlerBuilderExtensions.SelectStrategyByAuthority(IStandardHedgingHandlerBuilder, DataClassification)"/>
    /// extensions.
    /// <para>
    /// See <see cref="HttpStandardHedgingResilienceOptions"/> for more details about the used resilience strategies.
    /// </para>
    /// </remarks>
    public static IStandardHedgingHandlerBuilder AddStandardHedgingHandler(this IHttpClientBuilder builder)
    {
        _ = Throw.IfNull(builder);

        var optionsName = builder.Name;
        var routingBuilder = new RoutingStrategyBuilder(builder.Name, builder.Services);
        builder.Services.TryAddSingleton<IRequestCloner, RequestCloner>();
        _ = builder.Services.AddValidatedOptions<HttpStandardHedgingResilienceOptions, HttpStandardHedgingResilienceOptionsValidator>(optionsName);
        _ = builder.Services.AddValidatedOptions<HttpStandardHedgingResilienceOptions, HttpStandardHedgingResilienceOptionsCustomValidator>(optionsName);
        _ = builder.Services.PostConfigure<HttpStandardHedgingResilienceOptions>(optionsName, options =>
        {
            options.HedgingOptions.HedgingActionGenerator = args =>
            {
                if (!args.PrimaryContext.Properties.TryGetValue(ResilienceKeys.RequestSnapshot, out var snapshot))
                {
                    Throw.InvalidOperationException("Request message snapshot is not attached to the resilience context.");
                }

                if (!args.PrimaryContext.Properties.TryGetValue(ResilienceKeys.RoutingStrategy, out var routingStrategy))
                {
                    Throw.InvalidOperationException("Routing strategy is not attached to the resilience context.");
                }

                if (!routingStrategy.TryGetNextRoute(out var route))
                {
                    // no routes left, stop hedging
                    return null;
                }

                var requestMessage = snapshot.Create().ReplaceHost(route);

                // replace the request message
                args.ActionContext.Properties.Set(ResilienceKeys.RequestMessage, requestMessage);

                return () => args.Callback(args.ActionContext);
            };
        });

        // configure outer handler
        var outerHandler = builder.AddResilienceHandler(StandardHandlerPostfix, (builder, context) =>
        {
            var options = context.GetOptions<HttpStandardHedgingResilienceOptions>(optionsName);
            context.EnableReloads<HttpStandardHedgingResilienceOptions>(optionsName);

            _ = builder
                .AddStrategy(new RoutingResilienceStrategy(context.ServiceProvider.GetRoutingFactory(routingBuilder.Name)))
                .AddStrategy(new RequestMessageSnapshotStrategy(context.ServiceProvider.GetRequiredService<IRequestCloner>()))
                .AddTimeout(options.TotalRequestTimeoutOptions)
                .AddHedging(options.HedgingOptions);
        });

        // configure inner handler
        var innerBuilder = builder.AddResilienceHandler(
            StandardInnerHandlerPostfix,
            (builder, context) =>
            {
                var options = context.GetOptions<HttpStandardHedgingResilienceOptions>(optionsName);
                context.EnableReloads<HttpStandardHedgingResilienceOptions>(optionsName);

                _ = builder
                    .AddRateLimiter(options.EndpointOptions.RateLimiterOptions)
                    .AddAdvancedCircuitBreaker(options.EndpointOptions.CircuitBreakerOptions)
                    .AddTimeout(options.EndpointOptions.TimeoutOptions);
            })
            .SelectStrategyByAuthority(DataClassification.Unknown);

        return new StandardHedgingHandlerBuilder(builder.Name, builder.Services, routingBuilder);
    }

    private record StandardHedgingHandlerBuilder(
        string Name,
        IServiceCollection Services,
        IRoutingStrategyBuilder RoutingStrategyBuilder) : IStandardHedgingHandlerBuilder;
}
