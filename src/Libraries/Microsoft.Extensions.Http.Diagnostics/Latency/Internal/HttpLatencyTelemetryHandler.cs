// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Diagnostics.Latency;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Latency.Internal;

/// <summary>
/// This delegating handler creates a <see cref="ILatencyContext"/> for the request if it has not been created for the request.
/// It also adds client name to outgoing request header which helps with correlating client and server telemetry.
/// </summary>
internal sealed class HttpLatencyTelemetryHandler : DelegatingHandler
{
    private readonly HttpRequestLatencyListener _latencyListener;
    private readonly ILatencyContextProvider _latencyContextProvider;
    private readonly CheckpointToken _handlerStart;
    private readonly string _applicationName;
    private readonly HttpLatencyMediator _latencyMediator;

    public HttpLatencyTelemetryHandler(HttpRequestLatencyListener latencyListener, ILatencyContextTokenIssuer tokenIssuer, ILatencyContextProvider latencyContextProvider,
        IOptions<HttpClientLatencyTelemetryOptions> options, IOptions<ApplicationMetadata> appMetadata, HttpLatencyMediator latencyTelemetryMediator)
    {
        _latencyListener = latencyListener;
        _latencyContextProvider = latencyContextProvider;
        _handlerStart = tokenIssuer.GetCheckpointToken(HttpCheckpoints.HandlerRequestStart);
        _applicationName = appMetadata.Value.ApplicationName;
        _latencyMediator = latencyTelemetryMediator;

        if (options.Value.EnableDetailedLatencyBreakdown)
        {
            _latencyListener.Enable();
        }
    }

    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var context = _latencyContextProvider.CreateContext();
        context.AddCheckpoint(_handlerStart);
        _latencyListener.LatencyContext.Set(context);

        _latencyMediator.RecordStart(context, request);

        _ = request.Headers.TryAddWithoutValidation(TelemetryConstants.ClientApplicationNameHeader, _applicationName);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
