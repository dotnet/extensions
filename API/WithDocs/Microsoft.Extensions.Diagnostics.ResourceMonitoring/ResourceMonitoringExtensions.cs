// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Lets you configure and register resource monitoring components.
/// </summary>
public static class ResourceMonitoringExtensions
{
    /// <summary>
    /// Configures and adds an <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.IResourceMonitor" /> implementation to a service collection.
    /// </summary>
    /// <param name="services">The dependency injection container to add the monitor to.</param>
    /// <param name="configure">Delegate to configure <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.IResourceMonitorBuilder" />.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">Either <paramref name="services" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddResourceMonitoring(this IServiceCollection services, Action<IResourceMonitorBuilder> configure);

    /// <summary>
    /// Configures and adds an <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.IResourceMonitor" /> implementation to a host.
    /// </summary>
    /// <param name="builder">The host builder to bind to.</param>
    /// <param name="configure">Delegate to configure <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.IResourceMonitorBuilder" />.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">Either <paramref name="builder" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
    public static IHostBuilder ConfigureResourceMonitoring(this IHostBuilder builder, Action<IResourceMonitorBuilder> configure);

    /// <summary>
    /// Configures the resource utilization tracker.
    /// </summary>
    /// <param name="builder">The builder instance used to configure the tracker.</param>
    /// <param name="configure">Delegate to configure <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.ResourceMonitoringOptions" />.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">Either <paramref name="builder" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
    public static IResourceMonitorBuilder ConfigureMonitor(this IResourceMonitorBuilder builder, Action<ResourceMonitoringOptions> configure);

    /// <summary>
    /// Configures the resource utilization tracker.
    /// </summary>
    /// <param name="builder">The builder instance used to configure the tracker.</param>
    /// <param name="section">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> to use for configuring <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.ResourceMonitoringOptions" />.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">Either <paramref name="builder" /> or <paramref name="section" /> is <see langword="null" />.</exception>
    public static IResourceMonitorBuilder ConfigureMonitor(this IResourceMonitorBuilder builder, IConfigurationSection section);
}
