// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.AI;

/// <summary>Provides a dictionary used as the AdditionalProperties dictionary on Microsoft.Extensions.AI objects.</summary>
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
    public AdditionalPropertiesDictionary Clone() => new AdditionalPropertiesDictionary(_dictionary);

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

    /// <inheritdoc />
    void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item) => ((ICollection<KeyValuePair<string, object?>>)_dictionary).Add(item);

    /// <inheritdoc />
    public void Clear() => _dictionary.Clear();

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item) => _dictionary.Contains(item);

    /// <inheritdoc />
    public bool ContainsKey(string key) => _dictionary.ContainsKey(key);

    /// <inheritdoc />
    void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<string, object?>>)_dictionary).CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _dictionary.GetEnumerator();

    /// <inheritdoc />
    public bool Remove(string key) => _dictionary.Remove(key);

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item) => ((ICollection<KeyValuePair<string, object?>>)_dictionary).Remove(item);

    /// <inheritdoc />
    public bool TryGetValue(string key, out object? value) => _dictionary.TryGetValue(key, out value);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();
}
