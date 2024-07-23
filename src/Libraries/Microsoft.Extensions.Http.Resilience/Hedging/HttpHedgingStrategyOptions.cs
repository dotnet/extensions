// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading.Tasks;
using Polly.Hedging;

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
    /// By default, the options are set to handle only transient failures,
    /// that is, timeouts, 5xx responses, and <see cref="HttpRequestException"/> exceptions.
    /// </remarks>
    public HttpHedgingStrategyOptions()
    {
        ShouldHandle = args => new ValueTask<bool>(HttpClientHedgingResiliencePredicates.IsTransient(args.Outcome, args.Context.CancellationToken));
    }
}
