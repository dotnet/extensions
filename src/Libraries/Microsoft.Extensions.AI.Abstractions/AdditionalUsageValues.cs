// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// A key-value store for usage values. Values may be of the following types:
/// <see cref="int"/>, <see cref="long"/>, <see cref="float"/>, <see cref="double"/>, <see cref="decimal"/>.
/// </summary>
public class AdditionalUsageValues : IEnumerable<KeyValuePair<string, object>>
{
    private readonly Dictionary<string, Entry> _dictionary = new();

    /// <summary>
    /// Gets a value matching the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The corresponding dictionary entry.</returns>
    public object? this[string key] => _dictionary[key].AsObject();

    /// <summary>Gets the keys in the collection.</summary>
    public IEnumerable<string> Keys => _dictionary.Keys;

    /// <summary>Gets the number of items in the collection.</summary>
    public int Count => _dictionary.Count;

    /// <summary>
    /// Returns an enumerator that enumerates through the collection.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        foreach (var entry in _dictionary)
        {
            yield return new KeyValuePair<string, object>(entry.Key, entry.Value.AsObject());
        }
    }

    /// <summary>
    /// Returns an enumerator that enumerates through the collection.
    /// </summary>
    /// <returns>The enumerator.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        foreach (var entry in _dictionary)
        {
            yield return new KeyValuePair<string, object>(entry.Key, entry.Value.AsObject());
        }
    }

    /// <summary>Removes the specified entry.</summary>
    /// <param name="key">The key.</param>
    public void Remove(string key)
        => _dictionary.Remove(key);

    /// <summary>Gets the specified value as a <see cref="int"/>.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The value.</returns>
    public int GetInt32(string key) => _dictionary[key].AsInt();

    /// <summary>Gets the specified value as a <see cref="long"/>.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The value.</returns>
    public long GetInt64(string key) => _dictionary[key].AsLong();

    /// <summary>Gets the specified value as a <see cref="float"/>.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The value.</returns>
    public float GetSingle(string key) => _dictionary[key].AsFloat();

    /// <summary>Gets the specified value as a <see cref="double"/>.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The value.</returns>
    public double GetDouble(string key) => _dictionary[key].AsDouble();

    /// <summary>Gets the specified value as a <see cref="decimal"/>.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The value.</returns>
    public decimal GetDecimal(string key) => _dictionary[key].AsDecimal();

    /// <summary>Adds or overwrites an entry with the specified <paramref name="key"/>.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void Set(string key, int value) => _dictionary[key] = new Entry(value);

    /// <summary>Adds or overwrites an entry with the specified <paramref name="key"/>.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void Set(string key, long value) => _dictionary[key] = new Entry(value);

    /// <summary>Adds or overwrites an entry with the specified <paramref name="key"/>.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void Set(string key, float value) => _dictionary[key] = new Entry(value);

    /// <summary>Adds or overwrites an entry with the specified <paramref name="key"/>.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void Set(string key, double value) => _dictionary[key] = new Entry(value);

    /// <summary>Adds or overwrites an entry with the specified <paramref name="key"/>.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void Set(string key, decimal value) => _dictionary[key] = new Entry(value);

    /// <summary>Increases an existing entry stored with the specified <paramref name="key"/>, or creates a new entry.</summary>
    /// <param name="key">The key.</param>
    /// <param name="amountToAdd">The amount by which to increment the entry. If no stored value already exists, a new one is created with this value.</param>
    public void Add(string key, int amountToAdd) =>
        _dictionary[key] = _dictionary.TryGetValue(key, out var existingValue) ? existingValue.Add(amountToAdd) : new(amountToAdd);

    /// <summary>Increases an existing entry stored with the specified <paramref name="key"/>, or creates a new entry.</summary>
    /// <param name="key">The key.</param>
    /// <param name="amountToAdd">The amount by which to increment the entry. If no stored value already exists, a new one is created with this value.</param>
    public void Add(string key, long amountToAdd) =>
        _dictionary[key] = _dictionary.TryGetValue(key, out var existingValue) ? existingValue.Add(amountToAdd) : new(amountToAdd);

    /// <summary>Increases an existing entry stored with the specified <paramref name="key"/>, or creates a new entry.</summary>
    /// <param name="key">The key.</param>
    /// <param name="amountToAdd">The amount by which to increment the entry. If no stored value already exists, a new one is created with this value.</param>
    public void Add(string key, float amountToAdd) =>
        _dictionary[key] = _dictionary.TryGetValue(key, out var existingValue) ? existingValue.Add(amountToAdd) : new(amountToAdd);

    /// <summary>Increases an existing entry stored with the specified <paramref name="key"/>, or creates a new entry.</summary>
    /// <param name="key">The key.</param>
    /// <param name="amountToAdd">The amount by which to increment the entry. If no stored value already exists, a new one is created with this value.</param>
    public void Add(string key, double amountToAdd) =>
        _dictionary[key] = _dictionary.TryGetValue(key, out var existingValue) ? existingValue.Add(amountToAdd) : new(amountToAdd);

    /// <summary>Increases an existing entry stored with the specified <paramref name="key"/>, or creates a new entry.</summary>
    /// <param name="key">The key.</param>
    /// <param name="amountToAdd">The amount by which to increment the entry. If no stored value already exists, a new one is created with this value.</param>
    public void Add(string key, decimal amountToAdd) =>
        _dictionary[key] = _dictionary.TryGetValue(key, out var existingValue) ? existingValue.Add(amountToAdd) : new(amountToAdd);

    /// <summary>Adds all the entries from <paramref name="source"/> to this instance.</summary>
    /// <param name="source">The values to add.</param>
    public void AddFrom(AdditionalUsageValues source)
    {
        foreach (var keyValuePair in Throw.IfNull(source)._dictionary)
        {
            switch (keyValuePair.Value.Type)
            {
                case EntryType.Int:
                    Add(keyValuePair.Key, keyValuePair.Value.IntValue);
                    break;
                case EntryType.Long:
                    Add(keyValuePair.Key, keyValuePair.Value.LongValue);
                    break;
                case EntryType.Float:
                    Add(keyValuePair.Key, keyValuePair.Value.FloatValue);
                    break;
                case EntryType.Double:
                    Add(keyValuePair.Key, keyValuePair.Value.DoubleValue);
                    break;
                case EntryType.Decimal:
                    Add(keyValuePair.Key, keyValuePair.Value.DecimalValue);
                    break;
                default:
                    throw new InvalidOperationException("Unknown entry type.");
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private readonly struct Entry
    {
        [FieldOffset(0)]
        public readonly EntryType Type;

        [FieldOffset(4)]
        public readonly int IntValue;

        [FieldOffset(4)]
        public readonly long LongValue;

        [FieldOffset(4)]
        public readonly float FloatValue;

        [FieldOffset(4)]
        public readonly double DoubleValue;

        [FieldOffset(4)]
        public readonly decimal DecimalValue;

        public Entry(int value)
        {
            Type = EntryType.Int;
            IntValue = value;
        }

        public Entry(long value)
        {
            Type = EntryType.Long;
            LongValue = value;
        }

        public Entry(float value)
        {
            Type = EntryType.Float;
            FloatValue = value;
        }

        public Entry(double value)
        {
            Type = EntryType.Double;
            DoubleValue = value;
        }

        public Entry(decimal value)
        {
            Type = EntryType.Decimal;
            DecimalValue = value;
        }

        public object AsObject() => Type switch
        {
            EntryType.Int => IntValue,
            EntryType.Long => LongValue,
            EntryType.Float => FloatValue,
            EntryType.Double => DoubleValue,
            EntryType.Decimal => DecimalValue,
            _ => throw new InvalidOperationException("Unknown entry type.")
        };

        // In the common case we'll be reading the correct type. In the uncommon case we'll do a boxed conversion to keep things simple,
        // otherwise there are 25 conversion cases to write out.
        public int AsInt() => Type == EntryType.Int ? IntValue : Convert.ToInt32(AsObject(), CultureInfo.InvariantCulture);
        public long AsLong() => Type == EntryType.Long ? LongValue : Convert.ToInt64(AsObject(), CultureInfo.InvariantCulture);
        public float AsFloat() => Type == EntryType.Float ? FloatValue : Convert.ToSingle(AsObject(), CultureInfo.InvariantCulture);
        public double AsDouble() => Type == EntryType.Double ? DoubleValue : Convert.ToDouble(AsObject(), CultureInfo.InvariantCulture);
        public decimal AsDecimal() => Type == EntryType.Decimal ? DecimalValue : Convert.ToDecimal(AsObject(), CultureInfo.InvariantCulture);

        public Entry Add(int value) => new Entry(AsInt() + value);
        public Entry Add(long value) => new Entry(AsLong() + value);
        public Entry Add(float value) => new Entry(AsFloat() + value);
        public Entry Add(double value) => new Entry(AsDouble() + value);
        public Entry Add(decimal value) => new Entry(AsDecimal() + value);
    }

    private enum EntryType
    {
        Int,
        Long,
        Float,
        Double,
        Decimal
    }
}
