// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

/// <summary>
/// Interface for implementing log enrichers for incoming HTTP requests.
/// </summary>
public interface IHttpLogEnricher
{
    /// <summary>
    /// Enrich logs.
    /// </summary>
    /// <param name="collector">Tag collector to add tags to.</param>
    /// <param name="request"><see cref="HttpResponse"/> object associated with the incoming HTTP request.</param>
    /// <param name="response"><see cref="HttpResponse"/> object associated with the response to an incoming HTTP request.</param>
    void Enrich(IEnrichmentTagCollector collector, HttpRequest request, HttpResponse response);
}
