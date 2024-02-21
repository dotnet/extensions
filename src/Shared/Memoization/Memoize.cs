// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CA1716
namespace Microsoft.Shared.Memoization;
#pragma warning restore CA1716

/// <summary>
/// Given a function of arity N (1 &lt;= N &lt;= 2), return a function that behaves identically, except that
/// repeated invocations with the same parameters return a cached value rather than redoing the computation.
/// </summary>
/// <remarks>
/// Memoize is like a <see cref="Lazy{T}" />, but for functions instead of values.
/// Memoize will use the equality of the types of the input parameters. This means that arbitrary objects
/// will use reference equality, unless those types define their own equality semantics. This implies that
/// callers should take care to use types with Memoize that would be safe to put in a dictionary: if the
/// type is mutable, and its Equals/GetHashCode depends on those mutable parts, unexpected behaviour may
/// result.
/// </remarks>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

internal static class Memoize
{
    /// <summary>
    /// Returns a function that remembers the results of previous invocations of the given function.
    /// </summary>
    /// <typeparam name="TParameter">The function input type.</typeparam>
    /// <typeparam name="TResult">The function output type.</typeparam>
    /// <param name="f">The function that needs to be memoized.</param>
    /// <returns>A function that appears identical to the original function, but duplicate invocations are nearly instant.</returns>
    /// <remarks>
    /// Computed values consume memory. Garbage collection will free up that memory when the returned
    /// Func is freed. If you're computing values for large numbers of inputs, bear this in mind: if
    /// the Func lives for a long time, memory usage can increase without bound.
    /// </remarks>
    public static Func<TParameter, TResult> Function<TParameter, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TResult>(Func<TParameter, TResult> f)
        => new MemoizedFunction<TParameter, TResult>(f).Function;

    /// <summary>
    /// Returns a function that remembers the results of previous invocations of the given function.
    /// </summary>
    /// <typeparam name="TParameter1">The type of the function's first parameter.</typeparam>
    /// <typeparam name="TParameter2">The type of the function's second parameter.</typeparam>
    /// <typeparam name="TResult">The function output type.</typeparam>
    /// <param name="f">The function that needs to be memoized.</param>
    /// <returns>A function that appears identical to the original function, but duplicate invocations are nearly instant.</returns>
    /// <remarks>
    /// Computed values consume memory. Garbage collection will free up that memory when the returned
    /// Func is freed. If you're computing values for large numbers of inputs, bear this in mind: if
    /// the Func lives for a long time, memory usage can increase without bound.
    /// </remarks>
    public static Func<TParameter1, TParameter2, TResult> Function<TParameter1, TParameter2, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TResult>(
        Func<TParameter1, TParameter2, TResult> f)
        => new MemoizedFunction<TParameter1, TParameter2, TResult>(f).Function;
}
