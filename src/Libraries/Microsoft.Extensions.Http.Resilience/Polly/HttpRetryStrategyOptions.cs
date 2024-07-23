// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Resilience.Internal;
using Polly;
using Polly.Retry;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Implementation of the <see cref="RetryStrategyOptions{TResult}"/> for <see cref="HttpResponseMessage"/> results.
/// </summary>
public class HttpRetryStrategyOptions : RetryStrategyOptions<HttpResponseMessage>
{
    private bool _shouldRetryAfterHeader;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRetryStrategyOptions"/> class.
    /// </summary>
    /// <remarks>
    /// By default, the options are set to handle only transient failures,
    /// that is, timeouts, 5xx responses, and <see cref="HttpRequestException"/> exceptions.
    /// </remarks>
    public HttpRetryStrategyOptions()
    {
        ShouldHandle = args => new ValueTask<bool>(HttpClientResiliencePredicates.IsTransient(args.Outcome, args.Context.CancellationToken));
        BackoffType = DelayBackoffType.Exponential;
        ShouldRetryAfterHeader = true;
        UseJitter = true;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use the <c>Retry-After</c> header for the retry delays.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true"/>.
    /// </value>
    /// <remarks>
    /// If the property is set to <see langword="true"/> then the generator will resolve the delay
    /// based on the <c>Retry-After</c> header rules, otherwise it will return <see langword="null"/> and the retry strategy
    /// delay will generate the delay based on the configured options.
    /// </remarks>
    public bool ShouldRetryAfterHeader
    {
        get => _shouldRetryAfterHeader;
        set
        {
            _shouldRetryAfterHeader = value;

            if (_shouldRetryAfterHeader)
            {
                DelayGenerator = args => args.Outcome.Result switch
                {
                    HttpResponseMessage response when RetryAfterHelper.TryParse(response, TimeProvider.System, out var retryAfter) => new ValueTask<TimeSpan?>(retryAfter),
                    _ => default
                };
            }
            else
            {
                DelayGenerator = null;
            }
        }
    }
}
