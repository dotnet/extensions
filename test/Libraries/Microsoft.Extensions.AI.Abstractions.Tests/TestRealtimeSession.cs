// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

/// <summary>A test <see cref="IRealtimeSession"/> implementation that uses callbacks for verification.</summary>
public sealed class TestRealtimeSession : IRealtimeSession
{
    /// <summary>Gets or sets the callback to invoke when <see cref="UpdateAsync"/> is called.</summary>
    public Func<RealtimeSessionOptions, CancellationToken, Task>? UpdateAsyncCallback { get; set; }

    /// <summary>Gets or sets the callback to invoke when <see cref="InjectClientMessageAsync"/> is called.</summary>
    public Func<RealtimeClientMessage, CancellationToken, Task>? InjectClientMessageAsyncCallback { get; set; }

    /// <summary>Gets or sets the callback to invoke when <see cref="GetStreamingResponseAsync"/> is called.</summary>
    public Func<IAsyncEnumerable<RealtimeClientMessage>, CancellationToken, IAsyncEnumerable<RealtimeServerMessage>>? GetStreamingResponseAsyncCallback { get; set; }

    /// <summary>Gets or sets the callback to invoke when <see cref="GetService"/> is called.</summary>
    public Func<Type, object?, object?>? GetServiceCallback { get; set; }

    /// <inheritdoc/>
    public RealtimeSessionOptions? Options { get; set; }

    /// <inheritdoc/>
    public Task UpdateAsync(RealtimeSessionOptions options, CancellationToken cancellationToken = default)
    {
        return UpdateAsyncCallback?.Invoke(options, cancellationToken) ?? Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task InjectClientMessageAsync(RealtimeClientMessage message, CancellationToken cancellationToken = default)
    {
        return InjectClientMessageAsyncCallback?.Invoke(message, cancellationToken) ?? Task.CompletedTask;
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<RealtimeServerMessage> GetStreamingResponseAsync(
        IAsyncEnumerable<RealtimeClientMessage> updates, CancellationToken cancellationToken = default)
    {
        return GetStreamingResponseAsyncCallback?.Invoke(updates, cancellationToken) ?? EmptyAsyncEnumerable();
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (GetServiceCallback is { } callback)
        {
            return callback(serviceType, serviceKey);
        }

        return serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // No-op for test implementation
    }

    private static async IAsyncEnumerable<RealtimeServerMessage> EmptyAsyncEnumerable()
    {
        await Task.CompletedTask.ConfigureAwait(false);
        yield break;
    }
}
