// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

#pragma warning disable SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning disable SA1112 // Closing parenthesis should be on line of opening parenthesis
#pragma warning disable SA1114 // Parameter list should follow declaration
#pragma warning disable S3358 // Extract this nested ternary operation into an independent statement.
#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S4039 // Make 'AIFunctionArguments' sealed
#pragma warning disable CA1033 // Make 'AIFunctionArguments' sealed
#pragma warning disable CA1710 // Identifiers should have correct suffix

namespace Microsoft.Extensions.AI;

/// <summary>Represents arguments to be used with <see cref="AIFunction.InvokeAsync"/>.</summary>
/// <remarks>
/// <see cref="AIFunctionArguments"/> is a dictionary of name/value pairs that are used
/// as inputs to an <see cref="AIFunction"/>. However, an instance carries additional non-nominal
/// information, such as an optional <see cref="IServiceProvider"/> that can be used by
/// an <see cref="AIFunction"/> if it needs to resolve any services from a dependency injection
/// container.
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
public class AIFunctionArguments : IDictionary<string, object?>, IReadOnlyDictionary<string, object?>
{
    /// <summary>The nominal arguments.</summary>
    private readonly Dictionary<string, object?> _arguments;

    /// <summary>Initializes a new instance of the <see cref="AIFunctionArguments"/> class, and uses the default comparer for key comparisons.</summary>
    /// <remarks>The <see cref="IEqualityComparer{T}"/> is ordinal by default.</remarks>
    public AIFunctionArguments()
        : this(null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIFunctionArguments"/> class containing
    /// the specified <paramref name="arguments"/>.
    /// </summary>
    /// <param name="arguments">The arguments represented by this instance.</param>
    /// <remarks>
    /// The <paramref name="arguments"/> reference will be stored if the instance is
    /// already a <see cref="Dictionary{TKey, TValue}"/>, in which case all dictionary
    /// operations on this instance will be routed directly to that instance. If <paramref name="arguments"/>
    /// is not a dictionary, a shallow clone of its data will be used to populate this
    /// instance. A <see langword="null"/> <paramref name="arguments"/> is treated as an
    /// empty parameters dictionary.
    /// The <see cref="IEqualityComparer{T}"/> is ordinal by default.
    /// </remarks>
    public AIFunctionArguments(IDictionary<string, object?>? arguments)
        : this(arguments, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AIFunctionArguments"/> class.</summary>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use for key comparisons.</param>
    public AIFunctionArguments(IEqualityComparer<string>? comparer)
        : this(null, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIFunctionArguments"/> class containing
    /// the specified <paramref name="arguments"/>.
    /// </summary>
    /// <param name="arguments">The arguments represented by this instance.</param>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to be used.</param>
    /// <remarks>
    /// The <paramref name="arguments"/> reference will be stored if the instance is already a
    /// <see cref="Dictionary{TKey, TValue}"/> with the same <see cref="IEqualityComparer{T}"/> or if
    /// <paramref name="arguments"/> is <see langword="null" /> in which case all dictionary operations
    /// on this instance will be routed directly to that instance otherwise a shallow clone of the provided <paramref name="arguments"/>.
    /// A <see langword="null"/> <paramref name="arguments"/> is will be treated as an empty parameters dictionary.
    /// </remarks>
    public AIFunctionArguments(IDictionary<string, object?>? arguments, IEqualityComparer<string>? comparer)
    {
#pragma warning disable S1698 // Consider using 'Equals' if value comparison is intended.
        _arguments =
            arguments is null
                ? new Dictionary<string, object?>(comparer)
                : (arguments is Dictionary<string, object?> dc) && (comparer is null || dc.Comparer == comparer)
                    ? dc
                    : new Dictionary<string, object?>(arguments, comparer);
#pragma warning restore S1698 // Consider using 'Equals' if value comparison is intended.
    }

    /// <summary>Gets or sets services optionally associated with these arguments.</summary>
    public IServiceProvider? Services { get; set; }

    /// <summary>Gets or sets additional context associated with these arguments.</summary>
    /// <remarks>
    /// The context is a dictionary of name/value pairs that can be used to store arbitrary
    /// information for use by an <see cref="AIFunction"/> implementation. The meaning of this
    /// data is left up to the implementer of the <see cref="AIFunction"/>.
    /// </remarks>
    public IDictionary<object, object?>? Context { get; set; }

    /// <inheritdoc />
    public object? this[string key]
    {
        get => _arguments[key];
        set => _arguments[key] = value;
    }

    /// <inheritdoc />
    public ICollection<string> Keys => _arguments.Keys;

    /// <inheritdoc />
    public ICollection<object?> Values => _arguments.Values;

    /// <inheritdoc />
    public int Count => _arguments.Count;

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => false;

    /// <inheritdoc />
    IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys => Keys;

    /// <inheritdoc />
    IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values => Values;

    /// <inheritdoc />
    public void Add(string key, object? value) => _arguments.Add(key, value);

    /// <inheritdoc />
    void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item) =>
        ((ICollection<KeyValuePair<string, object?>>)_arguments).Add(item);

    /// <inheritdoc />
    public void Clear() => _arguments.Clear();

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item) =>
        ((ICollection<KeyValuePair<string, object?>>)_arguments).Contains(item);

    /// <inheritdoc />
    public bool ContainsKey(string key) => _arguments.ContainsKey(key);

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<string, object?>>)_arguments).CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _arguments.GetEnumerator();

    /// <inheritdoc />
    public bool Remove(string key) => _arguments.Remove(key);

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item) =>
        ((ICollection<KeyValuePair<string, object?>>)_arguments).Remove(item);

    /// <inheritdoc />
    public bool TryGetValue(string key, out object? value) => _arguments.TryGetValue(key, out value);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
