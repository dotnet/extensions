// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP3_1_OR_GREATER

using System.Diagnostics;
using System.Net.Http;

namespace Microsoft.Extensions.Http.Telemetry.Tracing;

/// <summary>
/// Interface for implementing an enricher for enriching only traces for outgoing HTTP requests.
/// </summary>
public interface IHttpClientTraceEnricher
{
    /// <summary>
    /// Enriches a trace with desired tags.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> object to be used to add the required tags to enrich the traces.</param>
    /// <param name="request">The HTTP request object associated with the outgoing request for the trace.</param>
    /// <param name="response">The HTTP response object associated with the outgoing request for the trace.</param>
    /// <remarks>
    ///  If your enricher fetches some information from <paramref name="request"/> or <paramref name="response"/> to enrich HTTP traces,
    ///  make sure to check them for <see langword="null"/>.
    /// </remarks>
    void Enrich(Activity activity, HttpRequestMessage? request, HttpResponseMessage? response);
}

#endif
