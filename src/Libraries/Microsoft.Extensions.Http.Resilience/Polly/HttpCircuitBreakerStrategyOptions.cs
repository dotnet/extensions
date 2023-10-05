// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading.Tasks;
using Polly.CircuitBreaker;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Implementation of the <see cref="CircuitBreakerStrategyOptions{TResult}"/> for <see cref="HttpResponseMessage"/> results.
/// </summary>
public class HttpCircuitBreakerStrategyOptions : CircuitBreakerStrategyOptions<HttpResponseMessage>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpCircuitBreakerStrategyOptions"/> class.
    /// </summary>
    /// <remarks>
    /// By default the options is set to handle only transient failures,
    /// that is, timeouts, 5xx responses, and <see cref="HttpRequestException"/> exceptions.
    /// </remarks>
    public HttpCircuitBreakerStrategyOptions()
    {
        ShouldHandle = args => new ValueTask<bool>(HttpClientResiliencePredicates.IsTransient(args.Outcome));
    }
}
