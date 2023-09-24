// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Lets you configure and register resource monitoring components.
/// </summary>
public static class ResourceMonitoringServiceCollectionExtensions
{
    /// <summary>
    /// Configures and adds an <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.IResourceMonitor" /> implementation to a service collection.
    /// </summary>
    /// <param name="services">The dependency injection container to add the monitor to.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddResourceMonitoring(this IServiceCollection services);

    /// <summary>
    /// Configures and adds an <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.IResourceMonitor" /> implementation to a service collection.
    /// </summary>
    /// <param name="services">The dependency injection container to add the monitor to.</param>
    /// <param name="configure">Delegate to configure <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.IResourceMonitorBuilder" />.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">Either <paramref name="services" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddResourceMonitoring(this IServiceCollection services, Action<IResourceMonitorBuilder> configure);
}
