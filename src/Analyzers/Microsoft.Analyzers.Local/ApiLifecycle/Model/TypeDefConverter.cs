// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle.Model;

internal sealed class TypeDefConverter : JsonConverter<TypeDef>
{
    public override TypeDef? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var r = new TypeDef();
        var currDepth = reader.CurrentDepth;

        while (reader.Read())
        {
            if (currDepth == reader.CurrentDepth && reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                // Check if the property name matches the one you're looking for
                if (reader.ValueTextEquals("Type"))
                {
                    _ = reader.Read();
                    var value = reader.GetString() ?? string.Empty;

                    r.ModifiersAndName = Utils.StripBaseAndConstraints(value);
                    r.Constraints = Utils.GetConstraints(value);
                    r.BaseTypes = Utils.GetBaseTypes(value);
                }
                else if (reader.ValueTextEquals("Stage"))
                {
                    _ = reader.Read();
                    var value = reader.GetString() ?? string.Empty;
                    _ = Enum.TryParse<Stage>(value, out var stage);

                    r.Stage = stage;

                }
                else if (reader.ValueTextEquals("Methods"))
                {
                    r.Methods = JsonSerializer.Deserialize<Method[]>(ref reader, options) ?? Array.Empty<Method>();
                }
                else if (reader.ValueTextEquals("Properties"))
                {
                    r.Properties = JsonSerializer.Deserialize<Prop[]>(ref reader, options) ?? Array.Empty<Prop>();
                }
                else if (reader.ValueTextEquals("Fields"))
                {
                    r.Fields = JsonSerializer.Deserialize<Field[]>(ref reader, options) ?? Array.Empty<Field>();
                }
            }
        }

        return r;
    }

    public override void Write(Utf8JsonWriter writer, TypeDef value, JsonSerializerOptions options) => throw new NotSupportedException("We don't need this functionality.");
}
