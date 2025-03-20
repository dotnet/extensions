// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Shared.Pools;

/// <summary>
/// An object pool policy for lists with capacity.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal sealed class PooledListWithCapacityPolicy<T> : PooledObjectPolicy<List<T>>
{
    private readonly int _listCapacity;
    public static PooledListWithCapacityPolicy<T> Instance(int listCapacity) => new(listCapacity);

    private PooledListWithCapacityPolicy(int listCapacity)
    {
        _listCapacity = listCapacity;
    }

    public override List<T> Create() => new(_listCapacity);

    public override bool Return(List<T> obj)
    {
        obj.Clear();
        return true;
    }
}
