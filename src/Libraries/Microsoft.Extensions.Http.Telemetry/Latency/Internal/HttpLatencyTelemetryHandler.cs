// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Telemetry.Latency.Internal;

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

    public HttpLatencyTelemetryHandler(HttpRequestLatencyListener latencyListener, ILatencyContextTokenIssuer tokenIssuer, ILatencyContextProvider latencyContextProvider,
        IOptions<HttpClientLatencyTelemetryOptions> options, IOptions<ApplicationMetadata> appMetdata)
    {
        var appMetadata = Throw.IfMemberNull(appMetdata, appMetdata.Value);
        var telemetryOptions = Throw.IfMemberNull(options, options.Value);

        _latencyListener = latencyListener;
        _latencyContextProvider = latencyContextProvider;
        _handlerStart = tokenIssuer.GetCheckpointToken(HttpCheckpoints.HandlerRequestStart);
        _applicationName = appMetdata.Value.ApplicationName;

        if (telemetryOptions.EnableDetailedLatencyBreakdown)
        {
            _latencyListener.Enable();
        }
    }

    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var context = _latencyContextProvider.CreateContext();
        context.AddCheckpoint(_handlerStart);
        _latencyListener.LatencyContext.Set(context);

        request.Headers.Add(TelemetryConstants.ClientApplicationNameHeader, _applicationName);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        _latencyListener.LatencyContext.Unset();

        return response;
    }
}
