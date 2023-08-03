// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Interface for implementing enrichers for request metrics.
/// </summary>
public interface IIncomingRequestMetricEnricher : IMetricEnricher
{
    /// <summary>
    /// Gets a list of dimension names to enrich incoming request metrics.
    /// </summary>
    IReadOnlyList<string> TagNames { get; }
}
