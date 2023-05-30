// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Extensions.Telemetry.Metering;

namespace Microsoft.Extensions.Http.Telemetry.Metering;

/// <summary>
/// Handler that logs outgoing request duration.
/// </summary>
/// <seealso cref="T:System.Net.Http.DelegatingHandler" />
public class HttpMeteringHandler : DelegatingHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Http.Telemetry.Metering.HttpMeteringHandler" /> class.
    /// </summary>
    /// <param name="meter">The meter.</param>
    /// <param name="enrichers">Enumerable of outgoing request metric enrichers.</param>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public HttpMeteringHandler(Meter<HttpMeteringHandler> meter, IEnumerable<IOutgoingRequestMetricEnricher> enrichers);

    /// <summary>
    /// Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.
    /// </summary>
    /// <param name="request">The HTTP request message to send to the server.</param>
    /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
    /// <returns>
    /// The task object representing the asynchronous operation.
    /// </returns>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}
