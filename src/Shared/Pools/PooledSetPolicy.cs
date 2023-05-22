// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

#pragma warning disable CA1716
namespace Microsoft.Shared.Pools;

/// <summary>
/// An object pool policy for sets.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal sealed class PooledSetPolicy<T> : PooledObjectPolicy<HashSet<T>>
    where T : notnull
{
    private readonly IEqualityComparer<T>? _comparer;

    public PooledSetPolicy(IEqualityComparer<T>? comparer = null)
    {
        _comparer = comparer;
    }

    public override HashSet<T> Create() => new(_comparer);

    public override bool Return(HashSet<T> obj)
    {
        obj.Clear();
        return true;
    }
}
