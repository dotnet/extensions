// Assembly 'Microsoft.Extensions.Resilience'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Http.Telemetry;
using Polly;

namespace Microsoft.Extensions.Resilience;

[Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class ResilienceContextExtensions
{
    public static void SetRequestMetadata(this ResilienceContext context, RequestMetadata requestMetadata);
    public static RequestMetadata? GetRequestMetadata(this ResilienceContext context);
}
