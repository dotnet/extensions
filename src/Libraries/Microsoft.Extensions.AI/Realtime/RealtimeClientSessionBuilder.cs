// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="IRealtimeClientSession"/>.</summary>
[Experimental("MEAI001")]
public sealed class RealtimeClientSessionBuilder
{
    private readonly Func<IServiceProvider, IRealtimeClientSession> _innerSessionFactory;

    /// <summary>The registered session factory instances.</summary>
    private List<Func<IRealtimeClientSession, IServiceProvider, IRealtimeClientSession>>? _sessionFactories;

    /// <summary>Initializes a new instance of the <see cref="RealtimeClientSessionBuilder"/> class.</summary>
    /// <param name="innerSession">The inner <see cref="IRealtimeClientSession"/> that represents the underlying backend.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerSession"/> is <see langword="null"/>.</exception>
    public RealtimeClientSessionBuilder(IRealtimeClientSession innerSession)
    {
        _ = Throw.IfNull(innerSession);
        _innerSessionFactory = _ => innerSession;
    }

    /// <summary>Initializes a new instance of the <see cref="RealtimeClientSessionBuilder"/> class.</summary>
    /// <param name="innerSessionFactory">A callback that produces the inner <see cref="IRealtimeClientSession"/> that represents the underlying backend.</param>
    public RealtimeClientSessionBuilder(Func<IServiceProvider, IRealtimeClientSession> innerSessionFactory)
    {
        _innerSessionFactory = Throw.IfNull(innerSessionFactory);
    }

    /// <summary>Builds an <see cref="IRealtimeClientSession"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.</summary>
    /// <param name="services">
    /// The <see cref="IServiceProvider"/> that should provide services to the <see cref="IRealtimeClientSession"/> instances.
    /// If <see langword="null"/>, an empty <see cref="IServiceProvider"/> will be used.
    /// </param>
    /// <returns>An instance of <see cref="IRealtimeClientSession"/> that represents the entire pipeline.</returns>
    public IRealtimeClientSession Build(IServiceProvider? services = null)
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
                        $"The {nameof(RealtimeClientSessionBuilder)} entry at index {i} returned null. " +
                        $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(IRealtimeClientSession)} instances.");
                }
            }
        }

        return session;
    }

    /// <summary>Adds a factory for an intermediate realtime session to the realtime session pipeline.</summary>
    /// <param name="sessionFactory">The session factory function.</param>
    /// <returns>The updated <see cref="RealtimeClientSessionBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sessionFactory"/> is <see langword="null"/>.</exception>
    public RealtimeClientSessionBuilder Use(Func<IRealtimeClientSession, IRealtimeClientSession> sessionFactory)
    {
        _ = Throw.IfNull(sessionFactory);

        return Use((innerSession, _) => sessionFactory(innerSession));
    }

    /// <summary>Adds a factory for an intermediate realtime session to the realtime session pipeline.</summary>
    /// <param name="sessionFactory">The session factory function.</param>
    /// <returns>The updated <see cref="RealtimeClientSessionBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sessionFactory"/> is <see langword="null"/>.</exception>
    public RealtimeClientSessionBuilder Use(Func<IRealtimeClientSession, IServiceProvider, IRealtimeClientSession> sessionFactory)
    {
        _ = Throw.IfNull(sessionFactory);

        (_sessionFactories ??= []).Add(sessionFactory);
        return this;
    }

}
