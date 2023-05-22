// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Connections;

internal sealed class ConnectionTimeoutDelegate
{
    internal TimeProvider TimeProvider = TimeProvider.System;

    private readonly ConnectionDelegate _next;
    private readonly TimeSpan _timeout;

    public ConnectionTimeoutDelegate(ConnectionDelegate next, IOptions<ConnectionTimeoutOptions> options)
    {
        _next = next;
        _timeout = options.Value.Timeout;
    }

    public async Task OnConnectionAsync(ConnectionContext context)
    {
        var connectionLifetimeNotification = context.Features.Get<IConnectionLifetimeNotificationFeature>();
        if (connectionLifetimeNotification == null)
        {
            Throw.InvalidOperationException("IConnectionLifetimeNotificationFeature hasn't been registered.");
        }

        var delayTask = TimeProvider.Delay(_timeout, CancellationToken.None);
        var next = _next(context);

        var completedTask = await Task.WhenAny(next, delayTask).ConfigureAwait(false);

        if (completedTask == delayTask)
        {
            connectionLifetimeNotification.RequestClose();
        }
    }
}
