// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides a collection of static methods for extending <see cref="IChatClient"/> instances.</summary>
[Experimental("MEAI001")]
public static class RealtimeSessionExtensions
{
    /// <summary>Asks the <see cref="IRealtimeSession"/> for an object of type <typeparamref name="TService"/>.</summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="session">The session.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that may be provided by the <see cref="IRealtimeSession"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    public static TService? GetService<TService>(this IRealtimeSession session, object? serviceKey = null)
    {
        _ = Throw.IfNull(session);

        return session.GetService(typeof(TService), serviceKey) is TService service ? service : default;
    }
}
