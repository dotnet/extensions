// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;

#pragma warning disable CA1716
namespace Microsoft.Shared.Pools;

#pragma warning disable R9A038

/// <summary>
/// A factory of object pools.
/// </summary>
/// <remarks>
/// This class makes it easy to create efficient object pools used to improve performance by reducing
/// strain on the garbage collector.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal static class PoolFactory
{
    internal const int DefaultCapacity = 1024;
    private const int DefaultMaxStringBuilderCapacity = 64 * 1024;
    private const int InitialStringBuilderCapacity = 128;

    private static readonly IPooledObjectPolicy<StringBuilder> _defaultStringBuilderPolicy = new StringBuilderPooledObjectPolicy
    {
        InitialCapacity = InitialStringBuilderCapacity,
        MaximumRetainedCapacity = DefaultCapacity
    };

    /// <summary>
    /// Creates an object pool.
    /// </summary>
    /// <typeparam name="T">The type of object to keep in the pool.</typeparam>
    /// <param name="maxCapacity">The maximum number of items to keep in the pool. This defaults to 1024. This value is a recommendation, the pool may keep more objects than this.</param>
    /// <returns>The pool.</returns>
    public static ObjectPool<T> CreatePool<T>(int maxCapacity = DefaultCapacity)
        where T : class, new()
    {
        _ = Throw.IfLessThan(maxCapacity, 1);

        return MakePool(NoopPooledObjectPolicy<T>.Instance, maxCapacity);
    }

    /// <summary>
    /// Creates an object pool with a custom policy.
    /// </summary>
    /// <typeparam name="T">The type of object to keep in the pool.</typeparam>
    /// <param name="policy">The custom policy that is responsible for creating new objects and preparing objects to be added to the pool.</param>
    /// <param name="maxCapacity">The maximum number of items to keep in the pool. This defaults to 1024. This value is a recommendation, the pool may keep more objects than this.</param>
    /// <returns>The pool.</returns>
    public static ObjectPool<T> CreatePool<T>(IPooledObjectPolicy<T> policy, int maxCapacity = DefaultCapacity)
        where T : class
    {
        _ = Throw.IfNull(policy);
        _ = Throw.IfLessThan(maxCapacity, 1);

        return MakePool(policy, maxCapacity);
    }

    /// <summary>
    /// Creates an object pool for resettable objects.
    /// </summary>
    /// <typeparam name="T">The type of object to keep in the pool.</typeparam>
    /// <param name="maxCapacity">The maximum number of items to keep in the pool. This defaults to 1024. This value is a recommendation, the pool may keep more objects than this.</param>
    /// <returns>The pool.</returns>
    /// <remarks>
    /// Objects are systematically reset before being added to the pool.
    /// </remarks>
    public static ObjectPool<T> CreateResettingPool<T>(int maxCapacity = DefaultCapacity)
        where T : class, IResettable, new()
    {
        _ = Throw.IfLessThan(maxCapacity, 1);

        return MakePool(new DefaultPooledObjectPolicy<T>(), maxCapacity);
    }

    /// <summary>
    /// Creates a pool of <see cref="StringBuilder"/> instances.
    /// </summary>
    /// <param name="maxCapacity">The maximum number of items to keep in the pool. This defaults to 1024. This value is a recommendation, the pool may keep more objects than this.</param>
    /// <param name="maxStringBuilderCapacity">The maximum capacity of the string builders to keep in the pool. This defaults to 64K.</param>
    /// <returns>The pool.</returns>
    public static ObjectPool<StringBuilder> CreateStringBuilderPool(int maxCapacity = DefaultCapacity, int maxStringBuilderCapacity = DefaultMaxStringBuilderCapacity)
    {
        _ = Throw.IfLessThan(maxCapacity, 1);
        _ = Throw.IfLessThan(maxStringBuilderCapacity, 1);

        if (maxStringBuilderCapacity == DefaultMaxStringBuilderCapacity)
        {
            return MakePool(_defaultStringBuilderPolicy, maxCapacity);
        }

        return MakePool(
            new StringBuilderPooledObjectPolicy
            {
                InitialCapacity = InitialStringBuilderCapacity,
                MaximumRetainedCapacity = maxStringBuilderCapacity
            }, maxCapacity);
    }

    /// <summary>
    /// Creates an object pool of <see cref="List{T}"/> instances.
    /// </summary>
    /// <typeparam name="T">The type of object held by the lists.</typeparam>
    /// <param name="maxCapacity">
    /// The maximum number of items to keep in the pool.
    /// This defaults to 1024.
    /// This value is a recommendation, the pool may keep more objects than this.
    /// </param>
    /// <returns>The pool.</returns>
    public static ObjectPool<List<T>> CreateListPool<T>(int maxCapacity = DefaultCapacity)
    {
        _ = Throw.IfLessThan(maxCapacity, 1);

        return MakePool(PooledListPolicy<T>.Instance, maxCapacity);
    }

    /// <summary>
    /// Creates an object pool of <see cref="Dictionary{TKey, TValue}"/> instances.
    /// </summary>
    /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
    /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
    /// <param name="comparer">Optional key comparer used by the dictionaries.</param>
    /// <param name="maxCapacity">
    /// The maximum number of items to keep in the pool.
    /// This defaults to 1024.
    /// This value is a recommendation, the pool may keep more objects than this.
    /// </param>
    /// <returns>The pool.</returns>
    public static ObjectPool<Dictionary<TKey, TValue>> CreateDictionaryPool<TKey, TValue>(IEqualityComparer<TKey>? comparer = null, int maxCapacity = DefaultCapacity)
        where TKey : notnull
    {
        _ = Throw.IfLessThan(maxCapacity, 1);

        return MakePool(new PooledDictionaryPolicy<TKey, TValue>(comparer), maxCapacity);
    }

    /// <summary>
    /// Creates an object pool of <see cref="HashSet{T}"/> instances.
    /// </summary>
    /// <typeparam name="T">The type of objects held in the sets.</typeparam>
    /// <param name="comparer">Optional key comparer used by the sets.</param>
    /// <param name="maxCapacity">
    /// The maximum number of items to keep in the pool.
    /// This defaults to 1024.
    /// This value is a recommendation, the pool may keep more objects than this.
    /// </param>
    /// <returns>The pool.</returns>
    public static ObjectPool<HashSet<T>> CreateHashSetPool<T>(IEqualityComparer<T>? comparer = null, int maxCapacity = DefaultCapacity)
        where T : notnull
    {
        _ = Throw.IfLessThan(maxCapacity, 1);

        return MakePool(new PooledSetPolicy<T>(comparer), maxCapacity);
    }

    /// <summary>
    /// Creates an object pool of <see cref="CancellationTokenSource" /> instances.
    /// </summary>
    /// <param name="maxCapacity">
    /// The maximum number of items to keep in the pool.
    /// This defaults to 1024.
    /// This value is a recommendation, the pool may keep more objects than this.
    /// </param>
    /// <returns>The pool.</returns>
    /// <remarks>
    /// On .NET 6 and above, cancellation token sources are reusable and this pool leverages this feature.
    /// When running on older frameworks, this pool is actually a no-op, every time a source is fetched
    /// from the pool, it is always a new instance. In that case, returning an object to the pool merely
    /// disposes it.
    /// </remarks>
    public static ObjectPool<CancellationTokenSource> CreateCancellationTokenSourcePool(int maxCapacity = DefaultCapacity)
    {
        _ = Throw.IfLessThan(maxCapacity, 1);

        return MakePool(PooledCancellationTokenSourcePolicy.Instance, maxCapacity);
    }

    /// <summary>
    /// Gets the shared pool of <see cref="StringBuilder"/> instances.
    /// </summary>
    public static ObjectPool<StringBuilder> SharedStringBuilderPool { get; } = CreateStringBuilderPool();

    private static DefaultObjectPool<T> MakePool<T>(IPooledObjectPolicy<T> policy, int maxRetained)
        where T : class
        => new(policy, maxRetained);
}
