// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Extensions for adding the Linux resource utilization provider.
/// </summary>
public static class LinuxUtilizationExtensions
{
    /// <summary>
    /// An extension method to configure and add the Linux utilization provider to services collection.
    /// </summary>
    /// <param name="builder">The tracker builder instance used to add the provider.</param>
    /// <returns>Returns the input tracker builder for call chaining.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IResourceMonitorBuilder AddLinuxProvider(this IResourceMonitorBuilder builder);

    /// <summary>
    /// An extension method to configure and add the Linux utilization provider to services collection.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="section">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> to use for configuring of <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.LinuxResourceUtilizationProviderOptions" />.</param>
    /// <returns>Returns the builder.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    /// <seealso cref="T:System.Diagnostics.Metrics.Instrument" />
    public static IResourceMonitorBuilder AddLinuxProvider(this IResourceMonitorBuilder builder, IConfigurationSection section);

    /// <summary>
    /// An extension method to configure and add the Linux utilization provider to services collection.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configure">The delegate for configuring of <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.LinuxResourceUtilizationProviderOptions" />.</param>
    /// <returns>Returns the builder.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
    public static IResourceMonitorBuilder AddLinuxProvider(this IResourceMonitorBuilder builder, Action<LinuxResourceUtilizationProviderOptions> configure);
}
