// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading.Tasks;
using Polly.CircuitBreaker;
using Polly.Hedging;
using Polly.Timeout;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Implementation of the <see cref="HedgingStrategyOptions{TResult}"/> class for <see cref="HttpResponseMessage"/> results.
/// </summary>
public class HttpHedgingStrategyOptions : HedgingStrategyOptions<HttpResponseMessage>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpHedgingStrategyOptions"/> class.
    /// </summary>
    /// <remarks>
    /// By default, the options are configured to handle only transient failures.
    /// Specifically, this includes HTTP status codes 408, 429, 500 and above, 
    /// as well as <see cref="HttpRequestException"/>, <see cref="BrokenCircuitException"/>, and <see cref="TimeoutRejectedException"/> exceptions.
    /// </remarks>
    public HttpHedgingStrategyOptions()
    {
        ShouldHandle = args => new ValueTask<bool>(HttpClientHedgingResiliencePredicates.IsTransient(args.Outcome, args.Context.CancellationToken));
    }
}
