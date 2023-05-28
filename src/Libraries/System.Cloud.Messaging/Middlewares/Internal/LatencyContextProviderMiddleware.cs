// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace System.Cloud.Messaging.Middlewares.Internal;

/// <summary>
/// An <see cref="IMessageMiddleware"/> implementation to register <see cref="ILatencyContextProvider"/> to record latency for <see cref="MessageDelegate"/>.
/// </summary>
internal sealed class LatencyContextProviderMiddleware : IMessageMiddleware
{
    private static readonly ObjectPool<List<Task>> _exporterTaskPool = PoolFactory.CreateListPool<Task>();

    private readonly ILatencyContextProvider _latencyContextProvider;
    private readonly ILatencyDataExporter[] _latencyDataExporters;

    /// <summary>
    /// Initializes a new instance of the <see cref="LatencyContextProviderMiddleware"/> class.
    /// </summary>
    /// <param name="latencyContextProvider"><see cref="ILatencyContextProvider"/>.</param>
    /// <param name="latencyDataExporters">The list of exporters for latency data.</param>
    public LatencyContextProviderMiddleware(ILatencyContextProvider latencyContextProvider,
                                            IEnumerable<ILatencyDataExporter> latencyDataExporters)
    {
        _latencyContextProvider = Throw.IfNull(latencyContextProvider);
        _latencyDataExporters = Throw.IfNullOrEmpty(latencyDataExporters).ToArray();
    }

    /// <inheritdoc/>
    public async ValueTask InvokeAsync(MessageContext context, MessageDelegate nextHandler)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(nextHandler);

        ILatencyContext latencyContext = _latencyContextProvider.CreateContext();
        context.SetLatencyContext(latencyContext);

        await nextHandler.Invoke(context).ConfigureAwait(false);

        latencyContext.Freeze();
        await ExportAsync(latencyContext.LatencyData, context.MessageCancelledToken).ConfigureAwait(false);
    }

    private async Task ExportAsync(LatencyData latencyData, CancellationToken cancellationToken)
    {
        List<Task> exports = _exporterTaskPool.Get();
        foreach (ILatencyDataExporter latencyDataExporter in _latencyDataExporters)
        {
            exports.Add(latencyDataExporter.ExportAsync(latencyData, cancellationToken));
        }

        await Task.WhenAll(exports).ConfigureAwait(false);

        _exporterTaskPool.Return(exports);
    }
}
