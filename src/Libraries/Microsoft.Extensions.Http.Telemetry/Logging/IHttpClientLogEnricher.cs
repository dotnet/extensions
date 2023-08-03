﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    /// <param name="enrichmentBag">Property bag to add enriched properties to.</param>
    /// <param name="request"><see cref="HttpRequestMessage"/> object associated with the outgoing HTTP request.</param>
    /// <param name="response"><see cref="HttpResponseMessage"/> object associated with the outgoing HTTP request.</param>
    /// <param name="exception">An optional <see cref="Exception"/> that was thrown within the outgoing HTTP request processing.</param>
    // TODO: adding a new parameter to the interface method is a breaking change. Pay additional attention to this during review.
    // Alternatively, we can decide to add another overload instead - where exception in not nullable.
    void Enrich(IEnrichmentPropertyBag enrichmentBag, HttpRequestMessage request, HttpResponseMessage? response = null, Exception? exception = null);
}
