// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a real-time client.</summary>
/// <remarks>This interface provides methods to create and manage real-time sessions.</remarks>
[Experimental("MEAI001")]
public interface IRealtimeClient : IDisposable
{
    /// <summary>Creates a new real-time session with the specified options.</summary>
    /// <param name="options">The session options.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created real-time session.</returns>
    Task<IRealtimeSession?> CreateSessionAsync(RealtimeSessionOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Asks the <see cref="IRealtimeClient"/> for an object of the specified type <paramref name="serviceType"/>.</summary>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that might be provided by the <see cref="IRealtimeClient"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    object? GetService(Type serviceType, object? serviceKey = null);
}
