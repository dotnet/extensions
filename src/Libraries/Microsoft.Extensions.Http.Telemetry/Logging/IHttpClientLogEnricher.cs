// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Http.Telemetry.Logging;

/// <summary>
/// Interface for implementing log enrichers for HTTP client requests.
/// </summary>
public interface IHttpClientLogEnricher
{
    /// <summary>
    /// Enrich HTTP client request logs.
    /// </summary>
    /// <param name="collector">Tag collector to add tags to.</param>
    /// <param name="request"><see cref="HttpRequestMessage"/> object associated with the outgoing HTTP request.</param>
    /// <param name="response"><see cref="HttpResponseMessage"/> object associated with the outgoing HTTP request.</param>
    void Enrich(IEnrichmentTagCollector collector, HttpRequestMessage? request = null, HttpResponseMessage? response = null);
}
