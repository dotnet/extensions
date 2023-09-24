// Assembly 'Microsoft.Extensions.Diagnostics.Extra'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceEnricherServiceCollectionExtensions
{
    public static IServiceCollection AddServiceLogEnricher(this IServiceCollection services);
    public static IServiceCollection AddServiceLogEnricher(this IServiceCollection services, Action<ServiceLogEnricherOptions> configure);
    public static IServiceCollection AddServiceLogEnricher(this IServiceCollection services, IConfigurationSection section);
}
