// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.DependencyInjection.Internal;

/// <summary>
/// An implementation of <see cref="BackgroundService"/> which operates on the provided <see cref="MessageConsumer"/>.
/// </summary>
internal sealed class ConsumerBackgroundService : BackgroundService
{
    private readonly MessageConsumer _consumer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerBackgroundService"/> class.
    /// </summary>
    /// <param name="consumer"><see cref="MessageConsumer"/>.</param>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public ConsumerBackgroundService(MessageConsumer consumer)
    {
        _consumer = Throw.IfNull(consumer);
    }

    /// <summary>
    /// Executes the underlying <see cref="MessageConsumer.ExecuteAsync(CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="MessageConsumer.ExecuteAsync(CancellationToken)"/> can have a synchronous implementation.
    /// BackgroundService assumes that its derived classes will have an asynchronous ExecuteAsync.
    /// Refer <see href="https://github.com/dotnet/runtime/blob/e3ffd343ad5bd3a999cb9515f59e6e7a777b2c34/src/libraries/Microsoft.Extensions.Hosting.Abstractions/src/BackgroundService.cs#L37"/>.
    /// Hence, using <see cref="Task.Yield"/> to not block and yield back to the current context.
    /// </remarks>
    /// <param name="stoppingToken">The cancellation token to stop the <see cref="ConsumerBackgroundService"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        await _consumer.ExecuteAsync(stoppingToken).ConfigureAwait(false);
    }
}
