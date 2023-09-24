// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;

namespace Microsoft.Extensions.DependencyInjection;

public static class ResourceMonitoringServiceCollectionExtensions
{
    public static IServiceCollection AddResourceMonitoring(this IServiceCollection services);
    public static IServiceCollection AddResourceMonitoring(this IServiceCollection services, Action<IResourceMonitorBuilder> configure);
}
