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
    /// <remarks>
    /// Detailed breakdowns add checkpoints for HTTP operations such as connection open, request headers sent etc.
    /// Defaults to <see langword="true"/>.
    /// </remarks>
    public bool EnableDetailedLatencyBreadkdown { get; set; } = true;
}
