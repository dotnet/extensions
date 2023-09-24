// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

public static class ResourceMonitoringBuilderExtensions
{
    public static IResourceMonitorBuilder ConfigureMonitor(this IResourceMonitorBuilder builder, Action<ResourceMonitoringOptions> configure);
    public static IResourceMonitorBuilder ConfigureMonitor(this IResourceMonitorBuilder builder, IConfigurationSection section);
}
