// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating realtime session that wraps an inner session with implementations provided by delegates.</summary>
[Experimental("MEAI001")]
internal sealed class AnonymousDelegatingRealtimeSession : DelegatingRealtimeSession
{
    /// <summary>The delegate to use as the implementation of <see cref="GetStreamingResponseAsync"/>.</summary>
    private readonly Func<IAsyncEnumerable<RealtimeClientMessage>, IRealtimeSession, CancellationToken, IAsyncEnumerable<RealtimeServerMessage>> _getStreamingResponseFunc;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymousDelegatingRealtimeSession"/> class.
    /// </summary>
    /// <param name="innerSession">The inner session.</param>
    /// <param name="getStreamingResponseFunc">
    /// A delegate that provides the implementation for <see cref="GetStreamingResponseAsync"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="innerSession"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="getStreamingResponseFunc"/> is <see langword="null"/>.</exception>
    public AnonymousDelegatingRealtimeSession(
        IRealtimeSession innerSession,
        Func<IAsyncEnumerable<RealtimeClientMessage>, IRealtimeSession, CancellationToken, IAsyncEnumerable<RealtimeServerMessage>> getStreamingResponseFunc)
        : base(innerSession)
    {
        _getStreamingResponseFunc = Throw.IfNull(getStreamingResponseFunc);
    }

    /// <inheritdoc/>
    public override IAsyncEnumerable<RealtimeServerMessage> GetStreamingResponseAsync(
        IAsyncEnumerable<RealtimeClientMessage> updates, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(updates);

        return _getStreamingResponseFunc(updates, InnerSession, cancellationToken);
    }
}
