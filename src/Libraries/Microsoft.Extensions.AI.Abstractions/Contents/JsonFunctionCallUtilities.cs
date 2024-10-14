// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S1121 // Assignments should not be made from within sub-expressions
#pragma warning disable S107 // Methods should not have too many parameters

using FunctionParameterKey = (
    System.Type? Type,
    string? ParameterName,
    string? Description,
    bool HasDefaultValue,
    object? DefaultValue,
    bool IncludeSchemaUri,
    bool DisallowAdditionalProperties,
    bool IncludeTypeInEnumSchemas);

namespace Microsoft.Extensions.AI;

/// <summary>Provides a collection of static utility methods for marshalling JSON data in function calling.</summary>
public static partial class JsonFunctionCallUtilities
{
    /// <summary>The uri used when populating the $schema keyword in inferred schemas.</summary>
#pragma warning disable S1075 // URIs should not be hardcoded
    private const string SchemaKeywordUri = "https://json-schema.org/draft/2020-12/schema";
#pragma warning restore S1075 // URIs should not be hardcoded

    /// <summary>Soft limit for how many items should be stored in the dictionaries in <see cref="_schemaCaches"/>.</summary>
    private const int CacheSoftLimit = 4096;

    /// <summary>Caches of generated schemas for each <see cref="JsonSerializerOptions"/> that's employed.</summary>
    private static readonly ConditionalWeakTable<JsonSerializerOptions, ConcurrentDictionary<FunctionParameterKey, JsonElement>> _schemaCaches = new();

    /// <summary>Gets a JSON schema accepting all values.</summary>
    private static readonly JsonElement _trueJsonSchema = ParseJsonElement("true"u8);

    /// <summary>Gets a JSON schema only accepting null values.</summary>
    private static readonly JsonElement _nullJsonSchema = ParseJsonElement("""{"type":"null"}"""u8);

    /// <summary>
    /// Removes characters from a .NET member name that shouldn't be used in an AI function name.
    /// </summary>
    /// <param name="memberName">The .NET member name that should be sanitized.</param>
    /// <returns>
    /// Replaces non-alphanumeric characters in the identifier with the underscore character.
    /// Primarily intended to remove characters produced by compiler-generated method name mangling.
    /// </returns>
    public static string SanitizeMemberName(string memberName)
    {
        _ = Throw.IfNull(memberName);
        return InvalidNameCharsRegex().Replace(memberName, "_");
    }

    /// <summary>Parses a JSON object into a <see cref="FunctionCallContent"/> with arguments encoded as <see cref="JsonElement"/>.</summary>
    /// <param name="json">A JSON object containing the parameters.</param>
    /// <param name="callId">The function call ID.</param>
    /// <param name="functionName">The function name.</param>
    /// <returns>The parsed dictionary of objects encoded as <see cref="JsonElement"/>.</returns>
    public static FunctionCallContent ParseFunctionCallContent([StringSyntax(StringSyntaxAttribute.Json)] string json, string callId, string functionName)
    {
        _ = Throw.IfNull(callId);
        _ = Throw.IfNull(functionName);
        _ = Throw.IfNull(json);

        Dictionary<string, object?>? arguments = null;
        Exception? parsingException = null;

        try
        {
            arguments = JsonSerializer.Deserialize(json, FunctionCallUtilityContext.Default.DictionaryStringObject);
        }
        catch (JsonException ex)
        {
            parsingException = new InvalidOperationException($"Function call arguments contained invalid JSON: {json}", ex);
        }

        return new FunctionCallContent(callId, functionName, arguments)
        {
            Exception = parsingException
        };
    }

    /// <summary>Parses a JSON object into a <see cref="FunctionCallContent"/> with arguments encoded as <see cref="JsonElement"/>.</summary>
    /// <param name="utf8Json">A UTF-8 encoded JSON object containing the parameters.</param>
    /// <param name="callId">The function call ID.</param>
    /// <param name="functionName">The function name.</param>
    /// <returns>The parsed dictionary of objects encoded as <see cref="JsonElement"/>.</returns>
    public static FunctionCallContent ParseFunctionCallContent(ReadOnlySpan<byte> utf8Json, string callId, string functionName)
    {
        _ = Throw.IfNull(callId);
        _ = Throw.IfNull(functionName);

        Dictionary<string, object?>? arguments = null;
        Exception? parsingException = null;

        try
        {
            arguments = JsonSerializer.Deserialize(utf8Json, FunctionCallUtilityContext.Default.DictionaryStringObject);
        }
        catch (JsonException ex)
        {
            parsingException = new InvalidOperationException($"Function call arguments contained invalid JSON: {Encoding.UTF8.GetString(utf8Json.ToArray())}", ex);
        }

        return new FunctionCallContent(callId, functionName, arguments)
        {
            Exception = parsingException
        };
    }

    /// <summary>
    /// Determines a JSON schema for the provided parameter metadata.
    /// </summary>
    /// <param name="parameterMetadata">The parameter metadata from which to infer the schema.</param>
    /// <param name="functionMetadata">The containing function metadata.</param>
    /// <param name="serializerOptions">The global <see cref="JsonSerializerOptions"/> governing serialization.</param>
    /// <param name="inferenceOptions">The options controlling schema inference.</param>
    /// <returns>A JSON schema document encoded as a <see cref="JsonElement"/>.</returns>
    public static JsonElement InferParameterJsonSchema(
        AIFunctionParameterMetadata parameterMetadata,
        AIFunctionMetadata functionMetadata,
        JsonSerializerOptions? serializerOptions,
        JsonSchemaInferenceOptions? inferenceOptions = null)
    {
        _ = Throw.IfNull(parameterMetadata);
        _ = Throw.IfNull(functionMetadata);

        serializerOptions ??= functionMetadata.JsonSerializerOptions;

        if (ReferenceEquals(serializerOptions, functionMetadata.JsonSerializerOptions) &&
            parameterMetadata.Schema is JsonElement schema)
        {
            // If the resolved options matches that of the function metadata,
            // we can just return the precomputed JSON schema value.
            return schema;
        }

        if (serializerOptions is null)
        {
            return _trueJsonSchema;
        }

        return InferParameterJsonSchema(
            parameterMetadata.ParameterType,
            parameterMetadata.Name,
            serializerOptions,
            description: parameterMetadata.Description,
            hasDefaultValue: parameterMetadata.HasDefaultValue,
            defaultValue: parameterMetadata.DefaultValue,
            inferenceOptions);
    }

    /// <summary>
    /// Determines a JSON schema for the provided parameter metadata.
    /// </summary>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="serializerOptions">The options used to extract the schema from the specified type.</param>
    /// <param name="description">The description of the parameter.</param>
    /// <param name="hasDefaultValue">Whether the parameter is optional.</param>
    /// <param name="defaultValue">The default value of the optional parameter, if applicable.</param>
    /// <param name="inferenceOptions">The options controlling schema inference.</param>
    /// <returns>A JSON schema document encoded as a <see cref="JsonElement"/>.</returns>
    public static JsonElement InferParameterJsonSchema(
        Type? type,
        string parameterName,
        JsonSerializerOptions serializerOptions,
        string? description = null,
        bool hasDefaultValue = false,
        object? defaultValue = null,
        JsonSchemaInferenceOptions? inferenceOptions = null)
    {
        _ = Throw.IfNull(parameterName);
        _ = Throw.IfNull(serializerOptions);
        inferenceOptions ??= JsonSchemaInferenceOptions.Default;

        FunctionParameterKey key = (
            type,
            parameterName,
            description,
            hasDefaultValue,
            defaultValue,
            IncludeSchemaUri: false,
            inferenceOptions.DisallowAdditionalProperties,
            inferenceOptions.IncludeTypeInEnumSchemas);

        return GetJsonSchemaCached(serializerOptions, key);
    }

    /// <summary>Infers a JSON schema for the specified type.</summary>
    /// <param name="type">The type for which to generate the schema.</param>
    /// <param name="serializerOptions">The options used to extract the schema from the specified type.</param>
    /// <param name="description">The description of the parameter.</param>
    /// <param name="hasDefaultValue">Whether the parameter is optional.</param>
    /// <param name="defaultValue">The default value of the optional parameter, if applicable.</param>
    /// <param name="inferenceOptions">The options controlling schema inference.</param>
    /// <returns>A <see cref="JsonElement"/> representing the schema.</returns>
    public static JsonElement InferJsonSchema(
        Type? type,
        JsonSerializerOptions serializerOptions,
        string? description = null,
        bool hasDefaultValue = false,
        object? defaultValue = null,
        JsonSchemaInferenceOptions? inferenceOptions = null)
    {
        _ = Throw.IfNull(serializerOptions);
        inferenceOptions ??= JsonSchemaInferenceOptions.Default;

        FunctionParameterKey key = (
            type,
            ParameterName: null,
            description,
            hasDefaultValue,
            defaultValue,
            inferenceOptions.IncludeSchemaKeyword,
            inferenceOptions.DisallowAdditionalProperties,
            inferenceOptions.IncludeTypeInEnumSchemas);

        return GetJsonSchemaCached(serializerOptions, key);
    }

    private static JsonElement GetJsonSchemaCached(JsonSerializerOptions options, FunctionParameterKey key)
    {
        options.MakeReadOnly();
        ConcurrentDictionary<FunctionParameterKey, JsonElement> cache = _schemaCaches.GetOrCreateValue(options);

        if (cache.Count >= CacheSoftLimit)
        {
            return GetJsonSchemaCore(options, key);
        }

        return cache.GetOrAdd(
            key: key,
#if NET
            valueFactory: static (key, options) => GetJsonSchemaCore(options, key),
            factoryArgument: options);
#else
            valueFactory: key => GetJsonSchemaCore(options, key));
#endif
    }

    private static JsonElement GetJsonSchemaCore(JsonSerializerOptions options, FunctionParameterKey key)
    {
        _ = Throw.IfNull(options);
        options.MakeReadOnly();

        if (options.ReferenceHandler == ReferenceHandler.Preserve)
        {
            throw new NotSupportedException("Schema generation not supported with ReferenceHandler.Preserve enabled.");
        }

        if (key.Type is null)
        {
            // For parameters without a type generate a rudimentary schema with available metadata.

            JsonObject? schemaObj = null;

            if (key.IncludeSchemaUri)
            {
                (schemaObj = [])["$schema"] = SchemaKeywordUri;
            }

            if (key.Description is not null)
            {
                (schemaObj ??= [])["description"] = key.Description;
            }

            if (key.HasDefaultValue)
            {
                JsonNode? defaultValueNode = key.DefaultValue is { } defaultValue
                    ? JsonSerializer.Serialize(defaultValue, options.GetTypeInfo(defaultValue.GetType()))
                    : null;

                (schemaObj ??= [])["default"] = defaultValueNode;
            }

            return schemaObj is null
                ? _trueJsonSchema
                : JsonSerializer.SerializeToElement(schemaObj, FunctionCallUtilityContext.Default.JsonNode);
        }

        if (key.Type == typeof(void))
        {
            return _nullJsonSchema;
        }

        JsonSchemaExporterOptions exporterOptions = new()
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = TransformSchemaNode,
        };

        JsonNode node = options.GetJsonSchemaAsNode(key.Type, exporterOptions);
        return JsonSerializer.SerializeToElement(node, FunctionCallUtilityContext.Default.JsonNode);

        JsonNode TransformSchemaNode(JsonSchemaExporterContext ctx, JsonNode schema)
        {
            const string SchemaPropertyName = "$schema";
            const string DescriptionPropertyName = "description";
            const string NotPropertyName = "not";
            const string TypePropertyName = "type";
            const string EnumPropertyName = "enum";
            const string PropertiesPropertyName = "properties";
            const string AdditionalPropertiesPropertyName = "additionalProperties";
            const string DefaultPropertyName = "default";
            const string RefPropertyName = "$ref";

            // Find the first DescriptionAttribute, starting first from the property, then the parameter, and finally the type itself.
            Type descAttrType = typeof(DescriptionAttribute);
            var descriptionAttribute =
                GetAttrs(descAttrType, ctx.PropertyInfo?.AttributeProvider)?.FirstOrDefault() ??
                GetAttrs(descAttrType, ctx.PropertyInfo?.AssociatedParameter?.AttributeProvider)?.FirstOrDefault() ??
                GetAttrs(descAttrType, ctx.TypeInfo.Type)?.FirstOrDefault();

            if (descriptionAttribute is DescriptionAttribute attr)
            {
                ConvertSchemaToObject(ref schema).Insert(0, DescriptionPropertyName, (JsonNode)attr.Description);
            }

            if (schema is JsonObject objSchema)
            {
                // The resulting schema might be a $ref using a pointer to a different location in the document.
                // As JSON pointer doesn't support relative paths, parameter schemas need to fix up such paths
                // to accommodate the fact that they're being nested inside of a higher-level schema.
                if (key.ParameterName is not null && objSchema.TryGetPropertyValue(RefPropertyName, out JsonNode? paramName))
                {
                    // Fix up any $ref URIs to match the path from the root document.
                    string refUri = paramName!.GetValue<string>();
                    Debug.Assert(refUri is "#" || refUri.StartsWith("#/", StringComparison.Ordinal), $"Expected {nameof(refUri)} to be either # or start with #/, got {refUri}");
                    refUri = refUri == "#"
                        ? $"#/{PropertiesPropertyName}/{key.ParameterName}"
                        : $"#/{PropertiesPropertyName}/{key.ParameterName}/{refUri.AsMemory("#/".Length)}";

                    objSchema[RefPropertyName] = (JsonNode)refUri;
                }

                // Include the type keyword in enum types
                if (key.IncludeTypeInEnumSchemas && ctx.TypeInfo.Type.IsEnum && objSchema.ContainsKey(EnumPropertyName) && !objSchema.ContainsKey(TypePropertyName))
                {
                    objSchema.Insert(0, TypePropertyName, "string");
                }

                // Disallow additional properties in object schemas
                if (key.DisallowAdditionalProperties && objSchema.ContainsKey(PropertiesPropertyName) && !objSchema.ContainsKey(AdditionalPropertiesPropertyName))
                {
                    objSchema.Add(AdditionalPropertiesPropertyName, (JsonNode)false);
                }
            }

            if (ctx.Path.IsEmpty)
            {
                // We are at the root-level schema node, append parameter-specific metadata

                if (!string.IsNullOrWhiteSpace(key.Description))
                {
                    JsonObject obj = ConvertSchemaToObject(ref schema);
                    JsonNode descriptionNode = (JsonNode)key.Description!;
                    int index = obj.IndexOf(DescriptionPropertyName);
                    if (index < 0)
                    {
                        // If there's no description property, insert it at the beginning of the doc.
                        obj.Insert(0, DescriptionPropertyName, (JsonNode)key.Description!);
                    }
                    else
                    {
                        // If there is a description property, just update it in-place.
                        obj[index] = (JsonNode)key.Description!;
                    }
                }

                if (key.HasDefaultValue)
                {
                    JsonNode? defaultValue = JsonSerializer.Serialize(key.DefaultValue, options.GetTypeInfo(typeof(object)));
                    ConvertSchemaToObject(ref schema)[DefaultPropertyName] = defaultValue;
                }

                if (key.IncludeSchemaUri)
                {
                    // The $schema property must be the first keyword in the object
                    ConvertSchemaToObject(ref schema).Insert(0, SchemaPropertyName, (JsonNode)SchemaKeywordUri);
                }
            }

            return schema;

            static object[]? GetAttrs(Type attrType, ICustomAttributeProvider? provider) =>
                provider?.GetCustomAttributes(attrType, inherit: false);

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

    private static JsonElement ParseJsonElement(ReadOnlySpan<byte> utf8Json)
    {
        Utf8JsonReader reader = new(utf8Json);
        return JsonElement.ParseValue(ref reader);
    }

    [JsonSerializable(typeof(Dictionary<string, object?>))]
    [JsonSerializable(typeof(JsonNode))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(JsonDocument))]
    private sealed partial class FunctionCallUtilityContext : JsonSerializerContext;

    /// <summary>Regex that flags any character other than ASCII digits or letters or the underscore.</summary>
#if NET
    [GeneratedRegex("[^0-9A-Za-z_]")]
    private static partial Regex InvalidNameCharsRegex();
#else
    private static Regex InvalidNameCharsRegex() => _invalidNameCharsRegex;
    private static readonly Regex _invalidNameCharsRegex = new("[^0-9A-Za-z_]", RegexOptions.Compiled);
#endif
}
