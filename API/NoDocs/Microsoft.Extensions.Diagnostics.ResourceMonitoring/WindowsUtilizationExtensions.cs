// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

public static class WindowsUtilizationExtensions
{
    public static IResourceMonitorBuilder AddWindowsProvider(this IResourceMonitorBuilder builder);
    public static IResourceMonitorBuilder AddWindowsPerfCounterPublisher(this IResourceMonitorBuilder builder);
    [Experimental("EXTEXP0008", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IResourceMonitorBuilder AddWindowsCounters(this IResourceMonitorBuilder builder);
    [Experimental("EXTEXP0008", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IResourceMonitorBuilder AddWindowsCounters(this IResourceMonitorBuilder builder, IConfigurationSection section);
    [Experimental("EXTEXP0008", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IResourceMonitorBuilder AddWindowsCounters(this IResourceMonitorBuilder builder, Action<WindowsCountersOptions> configure);
}
