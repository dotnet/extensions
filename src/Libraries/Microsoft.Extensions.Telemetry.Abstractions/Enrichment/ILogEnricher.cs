// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Enrichment;

/// <summary>
/// Represents a component that augments log records with additional properties.
/// </summary>
public interface ILogEnricher
{
    /// <summary>
    /// Collects tags for a log record.
    /// </summary>
    /// <param name="collector">Where the enricher puts the tags it produces.</param>
    void Enrich(IEnrichmentTagCollector collector);
}
