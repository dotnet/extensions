// Assembly 'Microsoft.Extensions.Http.Diagnostics'

using Microsoft.Extensions.Http.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for telemetry utilities.
/// </summary>
public static class HttpDiagnosticsServiceCollectionExtensions
{
    /// <summary>
    /// Adds dependency metadata.
    /// </summary>
    /// <param name="services"><see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> object instance.</param>
    /// <param name="downstreamDependencyMetadata">DownstreamDependencyMetadata object to add.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddDownstreamDependencyMetadata(this IServiceCollection services, IDownstreamDependencyMetadata downstreamDependencyMetadata);

    /// <summary>
    /// Adds dependency metadata.
    /// </summary>
    /// <typeparam name="T"><see cref="T:Microsoft.Extensions.Http.Diagnostics.IDownstreamDependencyMetadata" /> instance to be registered.</typeparam>
    /// <param name="services"><see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> object instance.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddDownstreamDependencyMetadata<T>(this IServiceCollection services) where T : class, IDownstreamDependencyMetadata;
}
