// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S1121 // Assignments should not be made from within sub-expressions
#pragma warning disable S107 // Methods should not have too many parameters
#pragma warning disable S1075 // URIs should not be hardcoded
#pragma warning disable SA1118 // Parameter should not span multiple lines
#pragma warning disable S109 // Magic numbers should not be used

namespace Microsoft.Extensions.AI;

/// <summary>Provides a collection of utility methods for marshalling JSON data.</summary>
public static partial class AIJsonUtilities
{
    private const string SchemaPropertyName = "$schema";
    private const string TitlePropertyName = "title";
    private const string DescriptionPropertyName = "description";
    private const string NotPropertyName = "not";
    private const string TypePropertyName = "type";
    private const string PatternPropertyName = "pattern";
    private const string EnumPropertyName = "enum";
    private const string PropertiesPropertyName = "properties";
    private const string RequiredPropertyName = "required";
    private const string AdditionalPropertiesPropertyName = "additionalProperties";
    private const string DefaultPropertyName = "default";
    private const string RefPropertyName = "$ref";

    /// <summary>The uri used when populating the $schema keyword in inferred schemas.</summary>
    private const string SchemaKeywordUri = "https://json-schema.org/draft/2020-12/schema";

    // List of keywords used by JsonSchemaExporter but explicitly disallowed by some AI vendors.
    // cf. https://platform.openai.com/docs/guides/structured-outputs#some-type-specific-keywords-are-not-yet-supported
    private static readonly string[] _schemaKeywordsDisallowedByAIVendors = ["minLength", "maxLength", "pattern", "format"];

    /// <summary>
    /// Determines a JSON schema for the provided method.
    /// </summary>
    /// <param name="method">The method from which to extract schema information.</param>
    /// <param name="title">The title keyword used by the method schema.</param>
    /// <param name="description">The description keyword used by the method schema.</param>
    /// <param name="serializerOptions">The options used to extract the schema from the specified type.</param>
    /// <param name="inferenceOptions">The options controlling schema inference.</param>
    /// <returns>A JSON schema document encoded as a <see cref="JsonElement"/>.</returns>
    public static JsonElement CreateFunctionJsonSchema(
        MethodBase method,
        string? title = null,
        string? description = null,
        JsonSerializerOptions? serializerOptions = null,
        AIJsonSchemaCreateOptions? inferenceOptions = null)
    {
        _ = Throw.IfNull(method);
        serializerOptions ??= DefaultOptions;
        inferenceOptions ??= AIJsonSchemaCreateOptions.Default;
        title ??= method.Name;
        description ??= method.GetCustomAttribute<DescriptionAttribute>()?.Description;

        JsonObject parameterSchemas = new();
        JsonArray? requiredProperties = null;
        foreach (ParameterInfo parameter in method.GetParameters())
        {
            if (string.IsNullOrWhiteSpace(parameter.Name))
            {
                Throw.ArgumentException(nameof(parameter), "Parameter is missing a name.");
            }

            JsonNode parameterSchema = CreateJsonSchemaCore(
                type: parameter.ParameterType,
                parameterName: parameter.Name,
                description: parameter.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description,
                hasDefaultValue: parameter.HasDefaultValue,
                defaultValue: parameter.HasDefaultValue ? parameter.DefaultValue : null,
                serializerOptions,
                inferenceOptions);

            parameterSchemas.Add(parameter.Name, parameterSchema);
            if (!parameter.IsOptional)
            {
                (requiredProperties ??= []).Add((JsonNode)parameter.Name);
            }
        }

        JsonObject schema = new();
        if (inferenceOptions.IncludeSchemaKeyword)
        {
            schema[SchemaPropertyName] = SchemaKeywordUri;
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            schema[TitlePropertyName] = title;
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            schema[DescriptionPropertyName] = description;
        }

        schema[TypePropertyName] = "object"; // Method schemas always hardcode the type as "object".
        schema[PropertiesPropertyName] = parameterSchemas;

        if (requiredProperties is not null)
        {
            schema[RequiredPropertyName] = requiredProperties;
        }

        return JsonSerializer.SerializeToElement(schema, JsonContext.Default.JsonNode);
    }

    /// <summary>Creates a JSON schema for the specified type.</summary>
    /// <param name="type">The type for which to generate the schema.</param>
    /// <param name="description">The description of the parameter.</param>
    /// <param name="hasDefaultValue"><see langword="true"/> if the parameter is optional; otherwise, <see langword="false"/>.</param>
    /// <param name="defaultValue">The default value of the optional parameter, if applicable.</param>
    /// <param name="serializerOptions">The options used to extract the schema from the specified type.</param>
    /// <param name="inferenceOptions">The options controlling schema inference.</param>
    /// <returns>A <see cref="JsonElement"/> representing the schema.</returns>
    public static JsonElement CreateJsonSchema(
        Type? type,
        string? description = null,
        bool hasDefaultValue = false,
        object? defaultValue = null,
        JsonSerializerOptions? serializerOptions = null,
        AIJsonSchemaCreateOptions? inferenceOptions = null)
    {
        serializerOptions ??= DefaultOptions;
        inferenceOptions ??= AIJsonSchemaCreateOptions.Default;
        JsonNode schema = CreateJsonSchemaCore(type, parameterName: null, description, hasDefaultValue, defaultValue, serializerOptions, inferenceOptions);
        return JsonSerializer.SerializeToElement(schema, JsonContext.Default.JsonNode);
    }

    /// <summary>Gets the default JSON schema to be used by types or functions.</summary>
    internal static JsonElement DefaultJsonSchema { get; } = ParseJsonElement("{}"u8);

    /// <summary>Validates the provided JSON schema document.</summary>
    internal static void ValidateSchemaDocument(JsonElement document, [CallerArgumentExpression("document")] string? paramName = null)
    {
        if (document.ValueKind is not JsonValueKind.Object or JsonValueKind.False or JsonValueKind.True)
        {
            Throw.ArgumentException(paramName ?? "schema", "The schema document must be an object or a boolean value.");
        }
    }

#if !NET9_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access",
        Justification = "Pre STJ-9 schema extraction can fail with a runtime exception if certain reflection metadata have been trimmed. " +
                        "The exception message will guide users to turn off 'IlcTrimMetadata' which resolves all issues.")]
#endif
    private static JsonNode CreateJsonSchemaCore(
        Type? type,
        string? parameterName,
        string? description,
        bool hasDefaultValue,
        object? defaultValue,
        JsonSerializerOptions serializerOptions,
        AIJsonSchemaCreateOptions inferenceOptions)
    {
        serializerOptions.MakeReadOnly();

        if (type is null)
        {
            // For parameters without a type generate a rudimentary schema with available metadata.

            JsonObject? schemaObj = null;

            if (inferenceOptions.IncludeSchemaKeyword)
            {
                (schemaObj = [])[SchemaPropertyName] = SchemaKeywordUri;
            }

            if (description is not null)
            {
                (schemaObj ??= [])[DescriptionPropertyName] = description;
            }

            if (hasDefaultValue)
            {
                JsonNode? defaultValueNode = defaultValue is not null
                    ? JsonSerializer.Serialize(defaultValue, serializerOptions.GetTypeInfo(defaultValue.GetType()))
                    : null;

                (schemaObj ??= [])[DefaultPropertyName] = defaultValueNode;
            }

            return schemaObj ?? (JsonNode)true;
        }

        if (type == typeof(void))
        {
            return new JsonObject { [TypePropertyName] = null };
        }

        JsonSchemaExporterOptions exporterOptions = new()
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = TransformSchemaNode,
        };

        return serializerOptions.GetJsonSchemaAsNode(type, exporterOptions);

        JsonNode TransformSchemaNode(JsonSchemaExporterContext schemaExporterContext, JsonNode schema)
        {
            AIJsonSchemaCreateContext ctx = new(schemaExporterContext);

            if (ctx.GetCustomAttribute<DescriptionAttribute>() is { } attr)
            {
                ConvertSchemaToObject(ref schema).InsertAtStart(DescriptionPropertyName, (JsonNode)attr.Description);
            }

            if (schema is JsonObject objSchema)
            {
                // The resulting schema might be a $ref using a pointer to a different location in the document.
                // As JSON pointer doesn't support relative paths, parameter schemas need to fix up such paths
                // to accommodate the fact that they're being nested inside of a higher-level schema.
                if (parameterName is not null && objSchema.TryGetPropertyValue(RefPropertyName, out JsonNode? paramName))
                {
                    // Fix up any $ref URIs to match the path from the root document.
                    string refUri = paramName!.GetValue<string>();
                    Debug.Assert(refUri is "#" || refUri.StartsWith("#/", StringComparison.Ordinal), $"Expected {nameof(refUri)} to be either # or start with #/, got {refUri}");
                    refUri = refUri == "#"
                        ? $"#/{PropertiesPropertyName}/{parameterName}"
                        : $"#/{PropertiesPropertyName}/{parameterName}/{refUri.AsMemory("#/".Length)}";

                    objSchema[RefPropertyName] = (JsonNode)refUri;
                }

                // Include the type keyword in enum types
                if (inferenceOptions.IncludeTypeInEnumSchemas && ctx.TypeInfo.Type.IsEnum && objSchema.ContainsKey(EnumPropertyName) && !objSchema.ContainsKey(TypePropertyName))
                {
                    objSchema.InsertAtStart(TypePropertyName, "string");
                }

                // Disallow additional properties in object schemas
                if (inferenceOptions.DisallowAdditionalProperties &&
                    objSchema.ContainsKey(PropertiesPropertyName) &&
                    !objSchema.ContainsKey(AdditionalPropertiesPropertyName))
                {
                    objSchema.Add(AdditionalPropertiesPropertyName, (JsonNode)false);
                }

                // Mark all properties as required
                if (inferenceOptions.RequireAllProperties &&
                    objSchema.TryGetPropertyValue(PropertiesPropertyName, out JsonNode? properties) &&
                    properties is JsonObject propertiesObj)
                {
                    _ = objSchema.TryGetPropertyValue(RequiredPropertyName, out JsonNode? required);
                    if (required is not JsonArray { } requiredArray || requiredArray.Count != propertiesObj.Count)
                    {
                        requiredArray = [.. propertiesObj.Select(prop => (JsonNode)prop.Key)];
                        objSchema[RequiredPropertyName] = requiredArray;
                    }
                }

                // Filter potentially disallowed keywords.
                foreach (string keyword in _schemaKeywordsDisallowedByAIVendors)
                {
                    _ = objSchema.Remove(keyword);
                }

                // Some consumers of the JSON schema, including Ollama as of v0.3.13, don't understand
                // schemas with "type": [...], and only understand "type" being a single value.
                // In certain configurations STJ represents .NET numeric types as ["string", "number"], which will then lead to an error.
                if (TypeIsIntegerWithStringNumberHandling(ctx, objSchema, out string? numericType))
                {
                    // We don't want to emit any array for "type". In this case we know it contains "integer" or "number",
                    // so reduce the type to that alone, assuming it's the most specific type.
                    // This makes schemas for Int32 (etc) work with Ollama.
                    JsonObject obj = ConvertSchemaToObject(ref schema);
                    obj[TypePropertyName] = numericType;
                    _ = obj.Remove(PatternPropertyName);
                }
            }

            if (ctx.Path.IsEmpty)
            {
                // We are at the root-level schema node, update/append parameter-specific metadata

                if (!string.IsNullOrWhiteSpace(description))
                {
                    JsonObject obj = ConvertSchemaToObject(ref schema);
                    int index = obj.IndexOf(DescriptionPropertyName);
                    if (index < 0)
                    {
                        // If there's no description property, insert it at the beginning of the doc.
                        obj.InsertAtStart(DescriptionPropertyName, (JsonNode)description!);
                    }
                    else
                    {
                        // If there is a description property, just update it in-place.
                        obj[index] = (JsonNode)description!;
                    }
                }

                if (hasDefaultValue)
                {
                    JsonNode? defaultValueNode = JsonSerializer.Serialize(defaultValue, serializerOptions.GetTypeInfo(typeof(object)));
                    ConvertSchemaToObject(ref schema)[DefaultPropertyName] = defaultValueNode;
                }

                if (inferenceOptions.IncludeSchemaKeyword)
                {
                    // The $schema property must be the first keyword in the object
                    ConvertSchemaToObject(ref schema).InsertAtStart(SchemaPropertyName, (JsonNode)SchemaKeywordUri);
                }
            }

            // Finally, apply any user-defined transformations if specified.
            if (inferenceOptions.TransformSchemaNode is { } transformer)
            {
                schema = transformer(ctx, schema);
            }

            return schema;

            static JsonObject ConvertSchemaToObject(ref JsonNode schema)
            {
                JsonObject obj;
                JsonValueKind kind = schema.GetValueKind();
                switch (kind)
                {
                    case JsonValueKind.Object:
                        return (JsonObject)schema;

                    case JsonValueKind.False:
                        schema = obj = new() { [NotPropertyName] = true };
                        return obj;

                    default:
                        Debug.Assert(kind is JsonValueKind.True, $"Invalid schema type: {kind}");
                        schema = obj = [];
                        return obj;
                }
            }
        }
    }

    private static bool TypeIsIntegerWithStringNumberHandling(AIJsonSchemaCreateContext ctx, JsonObject schema, [NotNullWhen(true)] out string? numericType)
    {
        numericType = null;

        if (ctx.TypeInfo.NumberHandling is not JsonNumberHandling.Strict && schema["type"] is JsonArray { Count: 2 } typeArray)
        {
            bool allowString = false;

            foreach (JsonNode? entry in typeArray)
            {
                if (entry?.GetValueKind() is JsonValueKind.String &&
                    entry.GetValue<string>() is string type)
                {
                    switch (type)
                    {
                        case "integer" or "number":
                            numericType = type;
                            break;
                        case "string":
                            allowString = true;
                            break;
                    }
                }
            }

            return allowString && numericType is not null;
        }

        return false;
    }

    private static void InsertAtStart(this JsonObject jsonObject, string key, JsonNode value)
    {
#if NET9_0_OR_GREATER
        jsonObject.Insert(0, key, value);
#else
        jsonObject.Remove(key);
        var copiedEntries = jsonObject.ToArray();
        jsonObject.Clear();

        jsonObject.Add(key, value);
        foreach (var entry in copiedEntries)
        {
            jsonObject[entry.Key] = entry.Value;
        }
#endif
    }

#if !NET9_0_OR_GREATER
    private static int IndexOf(this JsonObject jsonObject, string key)
    {
        int i = 0;
        foreach (var entry in jsonObject)
        {
            if (string.Equals(entry.Key, key, StringComparison.Ordinal))
            {
                return i;
            }

            i++;
        }

        return -1;
    }
#endif
    private static JsonElement ParseJsonElement(ReadOnlySpan<byte> utf8Json)
    {
        Utf8JsonReader reader = new(utf8Json);
        return JsonElement.ParseValue(ref reader);
    }
}
