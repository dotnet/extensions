// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

using FunctionParameterKey = (System.Type? Type, string ParameterName, string? Description, bool HasDefaultValue, object? DefaultValue);

namespace Microsoft.Extensions.AI;

/// <summary>Provides a collection of static utility methods for marshalling JSON data in function calling.</summary>
internal static partial class FunctionCallHelpers
{
    /// <summary>Soft limit for how many items should be stored in the dictionaries in <see cref="_schemaCaches"/>.</summary>
    private const int CacheSoftLimit = 4096;

    /// <summary>Caches of generated schemas for each <see cref="JsonSerializerOptions"/> that's employed.</summary>
    private static readonly ConditionalWeakTable<JsonSerializerOptions, ConcurrentDictionary<FunctionParameterKey, JsonElement>> _schemaCaches = new();

    /// <summary>Gets a JSON schema accepting all values.</summary>
    private static JsonElement TrueJsonSchema { get; } = ParseJsonElement("true"u8);

    /// <summary>Gets a JSON schema only accepting null values.</summary>
    private static JsonElement NullJsonSchema { get; } = ParseJsonElement("""{"type":"null"}"""u8);

    /// <summary>Parses a JSON object into a dictionary of objects encoded as <see cref="JsonElement"/>.</summary>
    /// <param name="json">A JSON object containing the parameters.</param>
    /// <param name="parsingException">If the parsing fails, the resulting exception.</param>
    /// <returns>The parsed dictionary of objects encoded as <see cref="JsonElement"/>.</returns>
    public static Dictionary<string, object?>? ParseFunctionCallArguments(string json, out Exception? parsingException)
    {
        _ = Throw.IfNull(json);

        parsingException = null;
        try
        {
            return JsonSerializer.Deserialize(json, FunctionCallHelperContext.Default.DictionaryStringObject);
        }
        catch (JsonException ex)
        {
            parsingException = new InvalidOperationException($"Function call arguments contained invalid JSON: {json}", ex);
            return null;
        }
    }

    /// <summary>Parses a JSON object into a dictionary of objects encoded as <see cref="JsonElement"/>.</summary>
    /// <param name="utf8Json">A UTF-8 encoded JSON object containing the parameters.</param>
    /// <param name="parsingException">If the parsing fails, the resulting exception.</param>
    /// <returns>The parsed dictionary of objects encoded as <see cref="JsonElement"/>.</returns>
    public static Dictionary<string, object?>? ParseFunctionCallArguments(ReadOnlySpan<byte> utf8Json, out Exception? parsingException)
    {
        parsingException = null;
        try
        {
            return JsonSerializer.Deserialize(utf8Json, FunctionCallHelperContext.Default.DictionaryStringObject);
        }
        catch (JsonException ex)
        {
            parsingException = new InvalidOperationException($"Function call arguments contained invalid JSON: {Encoding.UTF8.GetString(utf8Json.ToArray())}", ex);
            return null;
        }
    }

    /// <summary>
    /// Serializes a dictionary of function parameters into a JSON string.
    /// </summary>
    /// <param name="parameters">The dictionary of parameters.</param>
    /// <param name="options">A <see cref="JsonSerializerOptions"/> governing serialization.</param>
    /// <returns>A JSON encoding of the parameters.</returns>
    public static string FormatFunctionParametersAsJson(IDictionary<string, object?>? parameters, JsonSerializerOptions? options = null)
    {
        // Fall back to the built-in context since in most cases the return value is JsonElement or JsonNode.
        options ??= FunctionCallHelperContext.Default.Options;
        options.MakeReadOnly();
        return JsonSerializer.Serialize(parameters, options.GetTypeInfo(typeof(IDictionary<string, object>)));
    }

    /// <summary>
    /// Serializes a dictionary of function parameters into a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="parameters">The dictionary of parameters.</param>
    /// <param name="options">A <see cref="JsonSerializerOptions"/> governing serialization.</param>
    /// <returns>A JSON encoding of the parameters.</returns>
    public static JsonElement FormatFunctionParametersAsJsonElement(IDictionary<string, object?>? parameters, JsonSerializerOptions? options = null)
    {
        // Fall back to the built-in context since in most cases the return value is JsonElement or JsonNode.
        options ??= FunctionCallHelperContext.Default.Options;
        options.MakeReadOnly();
        return JsonSerializer.SerializeToElement(parameters, options.GetTypeInfo(typeof(IDictionary<string, object>)));
    }

    /// <summary>
    /// Serializes a .NET function return parameter to a JSON string.
    /// </summary>
    /// <param name="result">The result value to be serialized.</param>
    /// <param name="options">A <see cref="JsonSerializerOptions"/> governing serialization.</param>
    /// <returns>A JSON encoding of the parameter.</returns>
    public static string FormatFunctionResultAsJson(object? result, JsonSerializerOptions? options = null)
    {
        // Fall back to the built-in context since in most cases the return value is JsonElement or JsonNode.
        options ??= FunctionCallHelperContext.Default.Options;
        options.MakeReadOnly();
        return JsonSerializer.Serialize(result, options.GetTypeInfo(typeof(object)));
    }

    /// <summary>
    /// Serializes a .NET function return parameter to a JSON element.
    /// </summary>
    /// <param name="result">The result value to be serialized.</param>
    /// <param name="options">A <see cref="JsonSerializerOptions"/> governing serialization.</param>
    /// <returns>A JSON encoding of the parameter.</returns>
    public static JsonElement FormatFunctionResultAsJsonElement(object? result, JsonSerializerOptions? options = null)
    {
        // Fall back to the built-in context since in most cases the return value is JsonElement or JsonNode.
        options ??= FunctionCallHelperContext.Default.Options;
        options.MakeReadOnly();
        return JsonSerializer.SerializeToElement(result, options.GetTypeInfo(typeof(object)));
    }

    /// <summary>
    /// Determines a JSON schema for the provided parameter metadata.
    /// </summary>
    /// <param name="parameterMetadata">The parameter metadata from which to infer the schema.</param>
    /// <param name="functionMetadata">The containing function metadata.</param>
    /// <param name="options">The global <see cref="JsonSerializerOptions"/> governing serialization.</param>
    /// <returns>A JSON schema document encoded as a <see cref="JsonElement"/>.</returns>
    public static JsonElement InferParameterJsonSchema(
        AIFunctionParameterMetadata parameterMetadata,
        AIFunctionMetadata functionMetadata,
        JsonSerializerOptions? options)
    {
        options ??= functionMetadata.JsonSerializerOptions;

        if (ReferenceEquals(options, functionMetadata.JsonSerializerOptions) &&
            parameterMetadata.Schema is JsonElement schema)
        {
            // If the resolved options matches that of the function metadata,
            // we can just return the precomputed JSON schema value.
            return schema;
        }

        if (options is null)
        {
            return TrueJsonSchema;
        }

        return InferParameterJsonSchema(
            parameterMetadata.ParameterType,
            parameterMetadata.Name,
            parameterMetadata.Description,
            parameterMetadata.HasDefaultValue,
            parameterMetadata.DefaultValue,
            options);
    }

    /// <summary>
    /// Determines a JSON schema for the provided parameter metadata.
    /// </summary>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="description">The description of the parameter.</param>
    /// <param name="hasDefaultValue">Whether the parameter is optional.</param>
    /// <param name="defaultValue">The default value of the optional parameter, if applicable.</param>
    /// <param name="options">The options used to extract the schema from the specified type.</param>
    /// <returns>A JSON schema document encoded as a <see cref="JsonElement"/>.</returns>
    public static JsonElement InferParameterJsonSchema(
        Type? type,
        string name,
        string? description,
        bool hasDefaultValue,
        object? defaultValue,
        JsonSerializerOptions options)
    {
        _ = Throw.IfNull(name);
        _ = Throw.IfNull(options);

        options.MakeReadOnly();

        try
        {
            ConcurrentDictionary<FunctionParameterKey, JsonElement> cache = _schemaCaches.GetOrCreateValue(options);
            FunctionParameterKey key = new(type, name, description, hasDefaultValue, defaultValue);

            if (cache.Count > CacheSoftLimit)
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
        catch (ArgumentException)
        {
            // Invalid type; ignore, and leave schema as null.
            // This should be exceedingly rare, as we checked for all known category of
            // problematic types above. If it becomes more common that schema creation
            // could fail expensively, we'll want to track whether inference was already
            // attempted and avoid doing so on subsequent accesses if it was.
            return TrueJsonSchema;
        }
    }

    /// <summary>Infers a JSON schema from the return parameter.</summary>
    /// <param name="type">The type of the return parameter.</param>
    /// <param name="options">The options used to extract the schema from the specified type.</param>
    /// <returns>A <see cref="JsonElement"/> representing the schema.</returns>
    public static JsonElement InferReturnParameterJsonSchema(Type? type, JsonSerializerOptions options)
    {
        _ = Throw.IfNull(options);

        options.MakeReadOnly();

        // If there's no type, just return a schema that allows anything.
        if (type is null)
        {
            return TrueJsonSchema;
        }

        if (type == typeof(void))
        {
            return NullJsonSchema;
        }

        JsonNode node = options.GetJsonSchemaAsNode(type);
        return JsonSerializer.SerializeToElement(node, FunctionCallHelperContext.Default.JsonNode);
    }

    private static JsonElement GetJsonSchemaCore(JsonSerializerOptions options, FunctionParameterKey key)
    {
        _ = Throw.IfNull(options);

        if (options.ReferenceHandler == ReferenceHandler.Preserve)
        {
            throw new NotSupportedException("Schema generation not supported with ReferenceHandler.Preserve enabled.");
        }

        if (key.Type is null)
        {
            // For parameters without a type generate a rudimentary schema with available metadata.

            JsonObject schemaObj = [];
            if (key.Description is not null)
            {
                schemaObj["description"] = key.Description;
            }

            if (key.HasDefaultValue)
            {
                JsonNode? defaultValueNode = key.DefaultValue is { } defaultValue
                    ? JsonSerializer.Serialize(defaultValue, options.GetTypeInfo(defaultValue.GetType()))
                    : null;

                schemaObj["default"] = defaultValueNode;
            }

            return JsonSerializer.SerializeToElement(schemaObj, FunctionCallHelperContext.Default.JsonNode);
        }

        options.MakeReadOnly();

        JsonSchemaExporterOptions exporterOptions = new()
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = TransformSchemaNode,
        };

        JsonNode node = options.GetJsonSchemaAsNode(key.Type, exporterOptions);
        return JsonSerializer.SerializeToElement(node, FunctionCallHelperContext.Default.JsonNode);

        JsonNode TransformSchemaNode(JsonSchemaExporterContext ctx, JsonNode schema)
        {
            const string DescriptionPropertyName = "description";
            const string NotPropertyName = "not";
            const string PropertiesPropertyName = "properties";
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

            // If the type is recursive, the resulting schema will contain a $ref to the type itself.
            // As JSON pointer doesn't support relative paths, we need to fix up such paths to accommodate
            // the fact that they're being nested inside of a higher-level schema.
            if (schema is JsonObject refObj && refObj.TryGetPropertyValue(RefPropertyName, out JsonNode? paramName))
            {
                // Fix up any $ref URIs to match the path from the root document.
                string refUri = paramName!.GetValue<string>();
                Debug.Assert(refUri is "#" || refUri.StartsWith("#/", StringComparison.Ordinal), $"Expected {nameof(refUri)} to be either # or start with #/, got {refUri}");
                refUri = refUri == "#"
                    ? $"#/{PropertiesPropertyName}/{key.ParameterName}"
                    : $"#/{PropertiesPropertyName}/{key.ParameterName}/{refUri.AsMemory("#/".Length)}";

                refObj[RefPropertyName] = (JsonNode)refUri;
            }

            if (ctx.Path.IsEmpty)
            {
                // We are at the root-level schema node, append parameter-specific metadata

                if (!string.IsNullOrWhiteSpace(key.Description))
                {
                    ConvertSchemaToObject(ref schema).Insert(0, DescriptionPropertyName, (JsonNode)key.Description!);
                }

                if (key.HasDefaultValue)
                {
                    JsonNode? defaultValue = JsonSerializer.Serialize(key.DefaultValue, options.GetTypeInfo(typeof(object)));
                    ConvertSchemaToObject(ref schema)[DefaultPropertyName] = defaultValue;
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
    [JsonSerializable(typeof(IDictionary<string, object?>))]
    [JsonSerializable(typeof(JsonNode))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(JsonDocument))]
    private sealed partial class FunctionCallHelperContext : JsonSerializerContext;
}
