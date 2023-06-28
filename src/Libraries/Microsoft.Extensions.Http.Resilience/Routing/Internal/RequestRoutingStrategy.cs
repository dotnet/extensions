// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.Http.Resilience.Routing.Internal;

/// <summary>
/// Defines a strategy for retrieval of route URLs,
/// used to route one request across a set of different endpoints.
/// </summary>
internal abstract class RequestRoutingStrategy : IResettable, IDisposable
{
    protected RequestRoutingStrategy(Randomizer randomizer)
    {
        Randomizer = randomizer;
    }

    public Randomizer Randomizer { get; }

    /// <summary>
    /// Gets the next route Uri.
    /// </summary>
    /// <param name="nextRoute">Holds next route value, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if next route available, <see langword="false"/> otherwise.</returns>
    public abstract bool TryGetNextRoute([NotNullWhen(true)] out Uri? nextRoute);

    public abstract bool TryReset();

    public abstract void Dispose();
}
