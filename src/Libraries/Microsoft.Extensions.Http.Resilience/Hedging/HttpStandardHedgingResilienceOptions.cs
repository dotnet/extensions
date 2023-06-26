// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Http.Resilience.Internal.Validators;
using Microsoft.Extensions.Options.Validation;

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
/// <item>Total request timeout strategy applies an overall timeout to the execution, ensuring that the request including hedging attempts does not exceed the configured limit.</item>
/// <item>The hedging strategy executes the requests against multiple endpoints in case the dependency is slow or returns a transient error.</item>
/// <item>The bulkhead policy limits the maximum number of concurrent requests being send to the dependency.</item>
/// <item>The circuit breaker blocks the execution if too many direct failures or timeouts are detected.</item>
/// <item>The attempt timeout strategy limits each request attempt duration and throws if its exceeded.</item>
/// </list>
/// </para>
/// The last three strategies are assigned to each individual endpoint. The selection of endpoint can be customized by
/// <see cref="StandardHedgingHandlerBuilderExtensions.SelectStrategyByAuthority(IStandardHedgingHandlerBuilder, DataClassification)"/> or
/// <see cref="StandardHedgingHandlerBuilderExtensions.SelectStrategyBy(IStandardHedgingHandlerBuilder, Func{IServiceProvider, Func{System.Net.Http.HttpRequestMessage, string}})"/> extensions.
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
    /// By default it is initialized with a unique instance of <see cref="HttpTimeoutStrategyOptions"/>
    /// using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpTimeoutStrategyOptions TotalRequestTimeoutOptions { get; set; } = new HttpTimeoutStrategyOptions
    {
        StrategyName = StandardHedgingStrategyNames.TotalRequestTimeout,
        Timeout = TimeSpan.FromSeconds(30)
    };

    /// <summary>
    /// Gets or sets the hedging strategy options.
    /// </summary>
    /// <remarks>
    /// By default it is initialized with a unique instance of <see cref="HttpHedgingStrategyOptions"/> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpHedgingStrategyOptions HedgingOptions { get; set; } = new HttpHedgingStrategyOptions
    {
        StrategyName = StandardHedgingStrategyNames.Hedging
    };

    /// <summary>
    /// Gets or sets the hedging endpoint options.
    /// </summary>
    /// <remarks>
    /// By default it is initialized with a unique instance of <see cref="HedgingEndpointOptions"/> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HedgingEndpointOptions EndpointOptions { get; set; } = new HedgingEndpointOptions();
}
