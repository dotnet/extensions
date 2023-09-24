// Assembly 'Microsoft.Extensions.Diagnostics.Extra'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Microsoft.Extensions.DependencyInjection;

public static class ProcessEnricherExtensions
{
    public static IServiceCollection AddProcessLogEnricher(this IServiceCollection services);
    public static IServiceCollection AddProcessLogEnricher(this IServiceCollection services, Action<ProcessLogEnricherOptions> configure);
    public static IServiceCollection AddProcessLogEnricher(this IServiceCollection services, IConfigurationSection section);
}
