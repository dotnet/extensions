// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides a collection of static methods for extending <see cref="IRealtimeClient"/> instances.</summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public static class RealtimeClientExtensions
{
    /// <summary>Asks the <see cref="IRealtimeClient"/> for an object of type <typeparamref name="TService"/>.</summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="client">The client.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that may be provided by the <see cref="IRealtimeClient"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    public static TService? GetService<TService>(this IRealtimeClient client, object? serviceKey = null)
    {
        _ = Throw.IfNull(client);

        return client.GetService(typeof(TService), serviceKey) is TService service ? service : default;
    }
}
