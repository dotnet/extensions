// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.Middlewares.Internal;

/// <summary>
/// An <see cref="IMessageMiddleware"/> implementation to register <see cref="ILatencyContext"/> to record latency for <see cref="MessageDelegate"/>.
/// </summary>
internal sealed class LatencyContextMiddleware : IMessageMiddleware
{
    private readonly ILatencyContext _latencyContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="LatencyContextMiddleware"/> class.
    /// </summary>
    /// <param name="latencyContext"><see cref="ILatencyContext"/>.</param>
    public LatencyContextMiddleware(ILatencyContext latencyContext)
    {
        _latencyContext = Throw.IfNull(latencyContext);
    }

    /// <inheritdoc/>
    public async ValueTask InvokeAsync(MessageContext context, MessageDelegate nextHandler)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(nextHandler);

        context.SetLatencyContext(_latencyContext);
        await nextHandler.Invoke(context).ConfigureAwait(false);
    }
}
