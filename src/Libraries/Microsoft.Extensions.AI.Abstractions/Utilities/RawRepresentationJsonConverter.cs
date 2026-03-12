// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides best-effort JSON serialization for <c>RawRepresentation</c> properties.
/// </summary>
/// <remarks>
/// <para>
/// When writing JSON, the converter attempts to serialize the runtime value using the active
/// <see cref="JsonSerializerOptions"/>. If serialization fails (for example, due to circular references
/// or missing type metadata), it writes an empty JSON object (<c>{}</c>) as a fallback.
/// </para>
/// <para>
/// When reading JSON, it always materializes the payload as a <see cref="JsonElement"/>.
/// </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RawRepresentationJsonConverter : JsonConverter<object?>
{
    /// <inheritdoc />
    public override bool HandleNull => false;

    /// <inheritdoc />
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null)
        {
            return null;
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.Clone();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        _ = Throw.IfNull(writer);

        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        if (value is JsonElement jsonElement)
        {
            jsonElement.WriteTo(writer);
            return;
        }

        if (value is JsonDocument jsonDocument)
        {
            jsonDocument.RootElement.WriteTo(writer);
            return;
        }

        _ = Throw.IfNull(options);

        if (options.TryGetTypeInfo(value.GetType(), out JsonTypeInfo? typeInfo))
        {
            try
            {
                JsonSerializer.SerializeToElement(value, typeInfo).WriteTo(writer);
                return;
            }
            catch (Exception e) when (e is JsonException or InvalidOperationException or NotSupportedException)
            {
                // Serialization failed; fall through to write empty object.
            }
        }

        writer.WriteStartObject();
        writer.WriteEndObject();
    }
}
