// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

public static partial class AIJsonUtilities
{
    /// <summary>
    /// Transforms the given JSON schema based on the provided options.
    /// </summary>
    /// <param name="schema">The schema document to transform.</param>
    /// <param name="transformOptions">The options governing schema transformation.</param>
    /// <returns>A new schema document with transformations applied.</returns>
    /// <remarks>The schema and any nested schemas are transformed using depth-first traversal.</remarks>
    public static JsonElement TransformSchema(JsonElement schema, AIJsonSchemaTransformOptions transformOptions)
    {
        _ = Throw.IfNull(transformOptions);

        if (transformOptions == AIJsonSchemaTransformOptions.Default)
        {
            Throw.ArgumentException(nameof(transformOptions), "The options instance does not specify any transformations.");
        }

        JsonNode? nodeSchema = JsonSerializer.SerializeToNode(schema, JsonContext.Default.JsonElement);
        JsonNode transformedSchema = TransformSchema(nodeSchema, transformOptions);
        return JsonSerializer.SerializeToElement(transformedSchema, JsonContextNoIndentation.Default.JsonNode);
    }

    private static JsonNode TransformSchema(JsonNode? schema, AIJsonSchemaTransformOptions transformOptions)
    {
        List<string>? path = transformOptions.TransformSchemaNode is not null ? [] : null;
        return TransformSchemaCore(schema, transformOptions, path);
    }

    private static JsonNode TransformSchemaCore(JsonNode? schema, AIJsonSchemaTransformOptions transformOptions, List<string>? path)
    {
        switch (schema?.GetValueKind())
        {
            case JsonValueKind.False:
                if (transformOptions.ConvertBooleanSchemas)
                {
                    schema = new JsonObject { [NotPropertyName] = (JsonNode)true };
                }

                break;

            case JsonValueKind.True:
                if (transformOptions.ConvertBooleanSchemas)
                {
                    schema = new JsonObject();
                }

                break;

            case JsonValueKind.Object:
                JsonObject schemaObj = (JsonObject)schema;
                JsonObject? properties = null;

                // Step 1. Recursively apply transformations to any nested schemas we might be able to detect.
                if (schemaObj.TryGetPropertyValue(PropertiesPropertyName, out JsonNode? props) && props is JsonObject propsObj)
                {
                    properties = propsObj;
                    path?.Add(PropertiesPropertyName);
                    foreach (var prop in properties.ToArray())
                    {
                        path?.Add(prop.Key);
                        properties[prop.Key] = TransformSchemaCore(prop.Value, transformOptions, path);
                        path?.RemoveAt(path.Count - 1);
                    }

                    path?.RemoveAt(path.Count - 1);
                }

                if (schemaObj.TryGetPropertyValue(ItemsPropertyName, out JsonNode? itemsSchema))
                {
                    path?.Add(ItemsPropertyName);
                    schemaObj[ItemsPropertyName] = TransformSchemaCore(itemsSchema, transformOptions, path);
                    path?.RemoveAt(path.Count - 1);
                }

                if (schemaObj.TryGetPropertyValue(AdditionalPropertiesPropertyName, out JsonNode? additionalProps) &&
                    additionalProps?.GetValueKind() is not JsonValueKind.False)
                {
                    path?.Add(AdditionalPropertiesPropertyName);
                    schemaObj[AdditionalPropertiesPropertyName] = TransformSchemaCore(additionalProps, transformOptions, path);
                    path?.RemoveAt(path.Count - 1);
                }

                if (schemaObj.TryGetPropertyValue(NotPropertyName, out JsonNode? notSchema))
                {
                    path?.Add(NotPropertyName);
                    schemaObj[NotPropertyName] = TransformSchemaCore(notSchema, transformOptions, path);
                    path?.RemoveAt(path.Count - 1);
                }

                // Traverse keywords that contain arrays of schemas
                ReadOnlySpan<string> combinatorKeywords = ["anyOf", "oneOf", "allOf"];
                foreach (string combinatorKeyword in combinatorKeywords)
                {
                    if (schemaObj.TryGetPropertyValue(combinatorKeyword, out JsonNode? combinatorSchema) && combinatorSchema is JsonArray combinatorArray)
                    {
                        path?.Add(combinatorKeyword);
                        for (int i = 0; i < combinatorArray.Count; i++)
                        {
                            path?.Add($"[{i}]");
                            JsonNode element = TransformSchemaCore(combinatorArray[i], transformOptions, path);
                            if (!ReferenceEquals(element, combinatorArray[i]))
                            {
                                combinatorArray[i] = element;
                            }

                            path?.RemoveAt(path.Count - 1);
                        }

                        path?.RemoveAt(path.Count - 1);
                    }
                }

                // Step 2. Apply node-level transformations per the settings.
                if (transformOptions.DisallowAdditionalProperties && properties is not null && !schemaObj.ContainsKey(AdditionalPropertiesPropertyName))
                {
                    schemaObj[AdditionalPropertiesPropertyName] = (JsonNode)false;
                }

                if (transformOptions.RequireAllProperties && properties is not null)
                {
                    JsonArray requiredProps = [];
                    foreach (var prop in properties)
                    {
                        requiredProps.Add((JsonNode)prop.Key);
                    }

                    schemaObj[RequiredPropertyName] = requiredProps;
                }

                if (transformOptions.UseNullableKeyword &&
                    schemaObj.TryGetPropertyValue(TypePropertyName, out JsonNode? typeSchema) &&
                    typeSchema is JsonArray typeArray)
                {
                    bool isNullable = false;
                    string? foundType = null;

                    foreach (JsonNode? typeNode in typeArray)
                    {
                        string typeString = (string)typeNode!;
                        if (typeString is "null")
                        {
                            isNullable = true;
                            continue;
                        }

                        if (foundType is not null)
                        {
                            // The array contains more than one non-null types, abort the transformation.
                            foundType = null;
                            break;
                        }

                        foundType = typeString;
                    }

                    if (isNullable && foundType is not null)
                    {
                        schemaObj["type"] = (JsonNode)foundType;
                        schemaObj["nullable"] = (JsonNode)true;
                    }
                }

                if (transformOptions.MoveDefaultKeywordToDescription &&
                    schemaObj.TryGetPropertyValue(DefaultPropertyName, out JsonNode? defaultSchema))
                {
                    string? description = schemaObj.TryGetPropertyValue(DescriptionPropertyName, out JsonNode? descriptionSchema) ? descriptionSchema?.GetValue<string>() : null;
                    string defaultValueJson = JsonSerializer.Serialize(defaultSchema, JsonContextNoIndentation.Default.JsonNode!);
                    description = description is null
                        ? $"Default value: {defaultValueJson}"
                        : $"{description} (Default value: {defaultValueJson})";
                    schemaObj[DescriptionPropertyName] = description;
                    _ = schemaObj.Remove(DefaultPropertyName);
                }

                break;

            default:
                Throw.ArgumentException(nameof(schema), "Schema must be an object or a boolean value.");
                break;
        }

        // Apply user-defined transformations as the final step.
        if (transformOptions.TransformSchemaNode is { } transformer)
        {
            Debug.Assert(path != null, "Path should not be null when TransformSchemaNode is provided.");
            schema = transformer(new AIJsonSchemaTransformContext(path!.ToArray()), schema);
        }

        return schema;
    }
}
