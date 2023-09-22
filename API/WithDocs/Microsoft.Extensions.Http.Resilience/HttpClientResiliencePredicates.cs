// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Net.Http;
using Polly;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Provides static predicates used within the current package.
/// </summary>
public static class HttpClientResiliencePredicates
{
    /// <summary>
    /// Determines whether an exception should be treated by resilience strategies as a transient failure.
    /// </summary>
    public static readonly Predicate<Exception> IsTransientHttpException;

    /// <summary>
    /// Determines whether a response contains a transient failure.
    /// </summary>
    /// <remarks> The current handling implementation uses approach proposed by Polly:
    /// <see href="https://github.com/App-vNext/Polly.Extensions.Http/blob/master/src/Polly.Extensions.Http/HttpPolicyExtensions.cs" />.
    /// </remarks>
    public static readonly Predicate<HttpResponseMessage> IsTransientHttpFailure;

    /// <summary>
    /// Determines whether an outcome should be treated by resilience strategies as a transient failure.
    /// </summary>
    public static readonly Predicate<Outcome<HttpResponseMessage>> IsTransientHttpOutcome;
}
