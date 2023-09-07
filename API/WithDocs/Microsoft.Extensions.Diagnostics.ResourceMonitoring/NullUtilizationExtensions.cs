// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Extensions for adding the <see langword="null" /> resource utilization provider.
/// </summary>
public static class NullUtilizationExtensions
{
    /// <summary>
    /// Adds a platform independent and non-operational provider to the service collection.
    /// </summary>
    /// <param name="builder">The builder instance used to configure the tracker.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <remarks>This extension method will add a non-operational provider that generates fixed CPU and Memory information. Don't use this in
    /// production, but you can use it in development environment when you're uncertain about the underlying platform and don't need real data
    /// to be generated.</remarks>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IResourceMonitorBuilder AddNullProvider(this IResourceMonitorBuilder builder);
}
