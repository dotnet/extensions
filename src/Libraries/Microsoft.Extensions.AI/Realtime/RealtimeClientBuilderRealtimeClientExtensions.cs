// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="IRealtimeClient"/> in the context of <see cref="RealtimeClientBuilder"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public static class RealtimeClientBuilderRealtimeClientExtensions
{
    /// <summary>Creates a new <see cref="RealtimeClientBuilder"/> using <paramref name="innerClient"/> as its inner client.</summary>
    /// <param name="innerClient">The client to use as the inner client.</param>
    /// <returns>The new <see cref="RealtimeClientBuilder"/> instance.</returns>
    /// <remarks>
    /// This method is equivalent to using the <see cref="RealtimeClientBuilder"/> constructor directly,
    /// specifying <paramref name="innerClient"/> as the inner client.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    public static RealtimeClientBuilder AsBuilder(this IRealtimeClient innerClient)
    {
        _ = Throw.IfNull(innerClient);

        return new RealtimeClientBuilder(innerClient);
    }
}
