// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Concurrent;

/// <summary>
/// Extensions for <see cref="ConcurrentDictionary{Tkey,TValue}"/>.
/// </summary>
internal static class GetOrAddExtensions
{
    /// <summary>
    /// Adds a key/value pair to a concurrent dictionary by using the specified function and an argument if the key does not already exist, or returns the existing value if the key exists.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <typeparam name="TArg">The type of state to pass to the value factory.</typeparam>
    /// <param name="dictionary">The dictionary to operate on.</param>
    /// <param name="key">The key of the value to query or add.</param>
    /// <param name="valueFactory">A function that returns a value to insert into the dictionary if it is not already present.</param>
    /// <param name="factoryArgument">The state to pass to the value factory.</param>
    /// <returns>The value for the key. This will be either the existing value for the key if the key is already in the dictionary, or the new value if the key was not in the dictionary.</returns>
    /// <exception cref="ArgumentNullException">The dictionary, key, or value factory is <see langword="null"/>.</exception>
    [ExcludeFromCodeCoverage]
    public static TValue GetOrAdd<TKey, TValue, TArg>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
    {
        if (dictionary.TryGetValue(key, out TValue value))
        {
            return value;
        }

        return dictionary.GetOrAdd(key, valueFactory(key, factoryArgument));
    }
}
