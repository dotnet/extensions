// Assembly 'Microsoft.Extensions.Http.Diagnostics'

using Microsoft.Extensions.Http.Diagnostics;

namespace System.Net.Http;

/// <summary>
/// Extensions for telemetry utilities.
/// </summary>
public static class HttpDiagnosticsHttpRequestMessageExtensions
{
    /// <summary>
    /// Sets metadata for outgoing requests to be used for telemetry purposes.
    /// </summary>
    /// <param name="request"><see cref="T:System.Net.Http.HttpRequestMessage" /> object.</param>
    /// <param name="metadata">Metadata for the request.</param>
    public static void SetRequestMetadata(this HttpRequestMessage request, RequestMetadata metadata);

    /// <summary>
    /// Gets metadata for outgoing requests to be used for telemetry purposes.
    /// </summary>
    /// <param name="request"><see cref="T:System.Net.Http.HttpRequestMessage" /> object.</param>
    /// <returns>Request metadata or <see langword="null" />.</returns>
    public static RequestMetadata? GetRequestMetadata(this HttpRequestMessage request);
}
