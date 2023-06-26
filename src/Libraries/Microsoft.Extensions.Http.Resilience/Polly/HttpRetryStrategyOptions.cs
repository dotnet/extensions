﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Resilience.Internal;
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
        ShouldHandle = args => new ValueTask<bool>(HttpClientResiliencePredicates.IsTransientHttpOutcome(args.Outcome));
        BackoffType = RetryBackoffType.ExponentialWithJitter;
        ShouldRetryAfterHeader = true;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use the <c>Retry-After</c> header for the retry delays.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true"/>.
    /// </value>
    /// <remarks>
    /// If the property is set to <see langword="true"/> then the generator will resolve the delay
    /// based on the <c>Retry-After</c> header rules, otherwise it will return <see cref="RetryDelayArguments.DelayHint"/>
    /// that was suggested by the retry strategy.
    /// </remarks>
    public bool ShouldRetryAfterHeader
    {
        get => _shouldRetryAfterHeader;
        set
        {
            _shouldRetryAfterHeader = value;

            if (_shouldRetryAfterHeader)
            {
                RetryDelayGenerator = args => args.Outcome.Result switch
                {
                    HttpResponseMessage response when RetryAfterHelper.TryParse(response, TimeProvider.System, out var retryAfter) => new ValueTask<TimeSpan>(retryAfter),
                    _ => new ValueTask<TimeSpan>(args.Arguments.DelayHint)
                };
            }
            else
            {
                RetryDelayGenerator = null;
            }
        }
    }
}
