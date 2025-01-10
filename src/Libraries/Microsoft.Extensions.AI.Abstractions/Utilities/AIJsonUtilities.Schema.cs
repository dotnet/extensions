// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;
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
    /// <summary>The uri used when populating the $schema keyword in inferred schemas.</summary>
    private const string SchemaKeywordUri = "https://json-schema.org/draft/2020-12/schema";

    /// <summary>Soft limit for how many items should be stored in the dictionaries in <see cref="_schemaCaches"/>.</summary>
    private const int CacheSoftLimit = 4096;

    /// <summary>Caches of generated schemas for each <see cref="JsonSerializerOptions"/> that's employed.</summary>
    private static readonly ConditionalWeakTable<JsonSerializerOptions, ConcurrentDictionary<SchemaGenerationKey, JsonElement>> _schemaCaches = new();

    /// <summary>Gets a JSON schema accepting all values.</summary>
    private static readonly JsonElement _trueJsonSchema = ParseJsonElement("true"u8);

    /// <summary>Gets a JSON schema only accepting null values.</summary>
    private static readonly JsonElement _nullJsonSchema = ParseJsonElement("""{"type":"null"}"""u8);

    // List of keywords used by JsonSchemaExporter but explicitly disallowed by some AI vendors.
    // cf. https://platform.openai.com/docs/guides/structured-outputs#some-type-specific-keywords-are-not-yet-supported
    private static readonly string[] _schemaKeywordsDisallowedByAIVendors = ["minLength", "maxLength", "pattern", "format"];

    /// <summary>
    /// Determines a JSON schema for the provided parameter metadata.
    /// </summary>
    /// <param name="parameterMetadata">The parameter metadata from which to infer the schema.</param>
    /// <param name="functionMetadata">The containing function metadata.</param>
    /// <param name="serializerOptions">The options used to extract the schema from the specified type.</param>
    /// <param name="inferenceOptions">The options controlling schema inference.</param>
    /// <returns>A JSON schema document encoded as a <see cref="JsonElement"/>.</returns>
    public static JsonElement ResolveParameterJsonSchema(
        AIFunctionParameterMetadata parameterMetadata,
        AIFunctionMetadata functionMetadata,
        JsonSerializerOptions? serializerOptions = null,
        AIJsonSchemaCreateOptions? inferenceOptions = null)
    {
        _ = Throw.IfNull(parameterMetadata);
        _ = Throw.IfNull(functionMetadata);

        serializerOptions ??= functionMetadata.JsonSerializerOptions ?? DefaultOptions;

        if (ReferenceEquals(serializerOptions, functionMetadata.JsonSerializerOptions) &&
            parameterMetadata.Schema is JsonElement schema)
        {
            // If the resolved options matches that of the function metadata,
            // we can just return the precomputed JSON schema value.
            return schema;
        }

        return CreateParameterJsonSchema(
            parameterMetadata.ParameterType,
            parameterMetadata.Name,
            description: parameterMetadata.Description,
            hasDefaultValue: parameterMetadata.HasDefaultValue,
            defaultValue: parameterMetadata.DefaultValue,
            serializerOptions,
            inferenceOptions);
    }

    /// <summary>
    /// Creates a JSON schema for the provided parameter metadata.
    /// </summary>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="description">The description of the parameter.</param>
    /// <param name="hasDefaultValue"><see langword="true"/> if the parameter is optional; otherwise, <see langword="false"/>.</param>
    /// <param name="defaultValue">The default value of the optional parameter, if applicable.</param>
    /// <param name="serializerOptions">The options used to extract the schema from the specified type.</param>
    /// <param name="inferenceOptions">The options controlling schema inference.</param>
    /// <returns>A JSON schema document encoded as a <see cref="JsonElement"/>.</returns>
    /// <remarks>
    /// Uses a cache keyed on the <paramref name="serializerOptions"/> to store schema result,
    /// unless a <see cref="AIJsonSchemaCreateOptions.TransformSchemaNode" /> delegate has been specified.
    /// </remarks>
    public static JsonElement CreateParameterJsonSchema(
        Type? type,
        string parameterName,
        string? description = null,
        bool hasDefaultValue = false,
        object? defaultValue = null,
        JsonSerializerOptions? serializerOptions = null,
        AIJsonSchemaCreateOptions? inferenceOptions = null)
    {
        _ = Throw.IfNull(parameterName);

        serializerOptions ??= DefaultOptions;
        inferenceOptions ??= AIJsonSchemaCreateOptions.Default;

        SchemaGenerationKey key = new(
            type,
            parameterName,
            description,
            hasDefaultValue,
            defaultValue,
            inferenceOptions);

        return GetJsonSchemaCached(serializerOptions, key);
    }

    /// <summary>Creates a JSON schema for the specified type.</summary>
    /// <param name="type">The type for which to generate the schema.</param>
    /// <param name="description">The description of the parameter.</param>
    /// <param name="hasDefaultValue"><see langword="true"/> if the parameter is optional; otherwise, <see langword="false"/>.</param>
    /// <param name="defaultValue">The default value of the optional parameter, if applicable.</param>
    /// <param name="serializerOptions">The options used to extract the schema from the specified type.</param>
    /// <param name="inferenceOptions">The options controlling schema inference.</param>
    /// <returns>A <see cref="JsonElement"/> representing the schema.</returns>
    /// <remarks>
    /// Uses a cache keyed on the <paramref name="serializerOptions"/> to store schema result,
    /// unless a <see cref="AIJsonSchemaCreateOptions.TransformSchemaNode" /> delegate has been specified.
    /// </remarks>
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

        SchemaGenerationKey key = new(
            type,
            parameterName: null,
            description,
            hasDefaultValue,
            defaultValue,
            inferenceOptions);

        return GetJsonSchemaCached(serializerOptions, key);
    }

    private static JsonElement GetJsonSchemaCached(JsonSerializerOptions options, SchemaGenerationKey key)
    {
        options.MakeReadOnly();
        ConcurrentDictionary<SchemaGenerationKey, JsonElement> cache = _schemaCaches.GetOrCreateValue(options);

        if (key.TransformSchemaNode is not null || cache.Count >= CacheSoftLimit)
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

#if !NET9_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access",
        Justification = "Pre STJ-9 schema extraction can fail with a runtime exception if certain reflection metadata have been trimmed. " +
                        "The exception message will guide users to turn off 'IlcTrimMetadata' which resolves all issues.")]
#endif
    private static JsonElement GetJsonSchemaCore(JsonSerializerOptions options, SchemaGenerationKey key)
    {
        _ = Throw.IfNull(options);
        options.MakeReadOnly();

        if (key.Type is null)
        {
            // For parameters without a type generate a rudimentary schema with available metadata.

            JsonObject? schemaObj = null;

            if (key.IncludeSchemaKeyword)
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
                : JsonSerializer.SerializeToElement(schemaObj, JsonContext.Default.JsonNode);
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
        return JsonSerializer.SerializeToElement(node, JsonContext.Default.JsonNode);

        JsonNode TransformSchemaNode(JsonSchemaExporterContext schemaExporterContext, JsonNode schema)
        {
            const string SchemaPropertyName = "$schema";
            const string DescriptionPropertyName = "description";
            const string NotPropertyName = "not";
            const string TypePropertyName = "type";
            const string PatternPropertyName = "pattern";
            const string EnumPropertyName = "enum";
            const string PropertiesPropertyName = "properties";
            const string RequiredPropertyName = "required";
            const string AdditionalPropertiesPropertyName = "additionalProperties";
            const string DefaultPropertyName = "default";
            const string RefPropertyName = "$ref";

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
                    objSchema.InsertAtStart(TypePropertyName, "string");
                }

                // Disallow additional properties in object schemas
                if (key.DisallowAdditionalProperties &&
                    objSchema.ContainsKey(PropertiesPropertyName) &&
                    !objSchema.ContainsKey(AdditionalPropertiesPropertyName))
                {
                    objSchema.Add(AdditionalPropertiesPropertyName, (JsonNode)false);
                }

                // Mark all properties as required
                if (key.RequireAllProperties &&
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
                // STJ represents .NET integer types as ["string", "integer"], which will then lead to an error.
                if (TypeIsIntegerWithStringNumberHandling(ctx, objSchema))
                {
                    // We don't want to emit any array for "type". In this case we know it contains "integer"
                    // so reduce the type to that alone, assuming it's the most specific type.
                    // This makes schemas for Int32 (etc) work with Ollama.
                    JsonObject obj = ConvertSchemaToObject(ref schema);
                    obj[TypePropertyName] = "integer";
                    _ = obj.Remove(PatternPropertyName);
                }
            }

            if (ctx.Path.IsEmpty)
            {
                // We are at the root-level schema node, update/append parameter-specific metadata

                if (!string.IsNullOrWhiteSpace(key.Description))
                {
                    JsonObject obj = ConvertSchemaToObject(ref schema);
                    JsonNode descriptionNode = (JsonNode)key.Description!;
                    int index = obj.IndexOf(DescriptionPropertyName);
                    if (index < 0)
                    {
                        // If there's no description property, insert it at the beginning of the doc.
                        obj.InsertAtStart(DescriptionPropertyName, (JsonNode)key.Description!);
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

                if (key.IncludeSchemaKeyword)
                {
                    // The $schema property must be the first keyword in the object
                    ConvertSchemaToObject(ref schema).InsertAtStart(SchemaPropertyName, (JsonNode)SchemaKeywordUri);
                }
            }

            // Finally, apply any user-defined transformations if specified.
            if (key.TransformSchemaNode is { } transformer)
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

    private static bool TypeIsIntegerWithStringNumberHandling(AIJsonSchemaCreateContext ctx, JsonObject schema)
    {
        if (ctx.TypeInfo.NumberHandling is not JsonNumberHandling.Strict && schema["type"] is JsonArray { Count: 2 } typeArray)
        {
            bool allowString = false;
            bool allowNumeric = false;

            foreach (JsonNode? entry in typeArray)
            {
                if (entry?.GetValueKind() is JsonValueKind.String &&
                    entry.GetValue<string>() is string type)
                {
                    allowString |= type is "string";
                    allowNumeric |= type is "integer" or "number";
                }
            }

            return allowString && allowNumeric;
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

    /// <summary>The equatable key used to look up cached schemas.</summary>
    private readonly record struct SchemaGenerationKey
    {
        public SchemaGenerationKey(
            Type? type,
            string? parameterName,
            string? description,
            bool hasDefaultValue,
            object? defaultValue,
            AIJsonSchemaCreateOptions options)
        {
            Type = type;
            ParameterName = parameterName;
            Description = description;
            HasDefaultValue = hasDefaultValue;
            DefaultValue = defaultValue;
            IncludeSchemaKeyword = options.IncludeSchemaKeyword;
            DisallowAdditionalProperties = options.DisallowAdditionalProperties;
            IncludeTypeInEnumSchemas = options.IncludeTypeInEnumSchemas;
            RequireAllProperties = options.RequireAllProperties;
            TransformSchemaNode = options.TransformSchemaNode;
        }

        public Type? Type { get; }
        public string? ParameterName { get; }
        public string? Description { get; }
        public bool HasDefaultValue { get; }
        public object? DefaultValue { get; }
        public bool IncludeSchemaKeyword { get; }
        public bool DisallowAdditionalProperties { get; }
        public bool IncludeTypeInEnumSchemas { get; }
        public bool RequireAllProperties { get; }
        public Func<AIJsonSchemaCreateContext, JsonNode, JsonNode>? TransformSchemaNode { get; }
    }
}
