// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// Augments metric state with additional properties.
/// </summary>
public interface IMetricEnricher
{
    /// <summary>
    /// Collects tags for metrics.
    /// </summary>
    /// <param name="collector">Where the enricher puts the tags it produces.</param>
    void Enrich(IEnrichmentTagCollector collector);
}
