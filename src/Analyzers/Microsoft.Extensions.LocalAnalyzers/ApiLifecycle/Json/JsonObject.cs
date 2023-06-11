// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Forked from StyleCop.Analyzers repo.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.LocalAnalyzers.Json;

/// <summary>
/// Represents a key-value pair collection of JsonValue objects.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(JsonObjectDebugView))]
internal sealed class JsonObject : IEnumerable<KeyValuePair<string, JsonValue>>, IEnumerable<JsonValue>
{
    private readonly IDictionary<string, JsonValue> _properties;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonObject"/> class.
    /// </summary>
    public JsonObject()
    {
        _properties = new Dictionary<string, JsonValue>();
    }

    /// <summary>
    /// Gets the number of properties in this JsonObject.
    /// </summary>
    /// <value>The number of properties in this JsonObject.</value>
    public int Count => _properties.Count;

    /// <summary>
    /// Gets or sets the property with the given key.
    /// </summary>
    /// <param name="key">The key of the property to get or set.</param>
    /// <remarks>
    /// <para>The getter will return JsonValue.Null if the given key is not associated with any value.</para>
    /// </remarks>
    public JsonValue this[string key]
    {
        get => _properties.TryGetValue(key, out var value)
            ? value
            : JsonValue.Null;
        set => _properties[key] = value;
    }

    /// <summary>
    /// Adds a key with a <see langword="null" /> value to this collection.
    /// </summary>
    /// <param name="key">The key of the property to be added.</param>
    /// <remarks><para>Returns this JsonObject.</para></remarks>
    /// <returns>The <see cref="JsonObject"/> that was added.</returns>
    public JsonObject Add(string key) => Add(key, JsonValue.Null);

    /// <summary>
    /// Adds a value associated with a key to this collection.
    /// </summary>
    /// <param name="key">The key of the property to be added.</param>
    /// <param name="value">The value of the property to be added.</param>
    /// <returns>Returns this JsonObject.</returns>
    public JsonObject Add(string key, JsonValue value)
    {
        _properties.Add(key, value);
        return this;
    }

    /// <summary>
    /// Removes a property with the given key.
    /// </summary>
    /// <param name="key">The key of the property to be removed.</param>
    /// <returns>
    /// Returns true if the given key is found and removed; otherwise, false.
    /// </returns>
    public bool Remove(string key) => _properties.Remove(key);

    /// <summary>
    /// Clears the contents of this collection.
    /// </summary>
    /// <returns>Returns this JsonObject.</returns>
    public JsonObject Clear()
    {
        _properties.Clear();
        return this;
    }

    /// <summary>
    /// Changes the key of one of the items in the collection.
    /// </summary>
    /// <remarks>
    /// <para>This method has no effects if the <i>oldKey</i> does not exists.
    /// If the <i>newKey</i> already exists, the value will be overwritten.</para>
    /// </remarks>
    /// <param name="oldKey">The name of the key to be changed.</param>
    /// <param name="newKey">The new name of the key.</param>
    /// <returns>Returns this JsonObject.</returns>
    public JsonObject Rename(string oldKey, string newKey)
    {
        if (oldKey == newKey)
        {
            return this;
        }

        if (_properties.TryGetValue(oldKey, out var value))
        {
            this[newKey] = value;
            _ = Remove(oldKey);
        }

        return this;
    }

    /// <summary>
    /// Determines whether this collection contains an item associated with the given key.
    /// </summary>
    /// <param name="key">The key to locate in this collection.</param>
    /// <returns>Returns true if the key is found; otherwise, false.</returns>
    public bool ContainsKey(string key) => _properties.ContainsKey(key);

    /// <summary>
    /// Determines whether this collection contains the given JsonValue.
    /// </summary>
    /// <param name="value">The value to locate in this collection.</param>
    /// <returns>Returns true if the value is found; otherwise, false.</returns>
    public bool Contains(JsonValue value) => _properties.Values.Contains(value);

    /// <summary>
    /// Returns an enumerator that iterates through this collection.
    /// </summary>
    /// <returns>The enumerator that iterates through this collection.</returns>
    public IEnumerator<KeyValuePair<string, JsonValue>> GetEnumerator() => _properties.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through this collection.
    /// </summary>
    /// <returns>The enumerator that iterates through this collection.</returns>
    IEnumerator<JsonValue> IEnumerable<JsonValue>.GetEnumerator() => _properties.Values.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through this collection.
    /// </summary>
    /// <returns>The enumerator that iterates through this collection.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    [ExcludeFromCodeCoverage]
    private sealed class JsonObjectDebugView
    {
        private readonly JsonObject _object;

        [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Used by debugger.")]
        public JsonObjectDebugView(JsonObject jsonObject)
        {
            _object = jsonObject;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair[] Keys
        {
            get
            {
                var keys = new KeyValuePair[_object.Count];

                var i = 0;
                foreach (var property in _object)
                {
                    keys[i] = new KeyValuePair(property.Key, property.Value);
                    i += 1;
                }

                return keys;
            }
        }

        [DebuggerDisplay("{value.ToString(),nq}", Name = "{key}", Type = "JsonValue({Type})")]
        public sealed class KeyValuePair
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
#pragma warning disable IDE0052 // Remove unread private members
            private readonly string _key;
#pragma warning restore IDE0052 // Remove unread private members

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly JsonValue _value;

            public KeyValuePair(string key, JsonValue value)
            {
                _key = key;
                _value = value;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object View
            {
                get
                {
                    if (_value.IsJsonObject)
                    {
                        return (JsonObject)_value!;
                    }
                    else if (_value.IsJsonArray)
                    {
                        return (JsonArray)_value!;
                    }

                    return _value;
                }
            }
        }
    }
}
