// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Forked from StyleCop.Analyzers repo.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.LocalAnalyzers.Json;

/// <summary>
/// Represents an ordered collection of JsonValues.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(JsonArrayDebugView))]
internal sealed class JsonArray : IEnumerable<JsonValue>
{
    private readonly List<JsonValue> _items = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonArray"/> class, adding the given values to the collection.
    /// </summary>
    /// <param name="values">The values to be added to this collection.</param>
    public JsonArray(params JsonValue[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        _items.AddRange(values);
    }

    /// <summary>
    /// Gets the number of values in this collection.
    /// </summary>
    /// <value>The number of values in this collection.</value>
    public int Count => _items.Count;

    /// <summary>
    /// Gets or sets the value at the given index.
    /// </summary>
    /// <param name="index">The zero-based index of the value to get or set.</param>
    /// <remarks>
    /// <para>The getter will return JsonValue.Null if the given index is out of range.</para>
    /// </remarks>
    public JsonValue this[int index]
    {
        get => index >= 0 && index < _items.Count
            ? _items[index]
            : JsonValue.Null;
        set => _items[index] = value;
    }

    /// <summary>
    /// Adds the given value to this collection.
    /// </summary>
    /// <param name="value">The value to be added.</param>
    /// <returns>Returns this collection.</returns>
    public JsonArray Add(JsonValue value)
    {
        _items.Add(value);
        return this;
    }

    /// <summary>
    /// Inserts the given value at the given index in this collection.
    /// </summary>
    /// <param name="index">The index where the given value will be inserted.</param>
    /// <param name="value">The value to be inserted into this collection.</param>
    /// <returns>Returns this collection.</returns>
    public JsonArray Insert(int index, JsonValue value)
    {
        _items.Insert(index, value);
        return this;
    }

    /// <summary>
    /// Removes the value at the given index.
    /// </summary>
    /// <param name="index">The index of the value to be removed.</param>
    /// <returns>Return this collection.</returns>
    public JsonArray Remove(int index)
    {
        _items.RemoveAt(index);
        return this;
    }

    /// <summary>
    /// Clears the contents of this collection.
    /// </summary>
    /// <returns>Returns this collection.</returns>
    public JsonArray Clear()
    {
        _items.Clear();
        return this;
    }

    /// <summary>
    /// Determines whether the given item is in the JsonArray.
    /// </summary>
    /// <param name="item">The item to locate in the JsonArray.</param>
    /// <returns>Returns true if the item is found; otherwise, false.</returns>
    public bool Contains(JsonValue item) => _items.Contains(item);

    /// <summary>
    /// Determines the index of the given item in this JsonArray.
    /// </summary>
    /// <param name="item">The item to locate in this JsonArray.</param>
    /// <returns>The index of the item, if found. Otherwise, returns -1.</returns>
    public int IndexOf(JsonValue item) => _items.IndexOf(item);

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>The enumerator that iterates through the collection.</returns>
    public IEnumerator<JsonValue> GetEnumerator() => _items.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>The enumerator that iterates through the collection.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    [ExcludeFromCodeCoverage]
    private sealed class JsonArrayDebugView
    {
        [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Used by debugger.")]
        public JsonArrayDebugView(JsonArray array)
        {
            Items = array._items.ToArray();
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public JsonValue[] Items { get; }
    }
}
