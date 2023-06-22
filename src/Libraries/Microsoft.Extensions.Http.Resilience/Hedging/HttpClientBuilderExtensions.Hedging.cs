// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Internal.Routing;
using Microsoft.Extensions.Http.Resilience.Internal.Validators;
using Microsoft.Extensions.Http.Resilience.Routing.Internal;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience;

public static partial class HttpClientBuilderExtensions
{
    private const string StandardHandlerPostfix = "standard-hedging";
    private const string StandardInnerHandlerPostfix = "standard-hedging-endpoint";

    /// <summary>
    /// Adds a standard hedging handler which wraps the execution of the request with a standard hedging mechanism.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="configure">Configures the routing strategy associated with this handler.</param>
    /// <returns>
    /// A <see cref="IStandardHedgingHandlerBuilder"/> builder that can be used to configure the standard hedging behavior.
    /// </returns>
    /// <remarks>
    /// The standard hedging uses a pipeline pool of circuit breakers to ensure that unhealthy endpoints are not hedged against.
    /// By default, the selection from pool is based on the URL Authority (scheme + host + port).
    ///
    /// It is recommended that you configure the way the pipelines are selected by calling 'SelectPipelineByAuthority' extensions on top of returned <see cref="IStandardHedgingHandlerBuilder"/>.
    ///
    /// See <see cref="HttpStandardHedgingResilienceOptions"/> for more details about the policies inside the pipeline.
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
    /// The standard hedging uses a pipeline pool of circuit breakers to ensure that unhealthy endpoints are not hedged against.
    /// By default, the selection from pool is based on the URL Authority (scheme + host + port).
    ///
    /// It is recommended that you configure the way the pipelines are selected by calling 'SelectPipelineByAuthority' extensions on top of returned <see cref="IStandardHedgingHandlerBuilder"/>.
    ///
    /// See <see cref="HttpStandardHedgingResilienceOptions"/> for more details about the policies inside the pipeline.
    /// </remarks>
    public static IStandardHedgingHandlerBuilder AddStandardHedgingHandler(this IHttpClientBuilder builder)
    {
        _ = Throw.IfNull(builder);

        var optionsName = builder.Name;
        var routingBuilder = new RoutingStrategyBuilder(builder.Name, builder.Services);
        _ = builder.Services.AddRequestCloner();

        // configure outer handler
        var outerHandler = builder.AddResilienceHandler(StandardHandlerPostfix);
        _ = outerHandler
            .AddRoutingPolicy(serviceProvider => serviceProvider.GetRoutingFactory(routingBuilder.Name))
            .AddRequestMessageSnapshotPolicy()
            .AddPolicy<HttpResponseMessage, HttpStandardHedgingResilienceOptions, HttpStandardHedgingResilienceOptionsCustomValidator>(
                optionsName,
                options => { },
                (builder, options, _) => builder
                    .AddTimeoutPolicy(StandardHedgingPolicyNames.TotalRequestTimeout, options.TotalRequestTimeoutOptions)
                    .AddHedgingPolicy(StandardHedgingPolicyNames.Hedging, CreateHedgedTaskProvider(outerHandler.PipelineName), options.HedgingOptions));

        // configure inner handler
        var innerBuilder = builder.AddResilienceHandler(StandardInnerHandlerPostfix);
        _ = innerBuilder
            .SelectPipelineByAuthority(new DataClassification("FIXME", 1))
            .AddPolicy<HttpResponseMessage, HttpStandardHedgingResilienceOptions, HttpStandardHedgingResilienceOptionsValidator>(
                optionsName,
                options => { },
                (builder, options, _) => builder
                    .AddBulkheadPolicy(StandardHedgingPolicyNames.Bulkhead, options.EndpointOptions.BulkheadOptions)
                    .AddCircuitBreakerPolicy(StandardHedgingPolicyNames.CircuitBreaker, options.EndpointOptions.CircuitBreakerOptions)
                    .AddTimeoutPolicy(StandardHedgingPolicyNames.AttemptTimeout, options.EndpointOptions.TimeoutOptions));

        return new StandardHedgingHandlerBuilder(builder.Name, builder.Services, routingBuilder, innerBuilder);
    }
}
