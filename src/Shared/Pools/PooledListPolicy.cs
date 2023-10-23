// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

#pragma warning disable CA1716
namespace Microsoft.Shared.Pools;

/// <summary>
/// An object pool policy for lists.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal sealed class PooledListPolicy<T> : PooledObjectPolicy<List<T>>
{
    public static PooledListPolicy<T> Instance { get; } = new();

    private PooledListPolicy()
    {
    }

    public override List<T> Create() => [];

    public override bool Return(List<T> obj)
    {
        obj.Clear();
        return true;
    }
}
