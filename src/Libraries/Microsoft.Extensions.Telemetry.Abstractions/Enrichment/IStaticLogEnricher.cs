﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Enrichment;

/// <summary>
/// Augments log records with additional properties that are unchanging over the life of the object.
/// </summary>
public interface IStaticLogEnricher
{
    /// <summary>
    /// Collects tags for a log record.
    /// </summary>
    /// <param name="collector">The collector where the enricher puts the tags it produces.</param>
    void Enrich(IEnrichmentTagCollector collector);
}
