// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.AsyncState;

internal sealed class FeaturesPooledPolicy : IPooledObjectPolicy<ConcurrentDictionary<AsyncStateToken, object?>>
{
    /// <inheritdoc/>
    public ConcurrentDictionary<AsyncStateToken, object?> Create()
    {
        return [];
    }

    /// <inheritdoc/>
    public bool Return(ConcurrentDictionary<AsyncStateToken, object?> obj)
    {
        obj.Clear();
        return true;
    }
}
