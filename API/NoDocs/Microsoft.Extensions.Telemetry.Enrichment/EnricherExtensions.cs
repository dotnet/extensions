// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Telemetry.Enrichment;

public static class EnricherExtensions
{
    public static IServiceCollection AddLogEnricher<T>(this IServiceCollection services) where T : class, ILogEnricher;
    public static IServiceCollection AddLogEnricher(this IServiceCollection services, ILogEnricher enricher);
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddStaticLogEnricher<T>(this IServiceCollection services) where T : class, IStaticLogEnricher;
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddStaticLogEnricher(this IServiceCollection services, IStaticLogEnricher enricher);
}
