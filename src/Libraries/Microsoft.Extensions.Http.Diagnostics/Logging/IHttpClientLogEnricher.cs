// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Microsoft.Extensions.Http.Logging;

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
    /// <param name="exception">An optional <see cref="Exception"/> that was thrown within the outgoing HTTP request processing.</param>
    /// <remarks>
    /// Depending on the result of the HTTP request,
    /// the <paramref name="response"/> and <paramref name="exception"/> parameters might be <see langword="null"/>.
    /// </remarks>
    void Enrich(IEnrichmentTagCollector collector, HttpRequestMessage request, HttpResponseMessage? response, Exception? exception);
}
