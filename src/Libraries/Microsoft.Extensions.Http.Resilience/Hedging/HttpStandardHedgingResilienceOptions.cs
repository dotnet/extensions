// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Options for resilient pipeline of policies for usage in hedging HTTP scenarios. It is using 5 chained layers in this order (from the outermost to the innermost):
/// Total Request Timeout -> Hedging -> Bulkhead (per endpoint) -> Circuit Breaker (per endpoint) -> Attempt Timeout (per endpoint).
/// </summary>
/// /// <remarks>
/// The configuration of each policy is initialized with the default options per type. The request goes through these policies:
///
/// 1. Total request timeout policy applies an overall timeout to the execution, ensuring that the request including hedging attempts does not exceed the configured limit.
/// 2. The hedging policy executes the requests against multiple endpoints in case the dependency is slow or returns a transient error.
/// 3. The bulkhead policy limits the maximum number of concurrent requests being send to the dependency.
/// 4. The circuit breaker blocks the execution if too many direct failures or timeouts are detected.
/// 5. The attempt timeout policy limits each request attempt duration and throws if its exceeded.
///
/// The last three policies are assigned to each individual endpoint. The selection of endpoint can be customized by
/// <see cref="StandardHedgingHandlerBuilderExtensions.SelectPipelineByAuthority(IStandardHedgingHandlerBuilder, DataClassification)"/> or
/// <see cref="StandardHedgingHandlerBuilderExtensions.SelectPipelineBy(IStandardHedgingHandlerBuilder, System.Func{System.IServiceProvider, PipelineKeySelector})"/> extensions.
///
/// By default, the endpoint is selected by authority (scheme + host + port).
/// </remarks>
public class HttpStandardHedgingResilienceOptions
{
    /// <summary>
    /// Gets or sets the timeout policy options for the total timeout applied on the request execution.
    /// </summary>
    /// <remarks>
    /// By default it is initialized with a unique instance of <see cref="HttpTimeoutPolicyOptions"/>
    /// using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpTimeoutPolicyOptions TotalRequestTimeoutOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the hedging policy options.
    /// </summary>
    /// <remarks>
    /// By default it is initialized with a unique instance of <see cref="HttpHedgingPolicyOptions"/> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpHedgingPolicyOptions HedgingOptions { get; set; } = new();

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
