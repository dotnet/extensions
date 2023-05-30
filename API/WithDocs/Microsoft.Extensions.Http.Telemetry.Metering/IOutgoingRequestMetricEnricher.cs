// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Http.Telemetry.Metering;

/// <summary>
/// Interface for implementing enrichers for outgoing request metrics.
/// </summary>
public interface IOutgoingRequestMetricEnricher : IMetricEnricher
{
    /// <summary>
    /// Gets a list of dimension names to enrich outgoing request metrics.
    /// </summary>
    IReadOnlyList<string> DimensionNames { get; }
}
