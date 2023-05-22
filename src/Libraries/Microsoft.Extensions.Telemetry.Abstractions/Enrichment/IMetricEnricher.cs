// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// A component that augments metric state with additional properties.
/// </summary>
public interface IMetricEnricher
{
    /// <summary>
    /// Called to generate properties for metrics.
    /// </summary>
    /// <param name="bag">Where the enricher puts the properties it is producing.</param>
    void Enrich(IEnrichmentPropertyBag bag);
}
