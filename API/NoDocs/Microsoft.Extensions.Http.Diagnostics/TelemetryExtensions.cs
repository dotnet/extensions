// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Diagnostics;

public static class TelemetryExtensions
{
    public static void SetRequestMetadata(this HttpWebRequest request, RequestMetadata metadata);
    public static void SetRequestMetadata(this HttpRequestMessage request, RequestMetadata metadata);
    public static RequestMetadata? GetRequestMetadata(this HttpWebRequest request);
    public static RequestMetadata? GetRequestMetadata(this HttpRequestMessage request);
    public static IServiceCollection AddDownstreamDependencyMetadata(this IServiceCollection services, IDownstreamDependencyMetadata downstreamDependencyMetadata);
    public static IServiceCollection AddDownstreamDependencyMetadata<T>(this IServiceCollection services) where T : class, IDownstreamDependencyMetadata;
}
