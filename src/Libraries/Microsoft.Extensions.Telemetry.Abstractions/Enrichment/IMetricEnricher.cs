// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// A component that augments metric state with additional properties.
/// </summary>
public interface IMetricEnricher
{
    /// <summary>
    /// Called to collect tags for metrics.
    /// </summary>
    /// <param name="collector">Where the enricher puts the tags it is producing.</param>
    void Enrich(IEnrichmentTagCollector collector);
}
