// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using Microsoft.Shared.Diagnostics;
using Polly.Timeout;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Static predicates used within the current package.
/// </summary>
public static class HttpClientResiliencePredicates
{
    /// <summary>
    /// Determines whether an exception should be treated by policies as a transient failure.
    /// </summary>
    public static readonly Predicate<Exception> IsTransientHttpException = exception =>
    {
        _ = Throw.IfNull(exception);

        return exception is HttpRequestException ||
               exception is TimeoutRejectedException;
    };

    /// <summary>
    /// Determines whether a response contains a transient failure.
    /// </summary>
    /// <remarks> The current handling implementation uses approach proposed by Polly:
    /// <see href="https://github.com/App-vNext/Polly.Extensions.Http/blob/master/src/Polly.Extensions.Http/HttpPolicyExtensions.cs"/>.
    /// </remarks>
    public static readonly Predicate<HttpResponseMessage> IsTransientHttpFailure = response =>
    {
        _ = Throw.IfNull(response);

        var statusCode = (int)response.StatusCode;

        return statusCode >= InternalServerErrorCode ||
            response.StatusCode == HttpStatusCode.RequestTimeout ||
            statusCode == TooManyRequests;

    };

    private const int InternalServerErrorCode = (int)HttpStatusCode.InternalServerError;

    private const int TooManyRequests = 429;
}
