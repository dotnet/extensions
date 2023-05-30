// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Telemetry.Enrichment;

public static class EnricherExtensions
{
    public static IServiceCollection AddLogEnricher<T>(this IServiceCollection services) where T : class, ILogEnricher;
    public static IServiceCollection AddLogEnricher(this IServiceCollection services, ILogEnricher enricher);
    public static IServiceCollection AddMetricEnricher<T>(this IServiceCollection services) where T : class, IMetricEnricher;
    public static IServiceCollection AddMetricEnricher(this IServiceCollection services, IMetricEnricher enricher);
}
