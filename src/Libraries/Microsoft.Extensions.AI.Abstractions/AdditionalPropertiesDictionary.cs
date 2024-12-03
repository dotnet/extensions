// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2365 // Properties should not make collection or array copies
#pragma warning disable S3604 // Member initializer values should not be redundant

namespace Microsoft.Extensions.AI;

/// <summary>Provides a dictionary used as the AdditionalProperties dictionary on Microsoft.Extensions.AI objects.</summary>
[DebuggerTypeProxy(typeof(DebugView))]
[DebuggerDisplay("Count = {Count}")]
public sealed class AdditionalPropertiesDictionary : IDictionary<string, object?>, IReadOnlyDictionary<string, object?>
{
    /// <summary>The underlying dictionary.</summary>
    private readonly Dictionary<string, object?> _dictionary;

    /// <summary>Initializes a new instance of the <see cref="AdditionalPropertiesDictionary"/> class.</summary>
    public AdditionalPropertiesDictionary()
    {
        _dictionary = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Initializes a new instance of the <see cref="AdditionalPropertiesDictionary"/> class.</summary>
    public AdditionalPropertiesDictionary(IDictionary<string, object?> dictionary)
    {
        _dictionary = new(dictionary, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Initializes a new instance of the <see cref="AdditionalPropertiesDictionary"/> class.</summary>
    public AdditionalPropertiesDictionary(IEnumerable<KeyValuePair<string, object?>> collection)
    {
#if NET
        _dictionary = new(collection, StringComparer.OrdinalIgnoreCase);
#else
        _dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in collection)
        {
            _dictionary.Add(item.Key, item.Value);
        }
#endif
    }

    /// <summary>Creates a shallow clone of the properties dictionary.</summary>
    /// <returns>
    /// A shallow clone of the properties dictionary. The instance will not be the same as the current instance,
    /// but it will contain all of the same key-value pairs.
    /// </returns>
    public AdditionalPropertiesDictionary Clone() => new(_dictionary);

    /// <inheritdoc />
    public object? this[string key]
    {
        get => _dictionary[key];
        set => _dictionary[key] = value;
    }

    /// <inheritdoc />
    public ICollection<string> Keys => _dictionary.Keys;

    /// <inheritdoc />
    public ICollection<object?> Values => _dictionary.Values;

    /// <inheritdoc />
    public int Count => _dictionary.Count;

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => false;

    /// <inheritdoc />
    IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys => _dictionary.Keys;

    /// <inheritdoc />
    IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values => _dictionary.Values;

    /// <inheritdoc />
    public void Add(string key, object? value) => _dictionary.Add(key, value);

    /// <summary>Attempts to add the specified key and value to the dictionary.</summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <returns><see langword="true"/> if the key/value pair was added to the dictionary successfully; otherwise, <see langword="false"/>.</returns>
    public bool TryAdd(string key, object? value)
    {
#if NET
        return _dictionary.TryAdd(key, value);
#else
        if (!_dictionary.ContainsKey(key))
        {
            _dictionary.Add(key, value);
            return true;
        }

        return false;
#endif
    }

    /// <inheritdoc />
    void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item) => ((ICollection<KeyValuePair<string, object?>>)_dictionary).Add(item);

    /// <inheritdoc />
    public void Clear() => _dictionary.Clear();

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item) =>
        ((ICollection<KeyValuePair<string, object?>>)_dictionary).Contains(item);

    /// <inheritdoc />
    public bool ContainsKey(string key) => _dictionary.ContainsKey(key);

    /// <inheritdoc />
    void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<string, object?>>)_dictionary).CopyTo(array, arrayIndex);

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="AdditionalPropertiesDictionary"/>.
    /// </summary>
    /// <returns>An <see cref="AdditionalPropertiesDictionary.Enumerator"/> that enumerates the contents of the <see cref="AdditionalPropertiesDictionary"/>.</returns>
    public Enumerator GetEnumerator() => new(_dictionary.GetEnumerator());

    /// <inheritdoc />
    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public bool Remove(string key) => _dictionary.Remove(key);

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item) => ((ICollection<KeyValuePair<string, object?>>)_dictionary).Remove(item);

    /// <inheritdoc />
    public bool TryGetValue(string key, out object? value) => _dictionary.TryGetValue(key, out value);

    /// <summary>Attempts to extract a typed value from the dictionary.</summary>
    /// <typeparam name="T">Specifies the type of the value to be retrieved.</typeparam>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">
    /// The value retrieved from the dictionary, if found and successfully converted to the requested type;
    /// otherwise, the default value of <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a non-<see langword="null"/> value was found for <paramref name="key"/>
    /// in the dictionary and converted to the requested type; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// If a non-<see langword="null"/> value is found for the key in the dictionary, but the value is not of the requested type and is
    /// an <see cref="IConvertible"/> object, the method attempts to convert the object to the requested type.
    /// </remarks>
    public bool TryGetValue<T>(string key, [NotNullWhen(true)] out T? value)
    {
        if (TryGetValue(key, out object? obj))
        {
            switch (obj)
            {
                case T t:
                    // The object is already of the requested type. Return it.
                    value = t;
                    return true;

                case IConvertible:
                    // The object is convertible; try to convert it to the requested type. Unfortunately, there's no
                    // convenient way to do this that avoids exceptions and that doesn't involve a ton of boilerplate,
                    // so we only try when the source object is at least an IConvertible, which is what ChangeType uses.
                    try
                    {
                        value = (T)Convert.ChangeType(obj, typeof(T), CultureInfo.InvariantCulture);
                        return true;
                    }
                    catch (Exception e) when (e is ArgumentException or FormatException or InvalidCastException or OverflowException)
                    {
                        // Ignore known failure modes.
                    }

                    break;
            }
        }

        // Unable to find the value or convert it to the requested type.
        value = default;
        return false;
    }

    /// <summary>Enumerates the elements of an <see cref="AdditionalPropertiesDictionary"/>.</summary>
    public struct Enumerator : IEnumerator<KeyValuePair<string, object?>>
    {
        /// <summary>The wrapped dictionary enumerator.</summary>
        private Dictionary<string, object?>.Enumerator _dictionaryEnumerator;

        /// <summary>Initializes a new instance of the <see cref="Enumerator"/> struct with the dictionary enumerator to wrap.</summary>
        /// <param name="dictionaryEnumerator">The dictionary enumerator to wrap.</param>
        internal Enumerator(Dictionary<string, object?>.Enumerator dictionaryEnumerator)
        {
            _dictionaryEnumerator = dictionaryEnumerator;
        }

        /// <inheritdoc />
        public KeyValuePair<string, object?> Current => _dictionaryEnumerator.Current;

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public void Dispose() => _dictionaryEnumerator.Dispose();

        /// <inheritdoc />
        public bool MoveNext() => _dictionaryEnumerator.MoveNext();

        /// <inheritdoc />
        public void Reset() => Reset(ref _dictionaryEnumerator);

        /// <summary>Calls <see cref="IEnumerator.Reset"/> on an enumerator.</summary>
        private static void Reset<TEnumerator>(ref TEnumerator enumerator)
            where TEnumerator : struct, IEnumerator
        {
            enumerator.Reset();
        }
    }

    /// <summary>Provides a debugger view for the collection.</summary>
    private sealed class DebugView(AdditionalPropertiesDictionary properties)
    {
        private readonly AdditionalPropertiesDictionary _properties = Throw.IfNull(properties);

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public AdditionalProperty[] Items => (from p in _properties select new AdditionalProperty(p.Key, p.Value)).ToArray();

        [DebuggerDisplay("{Value}", Name = "[{Key}]")]
        public readonly struct AdditionalProperty(string key, object? value)
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
            public string Key { get; } = key;

            [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
            public object? Value { get; } = value;
        }
    }
}
