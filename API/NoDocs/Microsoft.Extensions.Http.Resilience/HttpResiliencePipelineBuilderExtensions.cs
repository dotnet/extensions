// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Http.Resilience;

public static class HttpResiliencePipelineBuilderExtensions
{
    public static IHttpResiliencePipelineBuilder SelectPipelineByAuthority(this IHttpResiliencePipelineBuilder builder, DataClassification classification);
    public static IHttpResiliencePipelineBuilder SelectPipelineBy(this IHttpResiliencePipelineBuilder builder, Func<IServiceProvider, Func<HttpRequestMessage, string>> selectorFactory);
}
