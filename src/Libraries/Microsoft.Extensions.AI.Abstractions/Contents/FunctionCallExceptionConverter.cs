// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
#if NET
using System.Runtime.ExceptionServices;
#endif
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Serializes an exception as a string and deserializes it back as a base <see cref="Exception"/> containing that contents as a message.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FunctionCallExceptionConverter : JsonConverter<Exception>
{
    private const string ClassNamePropertyName = "className";
    private const string MessagePropertyName = "message";
    private const string InnerExceptionPropertyName = "innerException";
    private const string StackTracePropertyName = "stackTraceString";

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
    {
        _ = Throw.IfNull(writer);
        _ = Throw.IfNull(value);

        // Schema and property order taken from Exception.GetObjectData() implementation.

        writer.WriteStartObject();
        writer.WriteString(ClassNamePropertyName, value.GetType().ToString());
        writer.WriteString(MessagePropertyName, value.Message);
        writer.WritePropertyName(InnerExceptionPropertyName);
        if (value.InnerException is Exception innerEx)
        {
            Write(writer, innerEx, options);
        }
        else
        {
            writer.WriteNullValue();
        }

        writer.WriteString(StackTracePropertyName, value.StackTrace);
        writer.WriteEndObject();
    }

    /// <inheritdoc />
    public override Exception? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        return ParseExceptionCore(doc.RootElement);

        static Exception ParseExceptionCore(JsonElement element)
        {
            string? message = null;
            string? stackTrace = null;
            Exception? innerEx = null;

            foreach (JsonProperty property in element.EnumerateObject())
            {
                switch (property.Name)
                {
                    case MessagePropertyName:
                        message = property.Value.GetString();
                        break;

                    case StackTracePropertyName:
                        stackTrace = property.Value.GetString();
                        break;

                    case InnerExceptionPropertyName when property.Value.ValueKind is not JsonValueKind.Null:
                        innerEx = ParseExceptionCore(property.Value);
                        break;
                }
            }

#pragma warning disable CA2201 // Do not raise reserved exception types
            Exception result = new(message, innerEx);
#pragma warning restore CA2201
#if NET
            if (stackTrace != null)
            {
                ExceptionDispatchInfo.SetRemoteStackTrace(result, stackTrace);
            }
#endif
            return result;
        }
    }
}
