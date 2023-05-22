// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Shared.Pools;

namespace Microsoft.AspNetCore.Telemetry.Internal;

/// <summary>
/// Middleware that manages latency context for requests.
/// </summary>
internal sealed class RequestLatencyTelemetryMiddleware : IMiddleware
{
    private static readonly ObjectPool<List<Task>> _exporterTaskPool = PoolFactory.CreateListPool<Task>();
    private static readonly ObjectPool<CancellationTokenSource> _cancellationTokenSourcePool = PoolFactory.CreateCancellationTokenSourcePool();

    private readonly TimeSpan _exportTimeout;

    private readonly ILatencyDataExporter[] _latencyDataExporters;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestLatencyTelemetryMiddleware"/> class.
    /// </summary>
    /// <param name="options">An instance of <see cref="RequestLatencyTelemetryOptions"/>.</param>
    /// <param name="latencyDataExporters">The list of exporters for latency data.</param>
    public RequestLatencyTelemetryMiddleware(IOptions<RequestLatencyTelemetryOptions> options, IEnumerable<ILatencyDataExporter> latencyDataExporters)
    {
        _exportTimeout = options.Value.LatencyDataExportTimeout;
        _latencyDataExporters = latencyDataExporters.ToArray();
    }

    /// <summary>
    /// Request handling method.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
    /// <returns>A <see cref="Task"/> that represents the execution of this middleware.</returns>
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var latencyContext = context.RequestServices.GetRequiredService<ILatencyContext>();

        context.Response.OnCompleted(async l =>
        {
            var latencyContext = l as ILatencyContext;
            latencyContext!.Freeze();
            await ExportAsync(latencyContext.LatencyData).ConfigureAwait(false);
        }, latencyContext);

        return next.Invoke(context);
    }

    [SuppressMessage("Resilience", "R9A061:The async method doesn't support cancellation", Justification = "The time limit is enforced inside of the method")]
    private async Task ExportAsync(LatencyData latencyData)
    {
        var tokenSource = _cancellationTokenSourcePool.Get();
        tokenSource.CancelAfter(_exportTimeout);

        List<Task> exports = _exporterTaskPool.Get();
        foreach (ILatencyDataExporter latencyDataExporter in _latencyDataExporters)
        {
            exports.Add(latencyDataExporter.ExportAsync(latencyData, tokenSource.Token));
        }

        await Task.WhenAll(exports).ConfigureAwait(false);

        _exporterTaskPool.Return(exports);
        _cancellationTokenSourcePool.Return(tokenSource);
    }
}
