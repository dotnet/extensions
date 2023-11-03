// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Http.Resilience.Internal.Validators;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Options for the pipeline of resilience strategies for usage in hedging HTTP scenarios.
/// </summary>
/// /// <remarks>
/// These options represents configuration for 5 chained layers in this order (from the outermost to the innermost):
/// <para>
/// Total Request Timeout -> Hedging -> Bulkhead (per endpoint) -> Circuit Breaker (per endpoint) -> Attempt Timeout (per endpoint).
/// </para>
/// The configuration of each resilience strategy is initialized with the default options per type. The request goes through these resilience strategies:
/// <para>
/// <list type="number">
/// <item><description>Total request timeout strategy applies an overall timeout to the execution,
/// ensuring that the request including hedging attempts does not exceed the configured limit.</description></item>
/// <item><description>The hedging strategy executes the requests against multiple endpoints in case the dependency is slow or returns a transient error.</description></item>
/// <item><description>The rate limiter pipeline limits the maximum number of requests being send to the dependency.</description></item>
/// <item><description>The circuit breaker blocks the execution if too many direct failures or timeouts are detected.</description></item>
/// <item><description>The attempt timeout strategy limits each request attempt duration and throws if its exceeded.</description></item>
/// </list>
/// </para>
/// The last three strategies are assigned to each individual endpoint. The selection of endpoint can be customized by
/// <see cref="StandardHedgingHandlerBuilderExtensions.SelectPipelineByAuthority(IStandardHedgingHandlerBuilder)"/> or
/// <see cref="StandardHedgingHandlerBuilderExtensions.SelectPipelineBy(IStandardHedgingHandlerBuilder, Func{IServiceProvider, Func{System.Net.Http.HttpRequestMessage, string}})"/> extensions.
/// <para>
/// By default, the endpoint is selected by authority (scheme + host + port).
/// </para>
/// </remarks>
public class HttpStandardHedgingResilienceOptions
{
    /// <summary>
    /// Gets or sets the timeout strategy options for the total timeout applied on the request execution.
    /// </summary>
    /// <remarks>
    /// By default, this property is initialized with a unique instance of <see cref="HttpTimeoutStrategyOptions"/>
    /// using default property values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpTimeoutStrategyOptions TotalRequestTimeout { get; set; } = new HttpTimeoutStrategyOptions
    {
        Name = StandardHedgingPipelineNames.TotalRequestTimeout
    };

    /// <summary>
    /// Gets or sets the hedging strategy options.
    /// </summary>
    /// <remarks>
    /// By default, this property is initialized with a unique instance of <see cref="HttpHedgingStrategyOptions"/> using default property values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpHedgingStrategyOptions Hedging { get; set; } = new HttpHedgingStrategyOptions
    {
        Name = StandardHedgingPipelineNames.Hedging
    };

    /// <summary>
    /// Gets or sets the hedging endpoint options.
    /// </summary>
    /// <remarks>
    /// By default, this property is initialized with a unique instance of <see cref="HedgingEndpointOptions"/> using default property values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HedgingEndpointOptions Endpoint { get; set; } = new HedgingEndpointOptions();
}
