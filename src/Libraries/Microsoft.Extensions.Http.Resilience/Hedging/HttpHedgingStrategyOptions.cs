// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading.Tasks;
using Polly.Hedging;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Implementation of the <see cref="HedgingStrategyOptions{TResult}"/> for <see cref="HttpResponseMessage"/> results.
/// </summary>
public class HttpHedgingStrategyOptions : HedgingStrategyOptions<HttpResponseMessage>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpHedgingStrategyOptions"/> class.
    /// </summary>
    /// <remarks>
    /// By default the options is set to handle only transient failures,
    /// i.e. timeouts, 5xx responses and <see cref="HttpRequestException"/> exceptions.
    /// </remarks>
    public HttpHedgingStrategyOptions()
    {
        ShouldHandle = args => new ValueTask<bool>(HttpClientHedgingResiliencePredicates.IsTransient(args));
    }
}
