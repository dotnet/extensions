// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Net.Http;
using Polly;

namespace Microsoft.Extensions.Http.Resilience;

public static class HttpClientHedgingResiliencePredicates
{
    public static readonly Predicate<Exception> IsTransientHttpException;
    public static readonly Predicate<Outcome<HttpResponseMessage>> IsTransientHttpOutcome;
}
