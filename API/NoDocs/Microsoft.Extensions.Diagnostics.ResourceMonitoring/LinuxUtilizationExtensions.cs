// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

public static class LinuxUtilizationExtensions
{
    public static IResourceMonitorBuilder AddLinuxProvider(this IResourceMonitorBuilder builder);
    public static IResourceMonitorBuilder AddLinuxProvider(this IResourceMonitorBuilder builder, IConfigurationSection section);
    public static IResourceMonitorBuilder AddLinuxProvider(this IResourceMonitorBuilder builder, Action<LinuxResourceUtilizationProviderOptions> configure);
}
