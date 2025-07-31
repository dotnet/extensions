﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
#if NET || NETFRAMEWORK
using System.ComponentModel.DataAnnotations;
#endif
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S107 // Methods should not have too many parameters
#pragma warning disable S109 // Magic numbers should not be used
#pragma warning disable S1075 // URIs should not be hardcoded
#pragma warning disable S1121 // Assignments should not be made from within sub-expressions
#pragma warning disable S1199 // Nested block
#pragma warning disable SA1118 // Parameter should not span multiple lines

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
#if NET || NETFRAMEWORK
    private const string FormatPropertyName = "format";
    private const string MinLengthStringPropertyName = "minLength";
    private const string MaxLengthStringPropertyName = "maxLength";
    private const string MinLengthCollectionPropertyName = "minItems";
    private const string MaxLengthCollectionPropertyName = "maxItems";
    private const string MinRangePropertyName = "minimum";
    private const string MaxRangePropertyName = "maximum";
#endif
#if NET
    private const string ContentEncodingPropertyName = "contentEncoding";
    private const string ContentMediaTypePropertyName = "contentMediaType";
    private const string MinExclusiveRangePropertyName = "exclusiveMinimum";
    private const string MaxExclusiveRangePropertyName = "exclusiveMaximum";
#endif

    /// <summary>The uri used when populating the $schema keyword in created schemas.</summary>
    private const string SchemaKeywordUri = "https://json-schema.org/draft/2020-12/schema";

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
                parameter: parameter,
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
        JsonNode schema = CreateJsonSchemaCore(type, parameter: null, description, hasDefaultValue, defaultValue, serializerOptions, inferenceOptions);

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
        ParameterInfo? parameter,
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
                if (parameter?.Name is not null && objSchema.TryGetPropertyValue(RefPropertyName, out JsonNode? paramName))
                {
                    // Fix up any $ref URIs to match the path from the root document.
                    string refUri = paramName!.GetValue<string>();
                    Debug.Assert(refUri is "#" || refUri.StartsWith("#/", StringComparison.Ordinal), $"Expected {nameof(refUri)} to be either # or start with #/, got {refUri}");
                    refUri = refUri == "#"
                        ? $"#/{PropertiesPropertyName}/{parameter.Name}"
                        : $"#/{PropertiesPropertyName}/{parameter.Name}/{refUri.AsMemory("#/".Length)}";

                    objSchema[RefPropertyName] = (JsonNode)refUri;
                }

                // Include the type keyword in enum types
                if (ctx.TypeInfo.Type.IsEnum && objSchema.ContainsKey(EnumPropertyName) && !objSchema.ContainsKey(TypePropertyName))
                {
                    objSchema.InsertAtStart(TypePropertyName, "string");
                }

                // Include a trivial items keyword if missing
                if (ctx.TypeInfo.Kind is JsonTypeInfoKind.Enumerable && !objSchema.ContainsKey(ItemsPropertyName))
                {
                    objSchema.Add(ItemsPropertyName, new JsonObject());
                }

                // Some consumers of the JSON schema, including Ollama as of v0.3.13, don't understand
                // schemas with "type": [...], and only understand "type" being a single value.
                // In certain configurations STJ represents .NET numeric types as ["string", "number"], which will then lead to an error.
                if (TypeIsIntegerWithStringNumberHandling(ctx, objSchema, out string? numericType, out bool isNullable))
                {
                    // We don't want to emit any array for "type". In this case we know it contains "integer" or "number",
                    // so reduce the type to that alone, assuming it's the most specific type.
                    // This makes schemas for Int32 (etc) work with Ollama.
                    JsonObject obj = ConvertSchemaToObject(ref schema);
                    if (isNullable)
                    {
                        // If the type is nullable, we still need use a type array
                        obj[TypePropertyName] = new JsonArray { (JsonNode)numericType, (JsonNode)"null" };
                    }
                    else
                    {
                        obj[TypePropertyName] = (JsonNode)numericType;
                    }

                    _ = obj.Remove(PatternPropertyName);
                }

                if (Nullable.GetUnderlyingType(ctx.TypeInfo.Type) is Type nullableElement)
                {
                    // Account for bug https://github.com/dotnet/runtime/issues/117493
                    // To be removed once System.Text.Json v10 becomes the lowest supported version.
                    // null not inserted in the type keyword for root-level Nullable<T> types.
                    if (objSchema.TryGetPropertyValue(TypePropertyName, out JsonNode? typeKeyWord) &&
                        typeKeyWord?.GetValueKind() is JsonValueKind.String)
                    {
                        string typeValue = typeKeyWord.GetValue<string>()!;
                        if (typeValue is not "null")
                        {
                            objSchema[TypePropertyName] = new JsonArray { (JsonNode)typeValue, (JsonNode)"null" };
                        }
                    }

                    // Include the type keyword in nullable enum types
                    if (nullableElement.IsEnum && objSchema.ContainsKey(EnumPropertyName) && !objSchema.ContainsKey(TypePropertyName))
                    {
                        objSchema.InsertAtStart(TypePropertyName, new JsonArray { (JsonNode)"string", (JsonNode)"null" });
                    }
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

            ApplyDataAnnotations(ref schema, ctx);

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

            void ApplyDataAnnotations(ref JsonNode schema, AIJsonSchemaCreateContext ctx)
            {
                if (ResolveAttribute<DisplayNameAttribute>() is { } displayNameAttribute)
                {
                    ConvertSchemaToObject(ref schema)[TitlePropertyName] ??= displayNameAttribute.DisplayName;
                }

#if NET || NETFRAMEWORK
                if (ResolveAttribute<EmailAddressAttribute>() is { } emailAttribute)
                {
                    ConvertSchemaToObject(ref schema)[FormatPropertyName] ??= "email";
                }

                if (ResolveAttribute<UrlAttribute>() is { } urlAttribute)
                {
                    ConvertSchemaToObject(ref schema)[FormatPropertyName] ??= "uri";
                }

                if (ResolveAttribute<RegularExpressionAttribute>() is { } regexAttribute)
                {
                    ConvertSchemaToObject(ref schema)[PatternPropertyName] ??= regexAttribute.Pattern;
                }

                if (ResolveAttribute<StringLengthAttribute>() is { } stringLengthAttribute)
                {
                    JsonObject obj = ConvertSchemaToObject(ref schema);

                    if (stringLengthAttribute.MinimumLength > 0)
                    {
                        obj[MinLengthStringPropertyName] ??= stringLengthAttribute.MinimumLength;
                    }

                    obj[MaxLengthStringPropertyName] ??= stringLengthAttribute.MaximumLength;
                }

                if (ResolveAttribute<MinLengthAttribute>() is { } minLengthAttribute)
                {
                    JsonObject obj = ConvertSchemaToObject(ref schema);
                    if (obj[TypePropertyName] is JsonNode typeNode && typeNode.GetValueKind() is JsonValueKind.String && typeNode.GetValue<string>() is "string")
                    {
                        obj[MinLengthStringPropertyName] ??= minLengthAttribute.Length;
                    }
                    else
                    {
                        obj[MinLengthCollectionPropertyName] ??= minLengthAttribute.Length;
                    }
                }

                if (ResolveAttribute<MaxLengthAttribute>() is { } maxLengthAttribute)
                {
                    JsonObject obj = ConvertSchemaToObject(ref schema);
                    if (obj[TypePropertyName] is JsonNode typeNode && typeNode.GetValueKind() is JsonValueKind.String && typeNode.GetValue<string>() is "string")
                    {
                        obj[MaxLengthStringPropertyName] ??= maxLengthAttribute.Length;
                    }
                    else
                    {
                        obj[MaxLengthCollectionPropertyName] ??= maxLengthAttribute.Length;
                    }
                }

                if (ResolveAttribute<RangeAttribute>() is { } rangeAttribute)
                {
                    JsonObject obj = ConvertSchemaToObject(ref schema);

                    JsonNode? minNode = null;
                    JsonNode? maxNode = null;
                    switch (rangeAttribute.Minimum)
                    {
                        case int minInt32 when rangeAttribute.Maximum is int maxInt32:
                            maxNode = maxInt32;
                            if (
#if NET
                                !rangeAttribute.MinimumIsExclusive ||
#endif
                                minInt32 > 0)
                            {
                                minNode = minInt32;
                            }

                            break;

                        case double minDouble when rangeAttribute.Maximum is double maxDouble:
                            maxNode = maxDouble;
                            if (
#if NET
                                !rangeAttribute.MinimumIsExclusive ||
#endif
                                minDouble > 0)
                            {
                                minNode = minDouble;
                            }

                            break;

                        case string minString when rangeAttribute.Maximum is string maxString:
                            maxNode = maxString;
                            minNode = minString;
                            break;
                    }

                    if (minNode is not null)
                    {
#if NET
                        if (rangeAttribute.MinimumIsExclusive)
                        {
                            obj[MinExclusiveRangePropertyName] ??= minNode;
                        }
                        else
#endif
                        {
                            obj[MinRangePropertyName] ??= minNode;
                        }
                    }

                    if (maxNode is not null)
                    {
#if NET
                        if (rangeAttribute.MaximumIsExclusive)
                        {
                            obj[MaxExclusiveRangePropertyName] ??= maxNode;
                        }
                        else
#endif
                        {
                            obj[MaxRangePropertyName] ??= maxNode;
                        }
                    }
                }
#endif

#if NET
                if (ResolveAttribute<Base64StringAttribute>() is { } base64Attribute)
                {
                    ConvertSchemaToObject(ref schema)[ContentEncodingPropertyName] ??= "base64";
                }

                if (ResolveAttribute<LengthAttribute>() is { } lengthAttribute)
                {
                    JsonObject obj = ConvertSchemaToObject(ref schema);

                    if (obj[TypePropertyName] is JsonNode typeNode && typeNode.GetValueKind() is JsonValueKind.String && typeNode.GetValue<string>() is "string")
                    {
                        if (lengthAttribute.MinimumLength > 0)
                        {
                            obj[MinLengthStringPropertyName] ??= lengthAttribute.MinimumLength;
                        }

                        obj[MaxLengthStringPropertyName] ??= lengthAttribute.MaximumLength;
                    }
                    else
                    {
                        if (lengthAttribute.MinimumLength > 0)
                        {
                            obj[MinLengthCollectionPropertyName] ??= lengthAttribute.MinimumLength;
                        }

                        obj[MaxLengthCollectionPropertyName] ??= lengthAttribute.MaximumLength;
                    }
                }

                if (ResolveAttribute<AllowedValuesAttribute>() is { } allowedValuesAttribute)
                {
                    JsonObject obj = ConvertSchemaToObject(ref schema);
                    if (!obj.ContainsKey(EnumPropertyName))
                    {
                        if (CreateJsonArray(allowedValuesAttribute.Values, serializerOptions) is { Count: > 0 } enumArray)
                        {
                            obj[EnumPropertyName] = enumArray;
                        }
                    }
                }

                if (ResolveAttribute<DeniedValuesAttribute>() is { } deniedValuesAttribute)
                {
                    JsonObject obj = ConvertSchemaToObject(ref schema);

                    JsonNode? notNode = obj[NotPropertyName];
                    if (notNode is null or JsonObject)
                    {
                        JsonObject notObj =
                            notNode as JsonObject ??
                            (JsonObject)(obj[NotPropertyName] = new JsonObject());

                        if (notObj[EnumPropertyName] is null)
                        {
                            if (CreateJsonArray(deniedValuesAttribute.Values, serializerOptions) is { Count: > 0 } enumArray)
                            {
                                notObj[EnumPropertyName] = enumArray;
                            }
                        }
                    }
                }

                static JsonArray CreateJsonArray(object?[] values, JsonSerializerOptions serializerOptions)
                {
                    JsonArray enumArray = new();
                    foreach (object? allowedValue in values)
                    {
                        if (allowedValue is not null && JsonSerializer.SerializeToNode(allowedValue, serializerOptions.GetTypeInfo(allowedValue.GetType())) is { } valueNode)
                        {
                            enumArray.Add(valueNode);
                        }
                    }

                    return enumArray;
                }

                if (ResolveAttribute<DataTypeAttribute>() is { } dataTypeAttribute)
                {
                    JsonObject obj = ConvertSchemaToObject(ref schema);
                    switch (dataTypeAttribute.DataType)
                    {
                        case DataType.DateTime:
                            obj[FormatPropertyName] ??= "date-time";
                            break;

                        case DataType.Date:
                            obj[FormatPropertyName] ??= "date";
                            break;

                        case DataType.Time:
                            obj[FormatPropertyName] ??= "time";
                            break;

                        case DataType.EmailAddress:
                            obj[FormatPropertyName] ??= "email";
                            break;

                        case DataType.Url:
                            obj[FormatPropertyName] ??= "uri";
                            break;

                        case DataType.ImageUrl:
                            obj[FormatPropertyName] ??= "uri";
                            obj[ContentMediaTypePropertyName] ??= "image/*";
                            break;
                    }
                }
#endif
                TAttribute? ResolveAttribute<TAttribute>()
                    where TAttribute : Attribute
                {
                    // If this is the root schema, check for any parameter attributes first.
                    if (ctx.Path.IsEmpty && parameter?.GetCustomAttribute<TAttribute>(inherit: true) is TAttribute attr)
                    {
                        return attr;
                    }

                    return ctx.GetCustomAttribute<TAttribute>(inherit: true);
                }
            }
        }
    }

    private static bool TypeIsIntegerWithStringNumberHandling(AIJsonSchemaCreateContext ctx, JsonObject schema, [NotNullWhen(true)] out string? numericType, out bool isNullable)
    {
        numericType = null;
        isNullable = false;

        if (ctx.TypeInfo.NumberHandling is not JsonNumberHandling.Strict && schema["type"] is JsonArray typeArray)
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
                            if (numericType is not null)
                            {
                                // Conflicting numeric type
                                return false;
                            }

                            numericType = type;
                            break;
                        case "string":
                            allowString = true;
                            break;
                        case "null":
                            isNullable = true;
                            break;
                        default:
                            // keyword is not valid in the context of numeric types.
                            return false;
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
            return parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) is null
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
