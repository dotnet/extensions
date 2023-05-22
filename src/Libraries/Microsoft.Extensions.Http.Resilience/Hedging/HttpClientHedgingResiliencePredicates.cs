// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;
using Polly.CircuitBreaker;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Static predicates used within the current package.
/// </summary>
public static class HttpClientHedgingResiliencePredicates
{
    /// <summary>
    /// Determines whether an exception should be treated by hedging as a transient failure.
    /// </summary>
    public static readonly Predicate<Exception> IsTransientHttpException = exception =>
    {
        _ = Throw.IfNull(exception);

        return exception switch
        {
            BrokenCircuitException => true,
            _ when HttpClientResiliencePredicates.IsTransientHttpException(exception) => true,
            _ => false,
        };
    };
}
