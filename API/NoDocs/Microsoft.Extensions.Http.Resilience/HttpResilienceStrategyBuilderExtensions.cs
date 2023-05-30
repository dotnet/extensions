// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Http.Resilience;

public static class HttpResilienceStrategyBuilderExtensions
{
    public static IHttpResilienceStrategyBuilder SelectStrategyByAuthority(this IHttpResilienceStrategyBuilder builder, DataClassification classification);
    public static IHttpResilienceStrategyBuilder SelectStrategyBy(this IHttpResilienceStrategyBuilder builder, Func<IServiceProvider, Func<HttpRequestMessage, string>> selectorFactory);
}
