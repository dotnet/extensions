// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Enrichment;

public static class EnricherExtensions
{
    public static IServiceCollection AddLogEnricher<T>(this IServiceCollection services) where T : class, ILogEnricher;
    public static IServiceCollection AddLogEnricher(this IServiceCollection services, ILogEnricher enricher);
    public static IServiceCollection AddStaticLogEnricher<T>(this IServiceCollection services) where T : class, IStaticLogEnricher;
    public static IServiceCollection AddStaticLogEnricher(this IServiceCollection services, IStaticLogEnricher enricher);
}
