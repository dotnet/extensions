// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Extensions.Resilience.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Implementation of the <see cref="FallbackPolicyOptions{TResult}"/> for <see cref="HttpResponseMessage"/> results.
/// </summary>
public class HttpFallbackPolicyOptions : FallbackPolicyOptions<HttpResponseMessage>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpFallbackPolicyOptions"/> class.
    /// </summary>
    /// <remarks>
    /// By default the options is set to handle only transient failures,
    /// i.e. timeouts, 5xx responses and <see cref="HttpRequestException"/> exceptions.
    /// </remarks>
    public HttpFallbackPolicyOptions()
    {
        ShouldHandleResultAsError = result => HttpClientResiliencePredicates.IsTransientHttpFailure(result);
        ShouldHandleException = exp => HttpClientResiliencePredicates.IsTransientHttpException(exp);
    }
}
