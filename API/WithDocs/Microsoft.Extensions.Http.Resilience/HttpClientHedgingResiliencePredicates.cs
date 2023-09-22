// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Net.Http;
using Polly;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Static predicates used within the current package.
/// </summary>
public static class HttpClientHedgingResiliencePredicates
{
    /// <summary>
    /// Determines whether an exception should be treated by hedging as a transient failure.
    /// </summary>
    public static readonly Predicate<Exception> IsTransientHttpException;

    /// <summary>
    /// Determines whether an outcome should be treated by hedging as a transient failure.
    /// </summary>
    public static readonly Predicate<Outcome<HttpResponseMessage>> IsTransientHttpOutcome;
}
