// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

#pragma warning disable CA1716
namespace Microsoft.Shared.Collections;
#pragma warning restore CA1716

/// <summary>
/// Defines static methods used to optimize the use of empty collections.
/// </summary>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

internal static class Empty
{
    /// <summary>
    /// Returns an optimized empty collection.
    /// </summary>
    /// <typeparam name="T">The type of the collection.</typeparam>
    /// <returns>Returns an efficient static instance of an empty collection.</returns>
    public static IReadOnlyCollection<T> ReadOnlyCollection<T>() => EmptyReadOnlyList<T>.Instance;

    /// <summary>
    /// Returns an optimized empty collection.
    /// </summary>
    /// <typeparam name="T">The type of the collection.</typeparam>
    /// <returns>Returns an efficient static instance of an empty collection.</returns>
    public static IEnumerable<T> Enumerable<T>() => EmptyReadOnlyList<T>.Instance;

    /// <summary>
    /// Returns an optimized empty collection.
    /// </summary>
    /// <typeparam name="T">The type of the collection.</typeparam>
    /// <returns>Returns an efficient static instance of an empty list.</returns>
    public static IReadOnlyList<T> ReadOnlyList<T>() => EmptyReadOnlyList<T>.Instance;

    /// <summary>
    /// Returns an optimized empty dictionary.
    /// </summary>
    /// <typeparam name="TKey">The key type of the dictionary.</typeparam>
    /// <typeparam name="TValue">The value type of the dictionary.</typeparam>
    /// <returns>Returns an efficient static instance of an empty dictionary.</returns>
    public static IReadOnlyDictionary<TKey, TValue> ReadOnlyDictionary<TKey, TValue>()
        where TKey : notnull
        => EmptyReadOnlyDictionary<TKey, TValue>.Instance;
}
