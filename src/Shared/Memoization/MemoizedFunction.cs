// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

#pragma warning disable CA1716
namespace Microsoft.Shared.Memoization;
#pragma warning restore CA1716

#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Memoizer for functions of arity 1.
/// </summary>
/// <remarks>
/// We don't use weak references because those can only wrap reference types, and we wish to support functions that return other kinds of values.
/// </remarks>
/// <typeparam name="TParameter">Input parameter type for the memoized function.</typeparam>
/// <typeparam name="TResult">Return type for the memoized function.</typeparam>

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

[DebuggerDisplay("{_values.Count} memoized values")]
internal sealed class MemoizedFunction<TParameter, TResult>
{
    private const int Concurrency = 10;
    private const int Capacity = 100;

    // Using a readonly struct means that we can assert in the ConcurrentDictionary
    // declaration that all keys are non-null. Otherwise we need to say so in the
    // type declaration ("where TParameter : notnull"), which forces users to care.
    internal readonly struct Arg : IEquatable<MemoizedFunction<TParameter, TResult>.Arg>
    {
        private readonly int _hash;

        public Arg(TParameter arg1)
        {
            Arg1 = arg1;
            _hash = Arg1?.GetHashCode() ?? 0;
        }

        public readonly TParameter Arg1;

        public override bool Equals(object? obj) => obj is MemoizedFunction<TParameter, TResult>.Arg arg && Equals(arg);

        public bool Equals(MemoizedFunction<TParameter, TResult>.Arg other) => EqualityComparer<TParameter>.Default.Equals(Arg1, other.Arg1);

        public override int GetHashCode() => _hash;
    }

    private readonly ConcurrentDictionary<Arg, Lazy<TResult>> _values;

    private readonly Func<TParameter, TResult> _function;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoizedFunction{TParameter1, TResult}"/> class.
    /// </summary>
    /// <param name="function">The function whose results will be memoized.</param>
    public MemoizedFunction(Func<TParameter, TResult> function)
    {
        _function = Throw.IfNull(function);
        _values = new(Concurrency, Capacity);
    }

    internal TResult Function(TParameter arg1)
    {
        var arg = new Arg(arg1);

        // Stryker disable once all
        if (_values.TryGetValue(arg, out var result))
        {
            return result.Value;
        }

        return _values.GetOrAdd(arg, new Lazy<TResult>(() => _function(arg1))).Value;
    }
}

/// <summary>
/// Memoizer for functions of arity 2.
/// </summary>
/// <typeparam name="TParameter1">First input parameter type for the memoized function.</typeparam>
/// <typeparam name="TParameter2">Second input parameter type for the memoized function.</typeparam>
/// <typeparam name="TResult">Return type for the memoized function.</typeparam>

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

[SuppressMessage(
        "Major Code Smell",
        "S2436:Types and methods should not have too many generic parameters",
        Justification = "We're using many generic types for the same reason Func<>, Func<,>, Func<,,>, ... exist.")]
[DebuggerDisplay("{_values.Count} memoized values")]
internal sealed class MemoizedFunction<TParameter1, TParameter2, TResult>
{
    private const int Concurrency = 10;
    private const int Capacity = 100;

    internal readonly struct Args : IEquatable<MemoizedFunction<TParameter1, TParameter2, TResult>.Args>
    {
        private readonly int _hash;

        public Args(TParameter1 arg1, TParameter2 arg2)
        {
            Arg1 = arg1;
            Arg2 = arg2;
            _hash = HashCode.Combine(Arg1, Arg2);
        }

        public readonly TParameter1 Arg1;

        public readonly TParameter2 Arg2;

        public override bool Equals(object? obj) => obj is MemoizedFunction<TParameter1, TParameter2, TResult>.Args args && Equals(args);

        public bool Equals(MemoizedFunction<TParameter1, TParameter2, TResult>.Args other) =>
               EqualityComparer<TParameter1>.Default.Equals(Arg1, other.Arg1)
            && EqualityComparer<TParameter2>.Default.Equals(Arg2, other.Arg2);

        public override int GetHashCode() => _hash;
    }

    private readonly ConcurrentDictionary<Args, Lazy<TResult>> _values;

    private readonly Func<TParameter1, TParameter2, TResult> _function;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoizedFunction{TParameter1, TParameter2, TResult}"/> class.
    /// </summary>
    /// <param name="function">The function whose results will be memoized.</param>
    public MemoizedFunction(Func<TParameter1, TParameter2, TResult> function)
    {
        _function = Throw.IfNull(function);
        _values = new(Concurrency, Capacity);
    }

    internal TResult Function(TParameter1 arg1, TParameter2 arg2)
    {
        var args = new Args(arg1, arg2);

        // Stryker disable once all
        if (_values.TryGetValue(args, out var result))
        {
            return result.Value;
        }

        return _values.GetOrAdd(args, new Lazy<TResult>(() => _function(arg1, arg2))).Value;
    }
}

/// <summary>
/// Memoizer for functions of arity 3.
/// </summary>
/// <typeparam name="TParameter1">First input parameter type for the memoized function.</typeparam>
/// <typeparam name="TParameter2">Second input parameter type for the memoized function.</typeparam>
/// <typeparam name="TParameter3">Third input parameter type for the memoized function.</typeparam>
/// <typeparam name="TResult">Return type for the memoized function.</typeparam>

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

[SuppressMessage(
    "Major Code Smell",
    "S2436:Types and methods should not have too many generic parameters",
    Justification = "We're using many generic types for the same reason Func<>, Func<,>, Func<,,>, ... exist.")]
[DebuggerDisplay("{_values.Count} memoized values")]
internal sealed class MemoizedFunction<TParameter1, TParameter2, TParameter3, TResult>
{
    private const int Concurrency = 10;
    private const int Capacity = 100;

    internal readonly struct Args : IEquatable<MemoizedFunction<TParameter1, TParameter2, TParameter3, TResult>.Args>
    {
        private readonly int _hash;

        public Args(TParameter1 arg1, TParameter2 arg2, TParameter3 arg3)
        {
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;

            _hash = HashCode.Combine(Arg1, Arg2, Arg3);
        }

        public readonly TParameter1 Arg1;

        public readonly TParameter2 Arg2;

        public readonly TParameter3 Arg3;

        public override bool Equals(object? obj) =>
            obj is MemoizedFunction<TParameter1, TParameter2, TParameter3, TResult>.Args args && Equals(args);

        public bool Equals(MemoizedFunction<TParameter1, TParameter2, TParameter3, TResult>.Args other) =>
               EqualityComparer<TParameter1>.Default.Equals(Arg1, other.Arg1)
            && EqualityComparer<TParameter2>.Default.Equals(Arg2, other.Arg2)
            && EqualityComparer<TParameter3>.Default.Equals(Arg3, other.Arg3);

        public override int GetHashCode() => _hash;
    }

    private readonly ConcurrentDictionary<Args, Lazy<TResult>> _values;

    private readonly Func<TParameter1, TParameter2, TParameter3, TResult> _function;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoizedFunction{TParameter1, TParameter2, TParameter3, TResult}"/> class.
    /// </summary>
    /// <param name="function">The function whose results will be memoized.</param>
    public MemoizedFunction(Func<TParameter1, TParameter2, TParameter3, TResult> function)
    {
        _function = Throw.IfNull(function);
        _values = new(Concurrency, Capacity);
    }

    internal TResult Function(TParameter1 arg1, TParameter2 arg2, TParameter3 arg3)
    {
        var args = new Args(arg1, arg2, arg3);

        // Stryker disable once all
        if (_values.TryGetValue(args, out var result))
        {
            return result.Value;
        }

        return _values.GetOrAdd(args, new Lazy<TResult>(() => _function(arg1, arg2, arg3))).Value;
    }
}
