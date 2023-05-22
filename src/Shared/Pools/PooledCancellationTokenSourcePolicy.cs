// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.Extensions.ObjectPool;

#pragma warning disable CA1716
namespace Microsoft.Shared.Pools;

/// <summary>
/// An object pool policy for cancellation token sources.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal sealed class PooledCancellationTokenSourcePolicy : PooledObjectPolicy<CancellationTokenSource>
{
    public static PooledCancellationTokenSourcePolicy Instance { get; } = new();

    private PooledCancellationTokenSourcePolicy()
    {
    }

    public override CancellationTokenSource Create() => new();

    public override bool Return(CancellationTokenSource obj)
    {
#if NET6_0_OR_GREATER
        if (obj.TryReset())
        {
            return true;
        }
#endif

        obj.Dispose();
        return false;
    }
}
