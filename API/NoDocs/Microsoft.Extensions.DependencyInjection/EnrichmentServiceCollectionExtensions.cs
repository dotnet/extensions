// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Microsoft.Extensions.DependencyInjection;

public static class EnrichmentServiceCollectionExtensions
{
    public static IServiceCollection AddLogEnricher<T>(this IServiceCollection services) where T : class, ILogEnricher;
    public static IServiceCollection AddLogEnricher(this IServiceCollection services, ILogEnricher enricher);
    public static IServiceCollection AddStaticLogEnricher<T>(this IServiceCollection services) where T : class, IStaticLogEnricher;
    public static IServiceCollection AddStaticLogEnricher(this IServiceCollection services, IStaticLogEnricher enricher);
}
