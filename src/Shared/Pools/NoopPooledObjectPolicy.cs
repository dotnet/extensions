// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;

#pragma warning disable CA1716
namespace Microsoft.Shared.Pools;

/// <summary>
/// An object pool policy that does nothing.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal sealed class NoopPooledObjectPolicy<T> : PooledObjectPolicy<T>
    where T : notnull, new()
{
    public static NoopPooledObjectPolicy<T> Instance { get; } = new();

    private NoopPooledObjectPolicy()
    {
    }

    public override T Create() => new();
    public override bool Return(T obj) => true;
}
