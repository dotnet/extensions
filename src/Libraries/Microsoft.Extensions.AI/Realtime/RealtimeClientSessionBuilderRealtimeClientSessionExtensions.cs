// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="IRealtimeClientSession"/> in the context of <see cref="RealtimeClientSessionBuilder"/>.</summary>
[Experimental("MEAI001")]
public static class RealtimeClientSessionBuilderRealtimeClientSessionExtensions
{
    /// <summary>Creates a new <see cref="RealtimeClientSessionBuilder"/> using <paramref name="innerSession"/> as its inner session.</summary>
    /// <param name="innerSession">The session to use as the inner session.</param>
    /// <returns>The new <see cref="RealtimeClientSessionBuilder"/> instance.</returns>
    /// <remarks>
    /// This method is equivalent to using the <see cref="RealtimeClientSessionBuilder"/> constructor directly,
    /// specifying <paramref name="innerSession"/> as the inner session.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="innerSession"/> is <see langword="null"/>.</exception>
    public static RealtimeClientSessionBuilder AsBuilder(this IRealtimeClientSession innerSession)
    {
        _ = Throw.IfNull(innerSession);

        return new RealtimeClientSessionBuilder(innerSession);
    }
}
