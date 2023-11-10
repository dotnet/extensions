// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Forked from StyleCop.Analyzers repo.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.Extensions.LocalAnalyzers.Json;

/// <summary>
/// A wrapper object that contains a valid JSON value.
/// </summary>
[DebuggerDisplay("{ToString(),nq}", Type = "JsonValue({Type})")]
[DebuggerTypeProxy(typeof(JsonValueDebugView))]
[SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality",
    Justification = "Would require unnecessary refactor.")]
internal readonly struct JsonValue : IEquatable<JsonValue>
{
    /// <summary>
    /// Represents a <see langword="null" /> JsonValue.
    /// </summary>
    public static readonly JsonValue Null = new(JsonValueType.Null, default, null);
    private readonly object? _reference;
    private readonly double _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonValue"/> struct, representing a Boolean value.
    /// </summary>
    /// <param name="value">The value to be wrapped.</param>
    public JsonValue(bool? value)
    {
        if (!value.HasValue)
        {
            this = Null;
            return;
        }

        Type = JsonValueType.Boolean;
        _reference = null;
        _value = value.Value ? 1 : 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonValue"/> struct, representing a Number value.
    /// </summary>
    /// <param name="value">The value to be wrapped.</param>
    public JsonValue(double? value)
    {
        if (!value.HasValue)
        {
            this = Null;
            return;
        }

        Type = JsonValueType.Number;
        _reference = null;
        _value = value.Value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonValue"/> struct, representing a String value.
    /// </summary>
    /// <param name="value">The value to be wrapped.</param>
    public JsonValue(string? value)
    {
        if (value is null)
        {
            this = Null;
            return;
        }

        Type = JsonValueType.String;
        _value = default;
        _reference = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonValue"/> struct, representing a JsonObject.
    /// </summary>
    /// <param name="value">The value to be wrapped.</param>
    public JsonValue(JsonObject? value)
    {
        if (value is null)
        {
            this = Null;
            return;
        }

        Type = JsonValueType.Object;
        _value = default;
        _reference = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonValue"/> struct, representing a Array reference value.
    /// </summary>
    /// <param name="value">The value to be wrapped.</param>
    public JsonValue(JsonArray? value)
    {
        if (value is null)
        {
            this = Null;
            return;
        }

        Type = JsonValueType.Array;
        _value = default;
        _reference = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonValue"/> struct.
    /// </summary>
    /// <param name="type">The Json type of the JsonValue.</param>
    /// <param name="value">
    /// The internal value of the JsonValue.
    /// This is used when the Json type is Number or Boolean.
    /// </param>
    /// <param name="reference">
    /// The internal value reference of the JsonValue.
    /// This value is used when the Json type is String, JsonObject, or JsonArray.
    /// </param>
    private JsonValue(JsonValueType type, double value, object? reference)
    {
        Type = type;
        _value = value;
        _reference = reference;
    }

    /// <summary>
    /// Gets the type of this JsonValue.
    /// </summary>
    /// <value>The type of this JsonValue.</value>
    public JsonValueType Type { get; }

    /// <summary>
    /// Gets a value indicating whether this JsonValue is Null.
    /// </summary>
    /// <value>A value indicating whether this JsonValue is Null.</value>
    public bool IsNull => Type == JsonValueType.Null;

    /// <summary>
    /// Gets a value indicating whether this JsonValue is a Boolean.
    /// </summary>
    /// <value>A value indicating whether this JsonValue is a Boolean.</value>
    public bool IsBoolean => Type == JsonValueType.Boolean;

    /// <summary>
    /// Gets a value indicating whether this JsonValue is an Integer.
    /// </summary>
    /// <value>A value indicating whether this JsonValue is an Integer.</value>
#pragma warning disable S2589 // Boolean expressions should not be gratuitous
    public bool IsInteger => IsNumber && unchecked((int)_value) == _value;
#pragma warning restore S2589 // Boolean expressions should not be gratuitous

    /// <summary>
    /// Gets a value indicating whether this JsonValue is a Number.
    /// </summary>
    /// <value>A value indicating whether this JsonValue is a Number.</value>
    public bool IsNumber => Type == JsonValueType.Number;

    /// <summary>
    /// Gets a value indicating whether this JsonValue is a String.
    /// </summary>
    /// <value>A value indicating whether this JsonValue is a String.</value>
    public bool IsString => Type == JsonValueType.String;

    /// <summary>
    /// Gets a value indicating whether this JsonValue is a JsonObject.
    /// </summary>
    /// <value>A value indicating whether this JsonValue is a JsonObject.</value>
    public bool IsJsonObject => Type == JsonValueType.Object;

    /// <summary>
    /// Gets a value indicating whether this JsonValue is a JsonArray.
    /// </summary>
    /// <value>A value indicating whether this JsonValue is a JsonArray.</value>
    public bool IsJsonArray => Type == JsonValueType.Array;

    /// <summary>
    /// Gets a value indicating whether this JsonValue represents a DateTime.
    /// </summary>
    /// <value>A value indicating whether this JsonValue represents a DateTime.</value>
    public bool IsDateTime => AsDateTime != null;

    /// <summary>
    /// Gets a value indicating whether this value is true or false.
    /// </summary>
    /// <value>This value as a Boolean type.</value>
    public bool AsBoolean => Type switch
    {
        JsonValueType.Boolean => _value == 1,
        JsonValueType.Number => _value != 0,
        JsonValueType.String => !string.IsNullOrEmpty((string?)_reference),
        JsonValueType.Object or JsonValueType.Array => true,
        _ => false,
    };

    /// <summary>
    /// Gets this value as an Integer type.
    /// </summary>
    /// <value>This value as an Integer type.</value>
    public int AsInteger
    {
        get
        {
            var current = AsNumber;

            if (current >= int.MaxValue)
            {
                return int.MaxValue;
            }

            if (_value <= int.MinValue)
            {
                return int.MinValue;
            }

            return (int)_value;
        }
    }

    /// <summary>
    /// Gets this value as a Number type.
    /// </summary>
    /// <value>This value as a Number type.</value>
    public double AsNumber => Type switch
    {
        JsonValueType.Boolean => _value == 1 ? 1 : 0,
        JsonValueType.Number => _value,
        JsonValueType.String => double.TryParse((string?)_reference, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? number
                : 0,
        _ => 0
    };

    /// <summary>
    /// Gets this value as a String type.
    /// </summary>
    /// <value>This value as a String type.</value>
    public string? AsString => Type switch
    {
        JsonValueType.Boolean => (_value == 1)
                                ? "true"
                                : "false",
        JsonValueType.Number => _value.ToString(CultureInfo.InvariantCulture),
        JsonValueType.String => (string?)_reference,
        _ => null,
    };

    /// <summary>
    /// Gets this value as an JsonObject.
    /// </summary>
    /// <value>This value as an JsonObject.</value>
#pragma warning disable S1168 // Empty arrays and collections should be returned instead of null
    public JsonObject? AsJsonObject => IsJsonObject ? (JsonObject?)_reference : null;

    /// <summary>
    /// Gets this value as an JsonArray.
    /// </summary>
    /// <value>This value as an JsonArray.</value>
    public JsonArray? AsJsonArray => IsJsonArray ? (JsonArray?)_reference : null;
#pragma warning restore S1168 // Empty arrays and collections should be returned instead of null

    /// <summary>
    /// Gets this value as a system.DateTime.
    /// </summary>
    /// <value>This value as a system.DateTime.</value>
    public DateTime? AsDateTime => IsString && DateTime.TryParse((string?)_reference ?? string.Empty, out var value)
            ? value
            : null;

    /// <summary>
    /// Gets this (inner) value as a System.object.
    /// </summary>
    /// <value>This (inner) value as a System.object.</value>
    public object? AsObject => Type switch
    {
        JsonValueType.Boolean or JsonValueType.Number => _value,
        JsonValueType.String or JsonValueType.Object or JsonValueType.Array => _reference,
        _ => null
    };

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get or set.</param>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when this JsonValue is not a JsonObject.
    /// </exception>
    public JsonValue this[string key]
    {
        get
        {
            if (IsJsonObject)
            {
                return ((JsonObject)_reference!)[key];
            }
            else
            {
                throw new InvalidOperationException("This value does not represent a JsonObject.");
            }
        }

        set
        {
            if (IsJsonObject)
            {
                ((JsonObject)_reference!)[key] = value;
            }
            else
            {
                throw new InvalidOperationException("This value does not represent a JsonObject.");
            }
        }
    }

    /// <summary>
    /// Gets or sets the value at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the value to get or set.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this <see cref="JsonValue"/> is not a <see cref="JsonArray"/>.
    /// </exception>
    public JsonValue this[int index]
    {
        get
        {
            if (IsJsonArray)
            {
                return ((JsonArray)_reference!)[index];
            }
            else
            {
                throw new InvalidOperationException("This value does not represent a JsonArray.");
            }
        }

        set
        {
            if (IsJsonArray)
            {
                ((JsonArray)_reference!)[index] = value;
            }
            else
            {
                throw new InvalidOperationException("This value does not represent a JsonArray.");
            }
        }
    }

    /// <summary>
    /// Converts the given nullable boolean into a JsonValue.
    /// </summary>
    /// <param name="value">The value to be converted.</param>
    public static implicit operator JsonValue(bool? value)
    {
        return new JsonValue(value);
    }

    /// <summary>
    /// Converts the given nullable double into a JsonValue.
    /// </summary>
    /// <param name="value">The value to be converted.</param>
    public static implicit operator JsonValue(double? value)
    {
        return new JsonValue(value);
    }

    /// <summary>
    /// Converts the given string into a JsonValue.
    /// </summary>
    /// <param name="value">The value to be converted.</param>
    public static implicit operator JsonValue(string value)
    {
        return new JsonValue(value);
    }

    /// <summary>
    /// Converts the given JsonObject into a JsonValue.
    /// </summary>
    /// <param name="value">The value to be converted.</param>
    public static implicit operator JsonValue(JsonObject value)
    {
        return new JsonValue(value);
    }

    /// <summary>
    /// Converts the given JsonArray into a JsonValue.
    /// </summary>
    /// <param name="value">The value to be converted.</param>
    public static implicit operator JsonValue(JsonArray value)
    {
        return new JsonValue(value);
    }

    /// <summary>
    /// Converts the given DateTime? into a JsonValue.
    /// </summary>
    /// <remarks>
    /// <para>The DateTime value will be stored as a string using ISO 8601 format,
    /// since JSON does not define a DateTime type.</para>
    /// </remarks>
    /// <param name="value">The value to be converted.</param>
    public static implicit operator JsonValue(DateTime? value)
    {
        return value == null
          ? Null
          : new JsonValue(value.Value.ToString("o", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Converts the given JsonValue into an Int.
    /// </summary>
    /// <param name="jsonValue">The JsonValue to be converted.</param>
    public static explicit operator int(JsonValue jsonValue)
    {
        return jsonValue.IsInteger ? jsonValue.AsInteger : 0;
    }

    /// <summary>
    /// Converts the given JsonValue into a nullable Int.
    /// </summary>
    /// <param name="jsonValue">The JsonValue to be converted.</param>
    /// <exception cref="System.InvalidCastException">
    /// Throws System.InvalidCastException when the inner value type of the
    /// JsonValue is not the desired type of the conversion.
    /// </exception>
    public static explicit operator int?(JsonValue jsonValue)
    {
        return jsonValue.IsNull ? null : (int)jsonValue;
    }

    /// <summary>
    /// Converts the given JsonValue into a Bool.
    /// </summary>
    /// <param name="jsonValue">The JsonValue to be converted.</param>
    public static explicit operator bool(JsonValue jsonValue)
    {
        return jsonValue.IsBoolean && jsonValue._value == 1;
    }

    /// <summary>
    /// Converts the given JsonValue into a nullable Bool.
    /// </summary>
    /// <param name="jsonValue">The JsonValue to be converted.</param>
    /// <exception cref="System.InvalidCastException">
    /// Throws System.InvalidCastException when the inner value type of the
    /// JsonValue is not the desired type of the conversion.
    /// </exception>
    public static explicit operator bool?(JsonValue jsonValue)
    {
        return jsonValue.IsNull ? null : (bool)jsonValue;
    }

    /// <summary>
    /// Converts the given JsonValue into a Double.
    /// </summary>
    /// <param name="jsonValue">The JsonValue to be converted.</param>
    public static explicit operator double(JsonValue jsonValue)
    {
        return jsonValue.IsNumber ? jsonValue._value : double.NaN;
    }

    /// <summary>
    /// Converts the given JsonValue into a nullable Double.
    /// </summary>
    /// <param name="jsonValue">The JsonValue to be converted.</param>
    /// <exception cref="System.InvalidCastException">
    /// Throws System.InvalidCastException when the inner value type of the
    /// JsonValue is not the desired type of the conversion.
    /// </exception>
    public static explicit operator double?(JsonValue jsonValue)
    {
        return jsonValue.IsNull
            ? null
            : (double)jsonValue;
    }

    /// <summary>
    /// Converts the given JsonValue into a String.
    /// </summary>
    /// <param name="jsonValue">The JsonValue to be converted.</param>
    public static explicit operator string?(JsonValue jsonValue)
    {
        return jsonValue.IsString || jsonValue.IsNull
            ? jsonValue._reference as string
            : null;
    }

    /// <summary>
    /// Converts the given JsonValue into a JsonObject.
    /// </summary>
    /// <param name="jsonValue">The JsonValue to be converted.</param>
    public static explicit operator JsonObject?(JsonValue jsonValue)
    {
        return jsonValue.IsJsonObject || jsonValue.IsNull ? jsonValue._reference as JsonObject : null;
    }

    /// <summary>
    /// Converts the given JsonValue into a JsonArray.
    /// </summary>
    /// <param name="jsonValue">The JsonValue to be converted.</param>
    public static explicit operator JsonArray?(JsonValue jsonValue)
    {
        return jsonValue.IsJsonArray || jsonValue.IsNull ? jsonValue._reference as JsonArray : null;
    }

    /// <summary>
    /// Converts the given JsonValue into a DateTime.
    /// </summary>
    /// <param name="jsonValue">The JsonValue to be converted.</param>
    public static explicit operator DateTime(JsonValue jsonValue)
    {
        return jsonValue.AsDateTime ?? DateTime.MinValue;
    }

    /// <summary>
    /// Converts the given JsonValue into a nullable DateTime.
    /// </summary>
    /// <param name="jsonValue">The JsonValue to be converted.</param>
    public static explicit operator DateTime?(JsonValue jsonValue)
    {
        return jsonValue.IsDateTime || jsonValue.IsNull
            ? jsonValue.AsDateTime
            : null;
    }

    /// <summary>
    /// Returns a value indicating whether the two given JsonValues are equal.
    /// </summary>
    /// <param name="a">First JsonValue to compare.</param>
    /// <param name="b">Second JsonValue to compare.</param>
    public static bool operator ==(JsonValue a, JsonValue b)
    {
        return (a.Type == b.Type)
            && (a._value == b._value)
            && Equals(a._reference, b._reference);
    }

    /// <summary>
    /// Returns a value indicating whether the two given JsonValues are unequal.
    /// </summary>
    /// <param name="a">First JsonValue to compare.</param>
    /// <param name="b">Second JsonValue to compare.</param>
    public static bool operator !=(JsonValue a, JsonValue b)
    {
        return !(a == b);
    }

    /// <summary>
    /// Returns a JsonValue by parsing the given string.
    /// </summary>
    /// <param name="text">The JSON-formatted string to be parsed.</param>
    /// <returns>The <see cref="JsonValue"/> representing the parsed text.</returns>
    public static JsonValue Parse(string text)
    {
        return JsonReader.Parse(text);
    }

    public bool Equals(JsonValue other)
    {
        return other == this;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is JsonValue jv)
        {
            return this == jv;
        }

        return IsNull && obj is null;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var r = _reference != null ? EqualityComparer<object>.Default.GetHashCode(_reference) : 1;

        return IsNull
            ? Type.GetHashCode()
            : Type.GetHashCode()
                ^ _value.GetHashCode()
                ^ r;
    }

    [ExcludeFromCodeCoverage]
    private sealed class JsonValueDebugView
    {
        private readonly JsonValue _jsonValue;

        [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Used by debugger.")]
        public JsonValueDebugView(JsonValue jsonValue)
        {
            _jsonValue = jsonValue;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
#pragma warning disable S1168 // Empty arrays and collections should be returned instead of null
        public JsonObject? ObjectView => _jsonValue.IsJsonObject
                    ? (JsonObject?)_jsonValue._reference
                    : null;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public JsonArray? ArrayView => _jsonValue.IsJsonArray
                    ? (JsonArray?)_jsonValue._reference
                    : null;
#pragma warning restore S1168 // Empty arrays and collections should be returned instead of null

        public JsonValueType Type => _jsonValue.Type;

        public object? Value => _jsonValue.IsJsonObject || _jsonValue.IsJsonArray
                    ? _jsonValue._reference
                    : null;
    }
}
