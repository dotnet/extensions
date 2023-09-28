// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.AspNetCore.Diagnostics.Latency;

/// <summary>
/// Options to configure the request latency middleware.
/// </summary>
public class RequestLatencyTelemetryOptions
{
    /// <summary>
    /// Gets or sets the amount of time to wait for export of latency data.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    [TimeSpan(RequestLatencyTelemetryOptionsValidator.MinimumTimeoutInMs)]
    public TimeSpan LatencyDataExportTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
