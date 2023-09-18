// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Enrichment;

public static class ServiceEnricherExtensions
{
    public static IServiceCollection AddServiceLogEnricher(this IServiceCollection services);
    public static IServiceCollection AddServiceLogEnricher(this IServiceCollection services, Action<ServiceLogEnricherOptions> configure);
    public static IServiceCollection AddServiceLogEnricher(this IServiceCollection services, IConfigurationSection section);
}
