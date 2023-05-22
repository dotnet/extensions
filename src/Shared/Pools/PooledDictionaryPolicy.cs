// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

#pragma warning disable CA1716
namespace Microsoft.Shared.Pools;

/// <summary>
/// An object pool policy for dictionaries.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal sealed class PooledDictionaryPolicy<TKey, TValue> : PooledObjectPolicy<Dictionary<TKey, TValue>>
    where TKey : notnull
{
    private readonly IEqualityComparer<TKey>? _comparer;

    public PooledDictionaryPolicy(IEqualityComparer<TKey>? comparer = null)
    {
        _comparer = comparer;
    }

    public override Dictionary<TKey, TValue> Create() => new(_comparer);

    public override bool Return(Dictionary<TKey, TValue> obj)
    {
        obj.Clear();
        return true;
    }
}
