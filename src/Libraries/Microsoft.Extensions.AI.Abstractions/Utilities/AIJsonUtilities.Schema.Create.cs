// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Threading;
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
    private const string ItemsPropertyName = "items";
    private const string RequiredPropertyName = "required";
    private const string AdditionalPropertiesPropertyName = "additionalProperties";
    private const string DefaultPropertyName = "default";
    private const string RefPropertyName = "$ref";

    /// <summary>The uri used when populating the $schema keyword in created schemas.</summary>
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
    /// <param name="inferenceOptions">The options controlling schema creation.</param>
    /// <returns>A JSON schema document encoded as a <see cref="JsonElement"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> is <see langword="null"/>.</exception>
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

            if (parameter.ParameterType == typeof(CancellationToken))
            {
                // CancellationToken is a special case that, by convention, we don't want to include in the schema.
                // Invocations of methods that include a CancellationToken argument should also special-case CancellationToken
                // to pass along what relevant token into the method's invocation.
                continue;
            }

            if (inferenceOptions.IncludeParameter is { } includeParameter &&
                !includeParameter(parameter))
            {
                // Skip parameters that should not be included in the schema.
                // By default, all parameters are included.
                continue;
            }

            JsonNode parameterSchema = CreateJsonSchemaCore(
                type: parameter.ParameterType,
                parameterName: parameter.Name,
                description: parameter.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description,
                hasDefaultValue: parameter.HasDefaultValue,
                defaultValue: GetDefaultValueNormalized(parameter),
                serializerOptions,
                inferenceOptions);

            parameterSchemas.Add(parameter.Name, parameterSchema);
            if (!parameter.IsOptional)
            {
                (requiredProperties ??= []).Add((JsonNode)parameter.Name);
            }
        }

        JsonNode schema = new JsonObject();
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

        // Finally, apply any schema transformations if specified.
        if (inferenceOptions.TransformOptions is { } options)
        {
            schema = TransformSchema(schema, options);
        }

        return JsonSerializer.SerializeToElement(schema, JsonContextNoIndentation.Default.JsonNode);
    }

    /// <summary>Creates a JSON schema for the specified type.</summary>
    /// <param name="type">The type for which to generate the schema.</param>
    /// <param name="description">The description of the parameter.</param>
    /// <param name="hasDefaultValue"><see langword="true"/> if the parameter is optional; otherwise, <see langword="false"/>.</param>
    /// <param name="defaultValue">The default value of the optional parameter, if applicable.</param>
    /// <param name="serializerOptions">The options used to extract the schema from the specified type.</param>
    /// <param name="inferenceOptions">The options controlling schema creation.</param>
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

        // Finally, apply any schema transformations if specified.
        if (inferenceOptions.TransformOptions is { } options)
        {
            schema = TransformSchema(schema, options);
        }

        return JsonSerializer.SerializeToElement(schema, JsonContextNoIndentation.Default.JsonNode);
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
        serializerOptions.TypeInfoResolver ??= DefaultOptions.TypeInfoResolver;
        serializerOptions.MakeReadOnly();

        if (type is null)
        {
            // For parameters without a type generate a rudimentary schema with available metadata.

            JsonObject? schemaObj = null;

            if (inferenceOptions.IncludeSchemaKeyword)
            {
                (schemaObj = [])[SchemaPropertyName] = SchemaKeywordUri;
            }

            if (hasDefaultValue)
            {
                JsonNode? defaultValueNode = defaultValue is not null
                    ? JsonSerializer.SerializeToNode(defaultValue, serializerOptions.GetTypeInfo(defaultValue.GetType()))
                    : null;

                (schemaObj ??= [])[DefaultPropertyName] = defaultValueNode;
            }

            if (description is not null)
            {
                (schemaObj ??= [])[DescriptionPropertyName] = description;
            }

            return schemaObj ?? new JsonObject();
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

            string? localDescription = ctx.Path.IsEmpty && description is not null
                ? description
                : ctx.GetCustomAttribute<DescriptionAttribute>()?.Description;

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
                if (ctx.TypeInfo.Type.IsEnum && objSchema.ContainsKey(EnumPropertyName) && !objSchema.ContainsKey(TypePropertyName))
                {
                    objSchema.InsertAtStart(TypePropertyName, "string");
                }

                // Include the type keyword in nullable enum types
                if (Nullable.GetUnderlyingType(ctx.TypeInfo.Type)?.IsEnum is true && objSchema.ContainsKey(EnumPropertyName) && !objSchema.ContainsKey(TypePropertyName))
                {
                    objSchema.InsertAtStart(TypePropertyName, new JsonArray { (JsonNode)"string", (JsonNode)"null" });
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

            if (ctx.Path.IsEmpty && hasDefaultValue)
            {
                JsonNode? defaultValueNode = JsonSerializer.SerializeToNode(defaultValue, ctx.TypeInfo);
                ConvertSchemaToObject(ref schema)[DefaultPropertyName] = defaultValueNode;
            }

            if (localDescription is not null)
            {
                // Insert the final description property at the start of the schema object.
                ConvertSchemaToObject(ref schema).InsertAtStart(DescriptionPropertyName, (JsonNode)localDescription);
            }

            if (ctx.Path.IsEmpty && inferenceOptions.IncludeSchemaKeyword)
            {
                // The $schema property must be the first keyword in the object
                ConvertSchemaToObject(ref schema).InsertAtStart(SchemaPropertyName, (JsonNode)SchemaKeywordUri);
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
        var copiedEntries = System.Linq.Enumerable.ToArray(jsonObject);
        jsonObject.Clear();

        jsonObject.Add(key, value);
        foreach (var entry in copiedEntries)
        {
            jsonObject[entry.Key] = entry.Value;
        }
#endif
    }

    private static JsonElement ParseJsonElement(ReadOnlySpan<byte> utf8Json)
    {
        Utf8JsonReader reader = new(utf8Json);
        return JsonElement.ParseValue(ref reader);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method.",
        Justification = "Called conditionally on structs whose default ctor never gets trimmed.")]
    private static object? GetDefaultValueNormalized(ParameterInfo parameterInfo)
    {
        // Taken from https://github.com/dotnet/runtime/blob/eff415bfd667125c1565680615a6f19152645fbf/src/libraries/System.Text.Json/Common/ReflectionExtensions.cs#L288-L317
        Type parameterType = parameterInfo.ParameterType;
        object? defaultValue = parameterInfo.DefaultValue;

        if (defaultValue is null || (defaultValue == DBNull.Value && parameterType != typeof(DBNull)))
        {
            return parameterType.IsValueType
#if NET
                ? RuntimeHelpers.GetUninitializedObject(parameterType)
#else
                ? System.Runtime.Serialization.FormatterServices.GetUninitializedObject(parameterType)
#endif
                : null;
        }

        // Default values of enums or nullable enums are represented using the underlying type and need to be cast explicitly
        // cf. https://github.com/dotnet/runtime/issues/68647
        if (parameterType.IsEnum)
        {
            return Enum.ToObject(parameterType, defaultValue);
        }

        if (Nullable.GetUnderlyingType(parameterType) is Type underlyingType && underlyingType.IsEnum)
        {
            return Enum.ToObject(underlyingType, defaultValue);
        }

        return defaultValue;
    }
}
