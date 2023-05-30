// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Telemetry.Enrichment;

public static class ProcessEnricherExtensions
{
    public static IServiceCollection AddProcessLogEnricher(this IServiceCollection services);
    public static IServiceCollection AddProcessLogEnricher(this IServiceCollection services, Action<ProcessLogEnricherOptions> configure);
    public static IServiceCollection AddProcessLogEnricher(this IServiceCollection services, IConfigurationSection section);
}
