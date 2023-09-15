// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Latency.Internal;

/// <summary>
/// Provides registry functionality.
/// </summary>
internal sealed class Registry
{
    private readonly FrozenDictionary<string, int> _keyOrder;

    private readonly bool _throwOnUnregisteredKeyLookup;

    /// <summary>
    /// Initializes a new instance of the <see cref="Registry"/> class.
    /// </summary>
    /// <param name="keys">Set of keys to be registered.</param>
    /// <param name="throwOnUnregisteredKeyLookup">Throws when getting order for unregistered keys if true.</param>
    public Registry(string[] keys, bool throwOnUnregisteredKeyLookup)
    {
        // Order the keys
        Array.Sort(keys);
        OrderedKeys = keys;

        // Create lookup for key order
        int c = OrderedKeys.Length;
        var keyOrderBuilder = new Dictionary<string, int>(c);
        for (int i = 0; i < c; i++)
        {
            if (OrderedKeys[i] == null)
            {
                Throw.ArgumentException(nameof(keys), "Supplied set contains null values");
            }

            keyOrderBuilder.Add(OrderedKeys[i], i);
        }

        _keyOrder = keyOrderBuilder.ToFrozenDictionary(StringComparer.Ordinal);
        _throwOnUnregisteredKeyLookup = throwOnUnregisteredKeyLookup;
    }

    /// <summary>
    /// Gets the list of registered keys, ordered using default comparator.
    /// </summary>
    /// <returns>List of ordered keys.</returns>
    public string[] OrderedKeys { get; }

    /// <summary>
    /// Gets the number of registered keys.
    /// </summary>
    public int KeyCount => OrderedKeys.Length;

    /// <summary>
    /// Gets the zero-based order of registered key.
    /// </summary>
    /// <param name="key">The key to get order for.</param>
    /// <returns>Index of key. Returns -1 if not registered.</returns>
    public int GetRegisteredKeyIndex(string key)
    {
        _ = Throw.IfNull(key);

        if (_keyOrder.TryGetValue(key, out var order))
        {
            return order;
        }
        else if (_throwOnUnregisteredKeyLookup)
        {
            Throw.ArgumentException(nameof(key), $"Name {key} has not been registered.");
        }

        return -1;
    }
}
