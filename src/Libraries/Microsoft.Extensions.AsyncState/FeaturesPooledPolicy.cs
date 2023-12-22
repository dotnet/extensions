// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.AsyncState;

internal sealed class FeaturesPooledPolicy : IPooledObjectPolicy<Features>
{
    /// <inheritdoc/>
    public Features Create()
    {
        return new Features();
    }

    /// <inheritdoc/>
    public bool Return(Features obj)
    {
        obj.Clear();
        return true;
    }
}
