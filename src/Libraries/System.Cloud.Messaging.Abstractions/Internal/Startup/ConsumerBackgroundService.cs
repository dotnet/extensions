// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.Internal;

/// <summary>
/// An implementation of <see cref="BackgroundService"/> which operates on the provided <see cref="IMessageConsumer"/>.
/// </summary>
internal sealed class ConsumerBackgroundService : BackgroundService
{
    private readonly IMessageConsumer _consumer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerBackgroundService"/> class.
    /// </summary>
    /// <param name="consumer"><see cref="IMessageConsumer"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public ConsumerBackgroundService(IMessageConsumer consumer)
    {
        _consumer = Throw.IfNull(consumer);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        await _consumer.ExecuteAsync(stoppingToken).ConfigureAwait(false);
    }
}
