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

public class HttpMeteringHandler : DelegatingHandler
{
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public HttpMeteringHandler(Meter<HttpMeteringHandler> meter, IEnumerable<IOutgoingRequestMetricEnricher> enrichers);
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}
