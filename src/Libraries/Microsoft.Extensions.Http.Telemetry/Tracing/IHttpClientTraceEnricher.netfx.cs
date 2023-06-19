// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETCOREAPP3_1_OR_GREATER

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Microsoft.Extensions.Http.Telemetry.Tracing;

/// <summary>
/// Interface for implementing enricher for enriching only traces for outgoing HTTP requests.
/// </summary>
public interface IHttpClientTraceEnricher
{
    /// <summary>
    /// Enrich trace with desired tags.
    /// </summary>
    /// <param name="activity"><see cref="Activity"/> object to be used to add the required tags to enrich the traces.</param>
    /// <param name="webRequest"><see cref="HttpWebRequest"/> object associated with the outgoing request for the trace.</param>
    /// <param name="webResponse"><see cref="HttpWebResponse"/> object associated with the outgoing request for the trace.</param>
    /// <remarks>
    /// If your enricher fetches some information from <see cref="HttpWebRequest"/> or <see cref="HttpWebResponse"/> to enrich HTTP traces, then make sure to check it for <see langword="null"/>.
    /// </remarks>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    void Enrich(Activity activity, HttpWebRequest? webRequest, HttpWebResponse? webResponse);
}

#endif
