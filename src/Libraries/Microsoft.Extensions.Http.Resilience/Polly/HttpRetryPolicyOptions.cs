// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Extensions.Resilience.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Implementation of the <see cref="RetryPolicyOptions{TResult}"/> for <see cref="HttpResponseMessage"/> results.
/// </summary>
public class HttpRetryPolicyOptions : RetryPolicyOptions<HttpResponseMessage>
{
    private bool _shouldRetryAfterHeader;

    /// <summary>
    /// Gets or sets a value indicating whether to retry after header.
    /// </summary>
    /// <value>
    /// The default value is <c>false</c>.
    /// </value>
    /// <remarks>
    /// If the property is set to <c>true</c>, then the DelayGenerator will maximize
    /// based on the RetryAfter header rules, otherwise it will remain <see langword="null"/>.
    /// </remarks>
    public bool ShouldRetryAfterHeader
    {
        get => _shouldRetryAfterHeader;
        set
        {
            _shouldRetryAfterHeader = value;
            RetryDelayGenerator = _shouldRetryAfterHeader ? HttpClientResilienceGenerators.HandleRetryAfterHeader : null;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRetryPolicyOptions"/> class.
    /// </summary>
    /// <remarks>
    /// By default, the options are set to handle only transient failures,
    /// that is, timeouts, 5xx responses, and <see cref="HttpRequestException"/> exceptions.
    /// </remarks>
    public HttpRetryPolicyOptions()
    {
        ShouldHandleResultAsError = result => HttpClientResiliencePredicates.IsTransientHttpFailure(result);
        ShouldHandleException = exp => HttpClientResiliencePredicates.IsTransientHttpException(exp);
    }
}
