// Assembly 'Microsoft.Extensions.Telemetry'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Telemetry.Enrichment;

public static class TracingEnricherExtensions
{
    public static TracerProviderBuilder AddTraceEnricher<T>(this TracerProviderBuilder builder) where T : class, ITraceEnricher;
    public static TracerProviderBuilder AddTraceEnricher(this TracerProviderBuilder builder, ITraceEnricher enricher);
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddTraceEnricher<T>(this IServiceCollection services) where T : class, ITraceEnricher;
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddTraceEnricher(this IServiceCollection services, ITraceEnricher enricher);
}
