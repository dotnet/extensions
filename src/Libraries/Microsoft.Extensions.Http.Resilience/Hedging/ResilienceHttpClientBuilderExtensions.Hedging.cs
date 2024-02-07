// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Http.Resilience.Hedging.Internals;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Internal.Validators;
using Microsoft.Extensions.Http.Resilience.Routing.Internal;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for <see cref="IHttpClientBuilder"/>.
/// </summary>
public static partial class ResilienceHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds a standard hedging handler that wraps the execution of the request with a standard hedging mechanism.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="configure">Configures the routing strategy associated with this handler.</param>
    /// <returns>
    /// A <see cref="IStandardHedgingHandlerBuilder"/> instance that can be used to configure the standard hedging behavior.
    /// </returns>
    /// <remarks>
    /// The standard hedging uses a pool of circuit breakers to ensure that unhealthy endpoints are not hedged against.
    /// By default, the selection from pool is based on the URL Authority (scheme + host + port).
    /// It is recommended that you configure the way the strategies are selected by calling
    /// <see cref="StandardHedgingHandlerBuilderExtensions.SelectPipelineByAuthority(IStandardHedgingHandlerBuilder)"/>
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
    /// Adds a standard hedging handler that wraps the execution of the request with a standard hedging mechanism.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <returns>
    /// A <see cref="IStandardHedgingHandlerBuilder"/> instance that can be used to configure the standard hedging behavior.
    /// </returns>
    /// <remarks>
    /// The standard hedging uses a pool of circuit breakers to ensure that unhealthy endpoints are not hedged against.
    /// By default, the selection from pool is based on the URL Authority (scheme + host + port).
    /// It is recommended that you configure the way the strategies are selected by calling
    /// <see cref="StandardHedgingHandlerBuilderExtensions.SelectPipelineByAuthority(IStandardHedgingHandlerBuilder)"/>
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

        builder.Services.TryAddSingleton<Randomizer>();

        _ = builder.Services.AddOptionsWithValidateOnStart<HttpStandardHedgingResilienceOptions, HttpStandardHedgingResilienceOptionsValidator>(optionsName);
        _ = builder.Services.AddOptionsWithValidateOnStart<HttpStandardHedgingResilienceOptions, HttpStandardHedgingResilienceOptionsCustomValidator>(optionsName);
        _ = builder.Services.PostConfigure<HttpStandardHedgingResilienceOptions>(optionsName, options =>
        {
            options.Hedging.ActionGenerator = args =>
            {
                if (!args.PrimaryContext.Properties.TryGetValue(ResilienceKeys.RequestSnapshot, out var snapshot))
                {
                    Throw.InvalidOperationException("Request message snapshot is not attached to the resilience context.");
                }

                var requestMessage = snapshot.CreateRequestMessage();

                // The secondary request message should use the action resilience context
                requestMessage.SetResilienceContext(args.ActionContext);

                // replace the request message
                args.ActionContext.Properties.Set(ResilienceKeys.RequestMessage, requestMessage);

                if (args.PrimaryContext.Properties.TryGetValue(ResilienceKeys.RoutingStrategy, out var routingPipeline))
                {
                    if (!routingPipeline.TryGetNextRoute(out var route))
                    {
                        // no routes left, stop hedging
                        return null;
                    }

                    requestMessage.RequestUri = requestMessage.RequestUri!.ReplaceHost(route);
                }

                return () => args.Callback(args.ActionContext);
            };
        });

        // configure outer handler
        var outerHandler = builder.AddResilienceHandler(HedgingConstants.HandlerPostfix, (builder, context) =>
        {
            var options = context.GetOptions<HttpStandardHedgingResilienceOptions>(optionsName);
            context.EnableReloads<HttpStandardHedgingResilienceOptions>(optionsName);
            var routingOptions = context.GetOptions<RequestRoutingOptions>(routingBuilder.Name);

            _ = builder
                .AddStrategy(_ => new RoutingResilienceStrategy(routingOptions.RoutingStrategyProvider), new EmptyResilienceStrategyOptions())
                .AddStrategy(_ => new RequestMessageSnapshotStrategy(), new EmptyResilienceStrategyOptions())
                .AddTimeout(options.TotalRequestTimeout)
                .AddHedging(options.Hedging);
        });

        // configure inner handler
        var innerBuilder = builder.AddResilienceHandler(
            HedgingConstants.InnerHandlerPostfix,
            (builder, context) =>
            {
                var options = context.GetOptions<HttpStandardHedgingResilienceOptions>(optionsName);
                context.EnableReloads<HttpStandardHedgingResilienceOptions>(optionsName);

                _ = builder
                    .AddRateLimiter(options.Endpoint.RateLimiter)
                    .AddCircuitBreaker(options.Endpoint.CircuitBreaker)
                    .AddTimeout(options.Endpoint.Timeout);
            })
            .SelectPipelineByAuthority();

        return new StandardHedgingHandlerBuilder(builder.Name, builder.Services, routingBuilder);
    }

    private sealed record StandardHedgingHandlerBuilder(
        string Name,
        IServiceCollection Services,
        IRoutingStrategyBuilder RoutingStrategyBuilder) : IStandardHedgingHandlerBuilder;

    private sealed class EmptyResilienceStrategyOptions : ResilienceStrategyOptions
    {
    }
}
