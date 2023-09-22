// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Latency;

public static class LatencyConsoleExtensions
{
    public static IServiceCollection AddConsoleLatencyDataExporter(this IServiceCollection services);
    public static IServiceCollection AddConsoleLatencyDataExporter(this IServiceCollection services, Action<LatencyConsoleOptions> configure);
    public static IServiceCollection AddConsoleLatencyDataExporter(this IServiceCollection services, IConfigurationSection section);
}
