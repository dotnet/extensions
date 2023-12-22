// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.AsyncState;

internal sealed class Features
{
    private readonly List<object?> _items = [];

    public object? Get(int index)
    {
        return _items.Count <= index ? null : _items[index];
    }

    public void Set(int index, object? value)
    {
        if (_items.Count <= index)
        {
            lock (_items)
            {
                var count = index + 1;

#if NET6_0_OR_GREATER
                _items.EnsureCapacity(count);
#endif

                var difference = count - _items.Count;

                for (int i = 0; i < difference; i++)
                {
                    _items.Add(null);
                }
            }
        }

        _items[index] = value;
    }

    public void Clear()
    {
        _items.Clear();
    }
}
