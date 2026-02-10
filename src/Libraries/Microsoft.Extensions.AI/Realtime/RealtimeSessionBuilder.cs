// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="IRealtimeSession"/>.</summary>
[Experimental("MEAI001")]
public sealed class RealtimeSessionBuilder
{
    private readonly Func<IServiceProvider, IRealtimeSession> _innerSessionFactory;

    /// <summary>The registered session factory instances.</summary>
    private List<Func<IRealtimeSession, IServiceProvider, IRealtimeSession>>? _sessionFactories;

    /// <summary>Initializes a new instance of the <see cref="RealtimeSessionBuilder"/> class.</summary>
    /// <param name="innerSession">The inner <see cref="IRealtimeSession"/> that represents the underlying backend.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerSession"/> is <see langword="null"/>.</exception>
    public RealtimeSessionBuilder(IRealtimeSession innerSession)
    {
        _ = Throw.IfNull(innerSession);
        _innerSessionFactory = _ => innerSession;
    }

    /// <summary>Initializes a new instance of the <see cref="RealtimeSessionBuilder"/> class.</summary>
    /// <param name="innerSessionFactory">A callback that produces the inner <see cref="IRealtimeSession"/> that represents the underlying backend.</param>
    public RealtimeSessionBuilder(Func<IServiceProvider, IRealtimeSession> innerSessionFactory)
    {
        _innerSessionFactory = Throw.IfNull(innerSessionFactory);
    }

    /// <summary>Builds an <see cref="IRealtimeSession"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.</summary>
    /// <param name="services">
    /// The <see cref="IServiceProvider"/> that should provide services to the <see cref="IRealtimeSession"/> instances.
    /// If <see langword="null"/>, an empty <see cref="IServiceProvider"/> will be used.
    /// </param>
    /// <returns>An instance of <see cref="IRealtimeSession"/> that represents the entire pipeline.</returns>
    public IRealtimeSession Build(IServiceProvider? services = null)
    {
        services ??= EmptyServiceProvider.Instance;
        var session = _innerSessionFactory(services);

        // To match intuitive expectations, apply the factories in reverse order, so that the first factory added is the outermost.
        if (_sessionFactories is not null)
        {
            for (var i = _sessionFactories.Count - 1; i >= 0; i--)
            {
                session = _sessionFactories[i](session, services);
                if (session is null)
                {
                    Throw.InvalidOperationException(
                        $"The {nameof(RealtimeSessionBuilder)} entry at index {i} returned null. " +
                        $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(IRealtimeSession)} instances.");
                }
            }
        }

        return session;
    }

    /// <summary>Adds a factory for an intermediate realtime session to the realtime session pipeline.</summary>
    /// <param name="sessionFactory">The session factory function.</param>
    /// <returns>The updated <see cref="RealtimeSessionBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sessionFactory"/> is <see langword="null"/>.</exception>
    public RealtimeSessionBuilder Use(Func<IRealtimeSession, IRealtimeSession> sessionFactory)
    {
        _ = Throw.IfNull(sessionFactory);

        return Use((innerSession, _) => sessionFactory(innerSession));
    }

    /// <summary>Adds a factory for an intermediate realtime session to the realtime session pipeline.</summary>
    /// <param name="sessionFactory">The session factory function.</param>
    /// <returns>The updated <see cref="RealtimeSessionBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sessionFactory"/> is <see langword="null"/>.</exception>
    public RealtimeSessionBuilder Use(Func<IRealtimeSession, IServiceProvider, IRealtimeSession> sessionFactory)
    {
        _ = Throw.IfNull(sessionFactory);

        (_sessionFactories ??= []).Add(sessionFactory);
        return this;
    }

    /// <summary>
    /// Adds to the realtime session pipeline an anonymous delegating realtime session based on a delegate that provides
    /// an implementation for <see cref="IRealtimeSession.GetStreamingResponseAsync"/>.
    /// </summary>
    /// <param name="getStreamingResponseFunc">
    /// A delegate that provides the implementation for <see cref="IRealtimeSession.GetStreamingResponseAsync"/>.
    /// This delegate is invoked with the sequence of realtime client messages, a delegate that represents invoking
    /// the inner session, and a cancellation token. The delegate should be passed whatever client messages and
    /// cancellation token should be passed along to the next stage in the pipeline.
    /// </param>
    /// <returns>The updated <see cref="RealtimeSessionBuilder"/> instance.</returns>
    /// <remarks>
    /// This overload can be used when the anonymous implementation needs to provide pre-processing and/or post-processing
    /// for the streaming response.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="getStreamingResponseFunc"/> is <see langword="null"/>.</exception>
    public RealtimeSessionBuilder Use(
        Func<IAsyncEnumerable<RealtimeClientMessage>, IRealtimeSession, CancellationToken, IAsyncEnumerable<RealtimeServerMessage>> getStreamingResponseFunc)
    {
        _ = Throw.IfNull(getStreamingResponseFunc);

        return Use((innerSession, _) => new AnonymousDelegatingRealtimeSession(innerSession, getStreamingResponseFunc));
    }
}
