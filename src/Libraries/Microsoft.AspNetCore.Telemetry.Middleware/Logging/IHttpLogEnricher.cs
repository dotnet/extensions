// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Interface for implementing log enrichers for incoming HTTP requests.
/// </summary>
[Experimental(diagnosticId: Experiments.HttpLogging, UrlFormat = Experiments.UrlFormat)]
public interface IHttpLogEnricher
{
    /// <summary>
    /// Enrich logs.
    /// </summary>
    /// <param name="collector">Tag collector to add tags to.</param>
    /// <param name="httpContext"><see cref="HttpContext"/> object associated with the incoming HTTP request.</param>
    void Enrich(IEnrichmentTagCollector collector, HttpContext httpContext);
}
#endif
