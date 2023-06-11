// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic;

/// <summary>
/// Extensions for <see cref="IDictionary{Tkey,TValue}"/>.
/// </summary>
internal static class DictionaryExtensions
{
    /// <summary>
    /// Tries to remove the value with the specified key from the dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to operate on.</param>
    /// <param name="key">The key of the value to query or add.</param>
    /// <param name="value">When this method returns true, the removed value; when this method returns false, the default value for <typeparamref name="TValue" />.</param>
    /// <returns>true when a value is found in the dictionary with the specified key; false when the dictionary cannot find a value associated with the specified key.</returns>
    /// <exception cref="ArgumentNullException">The dictionary or key is <see langword="null"/>.</exception>
    [ExcludeFromCodeCoverage]
    public static bool Remove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (dictionary.TryGetValue(key, out value))
        {
            _ = dictionary.Remove(key);
            return true;
        }

        value = default;
        return false;
    }
}
