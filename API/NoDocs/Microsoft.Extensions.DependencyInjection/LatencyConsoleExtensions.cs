// Assembly 'Microsoft.Extensions.Diagnostics.Extra'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.Latency;

namespace Microsoft.Extensions.DependencyInjection;

public static class LatencyConsoleExtensions
{
    public static IServiceCollection AddConsoleLatencyDataExporter(this IServiceCollection services);
    public static IServiceCollection AddConsoleLatencyDataExporter(this IServiceCollection services, Action<LatencyConsoleOptions> configure);
    public static IServiceCollection AddConsoleLatencyDataExporter(this IServiceCollection services, IConfigurationSection section);
}
