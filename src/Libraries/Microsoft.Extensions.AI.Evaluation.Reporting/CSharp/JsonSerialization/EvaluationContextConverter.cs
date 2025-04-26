// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;

internal sealed class EvaluationContextConverter : JsonConverter<EvaluationContext>
{
    private sealed class DeserializedEvaluationContext(string name, IReadOnlyList<AIContent> contents)
        : EvaluationContext(name, contents);

    private const string NamePropertyName = "name";
    private const string ContentsPropertyName = "contents";

    public override EvaluationContext Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.StartObject)
        {
            throw new JsonException($"Unexpected token '{reader.TokenType}'.");
        }

        string? name = null;
        IReadOnlyList<AIContent>? contents = null;

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject || (name is not null && contents is not null))
            {
                break;
            }

            if (reader.TokenType is JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString()!;
                if (!reader.Read())
                {
                    throw new JsonException(
                        $"Failed to read past the '{JsonTokenType.PropertyName}' token for property with name '{propertyName}'.");
                }

                switch (propertyName)
                {
                    case NamePropertyName:
                        if (reader.TokenType is not JsonTokenType.String)
                        {
                            throw new JsonException(
                                $"Expected '{JsonTokenType.String}' but found '{reader.TokenType}' after '{JsonTokenType.PropertyName}' token for property with name '{propertyName}'.");
                        }

                        name = reader.GetString();
                        break;

                    case ContentsPropertyName:
                        if (reader.TokenType is not JsonTokenType.StartArray)
                        {
                            throw new JsonException(
                                $"Expected '{JsonTokenType.StartArray}' but found '{reader.TokenType}' after '{JsonTokenType.PropertyName}' token for property with name '{propertyName}'.");
                        }

                        JsonTypeInfo contentsTypeInfo = options.GetTypeInfo(typeof(IReadOnlyList<AIContent>));
                        contents = JsonSerializer.Deserialize(ref reader, contentsTypeInfo) as IReadOnlyList<AIContent>;
                        break;
                }
            }
        }

        if (name is null || contents is null)
        {
            throw new JsonException($"Missing required properties '{NamePropertyName}' and '{ContentsPropertyName}'.");
        }

        return new DeserializedEvaluationContext(name, contents);
    }

    public override void Write(Utf8JsonWriter writer, EvaluationContext value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString(NamePropertyName, value.Name);

        writer.WritePropertyName(ContentsPropertyName);
        JsonTypeInfo contentsTypeInfo = options.GetTypeInfo(typeof(IReadOnlyList<AIContent>));
        JsonSerializer.Serialize(writer, value.Contents, contentsTypeInfo);

        writer.WriteEndObject();
    }
}
