// Assembly 'Microsoft.Extensions.Telemetry'

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Telemetry;

namespace Microsoft.Extensions.Telemetry;

/// <summary>
/// Extensions for telemetry utilities.
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Sets metadata for outgoing requests to be used for telemetry purposes.
    /// </summary>
    /// <param name="request"><see cref="T:System.Net.HttpWebRequest" /> object.</param>
    /// <param name="metadata">Metadata for the request.</param>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static void SetRequestMetadata(this HttpWebRequest request, RequestMetadata metadata);

    /// <summary>
    /// Sets metadata for outgoing requests to be used for telemetry purposes.
    /// </summary>
    /// <param name="request"><see cref="T:System.Net.Http.HttpRequestMessage" /> object.</param>
    /// <param name="metadata">Metadata for the request.</param>
    public static void SetRequestMetadata(this HttpRequestMessage request, RequestMetadata metadata);

    /// <summary>
    /// Gets metadata for outgoing requests to be used for telemetry purposes.
    /// </summary>
    /// <param name="request"><see cref="T:System.Net.HttpWebRequest" /> object.</param>
    /// <returns>Request metadata.</returns>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static RequestMetadata? GetRequestMetadata(this HttpWebRequest request);

    /// <summary>
    /// Gets metadata for outgoing requests to be used for telemetry purposes.
    /// </summary>
    /// <param name="request"><see cref="T:System.Net.Http.HttpRequestMessage" /> object.</param>
    /// <returns>Request metadata or <see langword="null" />.</returns>
    public static RequestMetadata? GetRequestMetadata(this HttpRequestMessage request);

    /// <summary>
    /// Adds dependency metadata.
    /// </summary>
    /// <param name="services"><see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> object instance.</param>
    /// <param name="downstreamDependencyMetadata">DownstreamDependencyMetadata object to add.</param>
    /// <returns><see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> object for chaining.</returns>
    public static IServiceCollection AddDownstreamDependencyMetadata(this IServiceCollection services, IDownstreamDependencyMetadata downstreamDependencyMetadata);

    /// <summary>
    /// Adds dependency metadata.
    /// </summary>
    /// <typeparam name="T"><see cref="T:Microsoft.Extensions.Http.Telemetry.IDownstreamDependencyMetadata" /> instance to be registered.</typeparam>
    /// <param name="services"><see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> object instance.</param>
    /// <returns><see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> object for chaining.</returns>
    public static IServiceCollection AddDownstreamDependencyMetadata<T>(this IServiceCollection services) where T : class, IDownstreamDependencyMetadata;
}
