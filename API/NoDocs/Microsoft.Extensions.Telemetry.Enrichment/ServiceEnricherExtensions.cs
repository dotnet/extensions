// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Telemetry.Enrichment;

public static class ServiceEnricherExtensions
{
    public static IServiceCollection AddServiceLogEnricher(this IServiceCollection services);
    public static IServiceCollection AddServiceLogEnricher(this IServiceCollection services, Action<ServiceLogEnricherOptions> configure);
    public static IServiceCollection AddServiceLogEnricher(this IServiceCollection services, IConfigurationSection section);
    public static IServiceCollection AddServiceMetricEnricher(this IServiceCollection services);
    public static IServiceCollection AddServiceMetricEnricher(this IServiceCollection services, Action<ServiceMetricEnricherOptions> configure);
    public static IServiceCollection AddServiceMetricEnricher(this IServiceCollection services, IConfigurationSection section);
    public static TracerProviderBuilder AddServiceTraceEnricher(this TracerProviderBuilder builder);
    public static TracerProviderBuilder AddServiceTraceEnricher(this TracerProviderBuilder builder, Action<ServiceTraceEnricherOptions> configure);
    public static TracerProviderBuilder AddServiceTraceEnricher(this TracerProviderBuilder builder, IConfigurationSection section);
}
