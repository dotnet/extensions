// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// Represents a component that augments log records with additional properties that are unchanging over the life of the object.
/// </summary>
[Experimental(diagnosticId: Experiments.Telemetry, UrlFormat = Experiments.UrlFormat)]
public interface IStaticLogEnricher
{
    /// <summary>
    /// Collects tags for a log record.
    /// </summary>
    /// <param name="collector">Where the enricher puts the tags it produces.</param>
    void Enrich(IEnrichmentTagCollector collector);
}
