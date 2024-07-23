// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using Polly;
using Polly.CircuitBreaker;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Static predicates used within the current package.
/// </summary>
public static class HttpClientHedgingResiliencePredicates
{
    /// <summary>
    /// Determines whether an outcome should be treated by hedging as a transient failure.
    /// </summary>
    /// <param name="outcome">The outcome of the user-specified callback.</param>
    /// <returns><see langword="true"/> if outcome is transient, <see langword="false"/> if not.</returns>
    public static bool IsTransient(Outcome<HttpResponseMessage> outcome)
        => outcome switch
        {
            { Result: { } response } when HttpClientResiliencePredicates.IsTransientHttpFailure(response) => true,
            { Exception: { } exception } when IsTransientHttpException(exception) => true,
            _ => false,
        };

    /// <summary>
    /// Determines whether an <see cref="HttpResponseMessage"/> should be treated by hedging as a transient failure.
    /// </summary>
    /// <param name="outcome">The outcome of the user-specified callback.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> associated with the execution.</param>
    /// <returns><see langword="true"/> if outcome is transient, <see langword="false"/> if not.</returns>
    [Experimental(diagnosticId: DiagnosticIds.Experiments.Resilience, UrlFormat = DiagnosticIds.UrlFormat)]
    public static bool IsTransient(Outcome<HttpResponseMessage> outcome, CancellationToken cancellationToken)
        => HttpClientResiliencePredicates.IsHttpConnectionTimeout(outcome, cancellationToken)
           || IsTransient(outcome);

    /// <summary>
    /// Determines whether an exception should be treated by hedging as a transient failure.
    /// </summary>
    internal static bool IsTransientHttpException(Exception exception)
    {
        _ = Throw.IfNull(exception);

        return exception switch
        {
            BrokenCircuitException => true,
            _ => HttpClientResiliencePredicates.IsTransientHttpException(exception),
        };
    }
}
