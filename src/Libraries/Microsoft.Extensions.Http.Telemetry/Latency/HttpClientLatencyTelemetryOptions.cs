// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.Extensions.Http.Telemetry.Latency;

/// <summary>
/// Options to configure the http client latency telemetry.
/// </summary>
public class HttpClientLatencyTelemetryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to collect detailed latency breakdown of <see cref="HttpClient"/> call.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    /// <remarks>
    /// Detailed breakdowns add checkpoints for HTTP operations, such as connection open and request headers sent.
    /// </remarks>
    public bool EnableDetailedLatencyBreakdown { get; set; } = true;
}
