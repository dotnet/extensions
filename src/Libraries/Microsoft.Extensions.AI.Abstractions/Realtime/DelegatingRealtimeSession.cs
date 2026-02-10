// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides an optional base class for an <see cref="IRealtimeSession"/> that passes through calls to another instance.
/// </summary>
/// <remarks>
/// This is recommended as a base type when building sessions that can be chained around an underlying <see cref="IRealtimeSession"/>.
/// The default implementation simply passes each call to the inner session instance.
/// </remarks>
[Experimental("MEAI001")]
public class DelegatingRealtimeSession : IRealtimeSession
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatingRealtimeSession"/> class.
    /// </summary>
    /// <param name="innerSession">The wrapped session instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerSession"/> is <see langword="null"/>.</exception>
    protected DelegatingRealtimeSession(IRealtimeSession innerSession)
    {
        InnerSession = Throw.IfNull(innerSession);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets the inner <see cref="IRealtimeSession" />.</summary>
    protected IRealtimeSession InnerSession { get; }

    /// <inheritdoc />
    public virtual RealtimeSessionOptions? Options => InnerSession.Options;

    /// <inheritdoc />
    public virtual Task InjectClientMessageAsync(RealtimeClientMessage message, CancellationToken cancellationToken = default) =>
        InnerSession.InjectClientMessageAsync(message, cancellationToken);

    /// <inheritdoc />
    public virtual Task UpdateAsync(RealtimeSessionOptions options, CancellationToken cancellationToken = default) =>
        InnerSession.UpdateAsync(options, cancellationToken);

    /// <inheritdoc />
    public virtual IAsyncEnumerable<RealtimeServerMessage> GetStreamingResponseAsync(
        IAsyncEnumerable<RealtimeClientMessage> updates, CancellationToken cancellationToken = default) =>
        InnerSession.GetStreamingResponseAsync(updates, cancellationToken);

    /// <inheritdoc />
    public virtual object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        // If the key is non-null, we don't know what it means so pass through to the inner service.
        return
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this :
            InnerSession.GetService(serviceType, serviceKey);
    }

    /// <summary>Provides a mechanism for releasing unmanaged resources.</summary>
    /// <param name="disposing"><see langword="true"/> if being called from <see cref="Dispose()"/>; otherwise, <see langword="false"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            InnerSession.Dispose();
        }
    }
}
