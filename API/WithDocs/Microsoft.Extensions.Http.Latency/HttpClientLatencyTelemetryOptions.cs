// Assembly 'Microsoft.Extensions.Http.Diagnostics'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.Latency;

/// <summary>
/// Options to configure the http client latency telemetry.
/// </summary>
public class HttpClientLatencyTelemetryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to collect detailed latency breakdown of <see cref="T:System.Net.Http.HttpClient" /> call.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    /// <remarks>
    /// Detailed breakdowns add checkpoints for HTTP operations, such as connection open and request headers sent.
    /// </remarks>
    public bool EnableDetailedLatencyBreakdown { get; set; }

    public HttpClientLatencyTelemetryOptions();
}
