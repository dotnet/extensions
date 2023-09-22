// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Polly;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection;

[Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class ContextExtensions
{
    public static Context WithCallingRequestMessage(this Context context, HttpRequestMessage request);
}
