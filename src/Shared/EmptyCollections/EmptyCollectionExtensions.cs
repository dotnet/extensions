// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

internal static class EmptyCollectionExtensions
{
    /// <summary>
    /// Returns an optimized empty collection if the input is null or empty, otherwise returns the input.
    /// </summary>
    /// <typeparam name="T">The type of the collection.</typeparam>
    /// <param name="collection">The collection to check for null or empty.</param>
    /// <returns>Returns a static instance of an empty type if the input collection is null or empty, otherwise the collection.</returns>
    /// <remarks>
    /// Substituting a static collection whenever an empty collection is needed helps in two ways. First,
    /// it allows the original empty collection to be garbage collected, freeing memory. Second, the
    /// empty collection that is returned is optimized to not allocated memory whenever the collection is
    /// enumerated.
    /// </remarks>
    public static IReadOnlyCollection<T> EmptyIfNull<T>(this IReadOnlyCollection<T>? collection)
        => collection == null || collection.Count == 0 ? EmptyReadOnlyList<T>.Instance : collection;

    /// <summary>
    /// Returns an optimized empty collection if the input is null or empty, otherwise returns the input.
    /// </summary>
    /// <typeparam name="T">The type of the collection.</typeparam>
    /// <param name="collection">The collection to check for null or empty.</param>
    /// <returns>Returns a static instance of an empty type if the input collection is null or empty, otherwise the collection.</returns>
    /// <remarks>
    /// Substituting a static collection whenever an empty collection is needed helps in two ways. First,
    /// it allows the original empty collection to be garbage collected, freeing memory. Second, the
    /// empty collection that is returned is optimized to not allocated memory whenever the collection is
    /// enumerated.
    /// </remarks>
    public static IEnumerable<T> EmptyIfNull<T>(this ICollection<T>? collection)
        => collection == null || collection.Count == 0 ? EmptyReadOnlyList<T>.Instance : collection;

    /// <summary>
    /// Returns an optimized empty collection if the input is null or empty, otherwise returns the input.
    /// </summary>
    /// <typeparam name="T">The type of the collection.</typeparam>
    /// <param name="list">The collection to check for null or empty.</param>
    /// <returns>Returns a static instance of an empty type if the input collection is null or empty, otherwise the collection.</returns>
    /// <remarks>
    /// Substituting a static collection whenever an empty collection is needed helps in two ways. First,
    /// it allows the original empty collection to be garbage collected, freeing memory. Second, the
    /// empty collection that is returned is optimized to not allocated memory whenever the collection is
    /// enumerated.
    /// </remarks>
    public static IReadOnlyList<T> EmptyIfNull<T>(this IReadOnlyList<T>? list)
        => list == null || list.Count == 0 ? EmptyReadOnlyList<T>.Instance : list;

    /// <summary>
    /// Returns an optimized empty list if the input is null or empty, otherwise returns the input.
    /// </summary>
    /// <typeparam name="T">The type of the collection.</typeparam>
    /// <param name="list">The list to check for null or empty.</param>
    /// <returns>Returns a static instance of an empty type if the input collection is null or empty, otherwise the collection.</returns>
    /// <remarks>
    /// Substituting a static list whenever an empty collection is needed helps in two ways. First,
    /// it allows the original empty collection to be garbage collected, freeing memory. Second, the
    /// empty collection that is returned is optimized to not allocated memory whenever the collection is
    /// enumerated.
    /// </remarks>
    public static IEnumerable<T> EmptyIfNull<T>(this IList<T>? list)
        => list == null || list.Count == 0 ? EmptyReadOnlyList<T>.Instance : list;

    /// <summary>
    /// Returns an optimized empty array if the input is null or empty, otherwise returns the input.
    /// </summary>
    /// <typeparam name="T">The type of the array.</typeparam>
    /// <param name="array">The array to check for null or empty.</param>
    /// <returns>Returns a static instance of an empty array if the input array is null or empty, otherwise the array.</returns>
    public static T[] EmptyIfNull<T>(this T[]? array)
        => array == null || array.Length == 0 ? Array.Empty<T>() : array;

    /// <summary>
    /// Returns an optimized empty collection if the input is null or can be determined to be empty, otherwise returns the input.
    /// </summary>
    /// <typeparam name="T">The type of the collection.</typeparam>
    /// <param name="enumerable">The collection to check for null or empty.</param>
    /// <returns>Returns a static instance of an empty type if the input collection is null or empty, otherwise the collection.</returns>
    /// <remarks>
    /// Note that this method does not enumerate the colleciton.
    /// </remarks>
    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? enumerable)
    {
        if (enumerable == null)
        {
            return EmptyReadOnlyList<T>.Instance;
        }

        // note this takes care of the IReadOnlyList<T> case too
        if (enumerable is IReadOnlyCollection<T> rc && rc.Count == 0)
        {
            return EmptyReadOnlyList<T>.Instance;
        }

        // note this takes care of the IList<T> case too
        if (enumerable is ICollection<T> c && c.Count == 0)
        {
            return EmptyReadOnlyList<T>.Instance;
        }

        return enumerable;
    }

    /// <summary>
    /// Returns an optimized empty dictionary if the input is null or can be determined to be empty, otherwise returns the input.
    /// </summary>
    /// <typeparam name="TKey">The key type of the dictionary.</typeparam>
    /// <typeparam name="TValue">The value type of the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to check for null or empty.</param>
    /// <returns>Returns a static instance of an empty type if the input dictionary is null or empty, otherwise the dictionary.</returns>
    /// <remarks>
    /// Note that this method does not enumerate the dictionary.
    /// </remarks>
    public static IReadOnlyDictionary<TKey, TValue> EmptyIfNull<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue>? dictionary)
        where TKey : notnull
    {
        if (dictionary == null || dictionary.Count == 0)
        {
            return EmptyReadOnlyDictionary<TKey, TValue>.Instance;
        }

        return dictionary;
    }
}
