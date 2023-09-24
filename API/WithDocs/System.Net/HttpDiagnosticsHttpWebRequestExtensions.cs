// Assembly 'Microsoft.Extensions.Http.Diagnostics'

using Microsoft.Extensions.Http.Diagnostics;

namespace System.Net;

/// <summary>
/// Extensions for telemetry utilities.
/// </summary>
public static class HttpDiagnosticsHttpWebRequestExtensions
{
    /// <summary>
    /// Sets metadata for outgoing requests to be used for telemetry purposes.
    /// </summary>
    /// <param name="request"><see cref="T:System.Net.HttpWebRequest" /> object.</param>
    /// <param name="metadata">Metadata for the request.</param>
    public static void SetRequestMetadata(this HttpWebRequest request, RequestMetadata metadata);

    /// <summary>
    /// Gets metadata for outgoing requests to be used for telemetry purposes.
    /// </summary>
    /// <param name="request"><see cref="T:System.Net.HttpWebRequest" /> object.</param>
    /// <returns>Request metadata.</returns>
    public static RequestMetadata? GetRequestMetadata(this HttpWebRequest request);
}
