// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using Microsoft.Shared.Diagnostics;
using Polly;
using Polly.Timeout;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Provides static predicates used within the current package.
/// </summary>
public static class HttpClientResiliencePredicates
{
    /// <summary>
    /// Determines whether an outcome should be treated by resilience strategies as a transient failure.
    /// </summary>
    /// <returns><see langword="true"/> if outcome is transient, <see langword="false"/> if not.</returns>
    public static bool IsTransient(Outcome<HttpResponseMessage> outcome) => outcome switch
    {
        { Result: { } response } when IsTransientHttpFailure(response) => true,
        { Exception: { } exception } when IsTransientHttpException(exception) => true,
        _ => false
    };

    /// <summary>
    /// Determines whether an exception should be treated by resilience strategies as a transient failure.
    /// </summary>
    internal static bool IsTransientHttpException(Exception exception)
    {
        _ = Throw.IfNull(exception);

        return exception is HttpRequestException ||
               exception is TimeoutRejectedException;
    }

    /// <summary>
    /// Determines whether a response contains a transient failure.
    /// </summary>
    /// <remarks> The current handling implementation uses approach proposed by Polly:
    /// <see href="https://github.com/App-vNext/Polly.Extensions.Http/blob/master/src/Polly.Extensions.Http/HttpPolicyExtensions.cs"/>.
    /// </remarks>
    internal static bool IsTransientHttpFailure(HttpResponseMessage response)
    {
        _ = Throw.IfNull(response);

        var statusCode = (int)response.StatusCode;

        return statusCode >= InternalServerErrorCode ||
            response.StatusCode == HttpStatusCode.RequestTimeout ||
            statusCode == TooManyRequests;

    }

    private const int InternalServerErrorCode = (int)HttpStatusCode.InternalServerError;

    private const int TooManyRequests = 429;
}
