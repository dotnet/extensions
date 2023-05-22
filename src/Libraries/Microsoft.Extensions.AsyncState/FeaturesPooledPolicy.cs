// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.AsyncState;

internal sealed class FeaturesPooledPolicy : IPooledObjectPolicy<List<object?>>
{
    /// <inheritdoc/>
    public List<object?> Create()
    {
        return new List<object?>();
    }

    /// <inheritdoc/>
    public bool Return(List<object?> obj)
    {
        for (int i = 0; i < obj.Count; i++)
        {
            obj[i] = null;
        }

        return true;
    }
}
