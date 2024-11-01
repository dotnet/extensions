// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET9_0_OR_GREATER
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
#if NET
using System.Runtime.InteropServices;
#endif
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable LA0002 // Use 'Microsoft.Shared.Text.NumericExtensions.ToInvariantString' for improved performance
#pragma warning disable S107 // Methods should not have too many parameters
#pragma warning disable S103 // Lines should not be too long
#pragma warning disable S1121 // Assignments should not be made from within sub-expressions
#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S3358 // Ternary operators should not be nested
#pragma warning disable EA0004 // Make type internal since project is executable

namespace System.Text.Json.Schema;

/// <summary>
/// Maps .NET types to JSON schema objects using contract metadata from <see cref="JsonTypeInfo"/> instances.
/// </summary>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal static partial class JsonSchemaExporter
{
    // Polyfill implementation of JsonSchemaExporter for System.Text.Json version 8.0.0.
    // Uses private reflection to access metadata not available with the older APIs of STJ.

    private const string RequiresUnreferencedCodeMessage =
        "Uses private reflection on System.Text.Json components to access converter metadata. " +
        "If running Native AOT ensure that the 'IlcTrimMetadata' property has been disabled.";

    /// <summary>
    /// Generates a JSON schema corresponding to the contract metadata of the specified type.
    /// </summary>
    /// <param name="options">The options instance from which to resolve the contract metadata.</param>
    /// <param name="type">The root type for which to generate the JSON schema.</param>
    /// <param name="exporterOptions">The exporterOptions object controlling the schema generation.</param>
    /// <returns>A new <see cref="JsonNode"/> instance defining the JSON schema for <paramref name="type"/>.</returns>
    /// <exception cref="ArgumentNullException">One of the specified parameters is <see langword="null" />.</exception>
    /// <exception cref="NotSupportedException">The <paramref name="options"/> parameter contains unsupported exporterOptions.</exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static JsonNode GetJsonSchemaAsNode(this JsonSerializerOptions options, Type type, JsonSchemaExporterOptions? exporterOptions = null)
    {
        _ = Throw.IfNull(options);
        _ = Throw.IfNull(type);
        ValidateOptions(options);

        exporterOptions ??= JsonSchemaExporterOptions.Default;
        JsonTypeInfo typeInfo = options.GetTypeInfo(type);
        return MapRootTypeJsonSchema(typeInfo, exporterOptions);
    }

    /// <summary>
    /// Generates a JSON schema corresponding to the specified contract metadata.
    /// </summary>
    /// <param name="typeInfo">The contract metadata for which to generate the schema.</param>
    /// <param name="exporterOptions">The exporterOptions object controlling the schema generation.</param>
    /// <returns>A new <see cref="JsonNode"/> instance defining the JSON schema for <paramref name="typeInfo"/>.</returns>
    /// <exception cref="ArgumentNullException">One of the specified parameters is <see langword="null" />.</exception>
    /// <exception cref="NotSupportedException">The <paramref name="typeInfo"/> parameter contains unsupported exporterOptions.</exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static JsonNode GetJsonSchemaAsNode(this JsonTypeInfo typeInfo, JsonSchemaExporterOptions? exporterOptions = null)
    {
        _ = Throw.IfNull(typeInfo);
        ValidateOptions(typeInfo.Options);

        exporterOptions ??= JsonSchemaExporterOptions.Default;
        return MapRootTypeJsonSchema(typeInfo, exporterOptions);
    }

    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static JsonNode MapRootTypeJsonSchema(JsonTypeInfo typeInfo, JsonSchemaExporterOptions exporterOptions)
    {
        GenerationState state = new(exporterOptions, typeInfo.Options);
        JsonSchema schema = MapJsonSchemaCore(ref state, typeInfo);
        return schema.ToJsonNode(exporterOptions);
    }

    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    private static JsonSchema MapJsonSchemaCore(
        ref GenerationState state,
        JsonTypeInfo typeInfo,
        Type? parentType = null,
        JsonPropertyInfo? propertyInfo = null,
        ICustomAttributeProvider? propertyAttributeProvider = null,
        ParameterInfo? parameterInfo = null,
        bool isNonNullableType = false,
        JsonConverter? customConverter = null,
        JsonNumberHandling? customNumberHandling = null,
        JsonTypeInfo? parentPolymorphicTypeInfo = null,
        bool parentPolymorphicTypeContainsTypesWithoutDiscriminator = false,
        bool parentPolymorphicTypeIsNonNullable = false,
        KeyValuePair<string, JsonSchema>? typeDiscriminator = null,
        bool cacheResult = true)
    {
        Debug.Assert(typeInfo.IsReadOnly, "The specified contract must have been made read-only.");

        JsonSchemaExporterContext exporterContext = state.CreateContext(typeInfo, parentPolymorphicTypeInfo, parentType, propertyInfo, parameterInfo, propertyAttributeProvider);

        if (cacheResult && typeInfo.Kind is not JsonTypeInfoKind.None &&
            state.TryGetExistingJsonPointer(exporterContext, out string? existingJsonPointer))
        {
            // The schema context has already been generated in the schema document, return a reference to it.
            return CompleteSchema(ref state, new JsonSchema { Ref = existingJsonPointer });
        }

        JsonSchema schema;
        JsonConverter effectiveConverter = customConverter ?? typeInfo.Converter;
        JsonNumberHandling effectiveNumberHandling = customNumberHandling ?? typeInfo.NumberHandling ?? typeInfo.Options.NumberHandling;

        if (!IsBuiltInConverter(effectiveConverter))
        {
            // Return a `true` schema for types with user-defined converters.
            return CompleteSchema(ref state, JsonSchema.True);
        }

        if (parentPolymorphicTypeInfo is null && typeInfo.PolymorphismOptions is { DerivedTypes.Count: > 0 } polyOptions)
        {
            // This is the base type of a polymorphic type hierarchy. The schema for this type
            // will include an "anyOf" property with the schemas for all derived types.

            string typeDiscriminatorKey = polyOptions.TypeDiscriminatorPropertyName;
            List<JsonDerivedType> derivedTypes = polyOptions.DerivedTypes.ToList();

            if (!typeInfo.Type.IsAbstract && !derivedTypes.Any(derived => derived.DerivedType == typeInfo.Type))
            {
                // For non-abstract base types that haven't been explicitly configured,
                // add a trivial schema to the derived types since we should support it.
                derivedTypes.Add(new JsonDerivedType(typeInfo.Type));
            }

            bool containsTypesWithoutDiscriminator = derivedTypes.Exists(static derivedTypes => derivedTypes.TypeDiscriminator is null);
            JsonSchemaType schemaType = JsonSchemaType.Any;
            List<JsonSchema>? anyOf = new(derivedTypes.Count);

            state.PushSchemaNode(JsonSchemaConstants.AnyOfPropertyName);

            foreach (JsonDerivedType derivedType in derivedTypes)
            {
                Debug.Assert(derivedType.TypeDiscriminator is null or int or string, "Type discriminator does not have the expected type.");

                KeyValuePair<string, JsonSchema>? derivedTypeDiscriminator = null;
                if (derivedType.TypeDiscriminator is { } discriminatorValue)
                {
                    JsonNode discriminatorNode = discriminatorValue switch
                    {
                        string stringId => (JsonNode)stringId,
                        _ => (JsonNode)(int)discriminatorValue,
                    };

                    JsonSchema discriminatorSchema = new() { Constant = discriminatorNode };
                    derivedTypeDiscriminator = new(typeDiscriminatorKey, discriminatorSchema);
                }

                JsonTypeInfo derivedTypeInfo = typeInfo.Options.GetTypeInfo(derivedType.DerivedType);

                state.PushSchemaNode(anyOf.Count.ToString(CultureInfo.InvariantCulture));
                JsonSchema derivedSchema = MapJsonSchemaCore(
                    ref state,
                    derivedTypeInfo,
                    parentPolymorphicTypeInfo: typeInfo,
                    typeDiscriminator: derivedTypeDiscriminator,
                    parentPolymorphicTypeContainsTypesWithoutDiscriminator: containsTypesWithoutDiscriminator,
                    parentPolymorphicTypeIsNonNullable: isNonNullableType,
                    cacheResult: false);

                state.PopSchemaNode();

                // Determine if all derived schemas have the same type.
                if (anyOf.Count == 0)
                {
                    schemaType = derivedSchema.Type;
                }
                else if (schemaType != derivedSchema.Type)
                {
                    schemaType = JsonSchemaType.Any;
                }

                anyOf.Add(derivedSchema);
            }

            state.PopSchemaNode();

            if (schemaType is not JsonSchemaType.Any)
            {
                // If all derived types have the same schema type, we can simplify the schema
                // by moving the type keyword to the base schema and removing it from the derived schemas.
                foreach (JsonSchema derivedSchema in anyOf)
                {
                    derivedSchema.Type = JsonSchemaType.Any;

                    if (derivedSchema.KeywordCount == 0)
                    {
                        // if removing the type results in an empty schema,
                        // remove the anyOf array entirely since it's always true.
                        anyOf = null;
                        break;
                    }
                }
            }

            schema = new()
            {
                Type = schemaType,
                AnyOf = anyOf,

                // If all derived types have a discriminator, we can require it in the base schema.
                Required = containsTypesWithoutDiscriminator ? null : new() { typeDiscriminatorKey },
            };

            return CompleteSchema(ref state, schema);
        }

        if (Nullable.GetUnderlyingType(typeInfo.Type) is Type nullableElementType)
        {
            JsonTypeInfo elementTypeInfo = typeInfo.Options.GetTypeInfo(nullableElementType);
            customConverter = ExtractCustomNullableConverter(customConverter);
            schema = MapJsonSchemaCore(ref state, elementTypeInfo, customConverter: customConverter, cacheResult: false);

            if (schema.Enum != null)
            {
                Debug.Assert(elementTypeInfo.Type.IsEnum, "The enum keyword should only be populated by schemas for enum types.");
                schema.Enum.Add(null); // Append null to the enum array.
            }

            return CompleteSchema(ref state, schema);
        }

        switch (typeInfo.Kind)
        {
            case JsonTypeInfoKind.Object:
                List<KeyValuePair<string, JsonSchema>>? properties = null;
                List<string>? required = null;
                JsonSchema? additionalProperties = null;

                JsonUnmappedMemberHandling effectiveUnmappedMemberHandling = typeInfo.UnmappedMemberHandling ?? typeInfo.Options.UnmappedMemberHandling;
                if (effectiveUnmappedMemberHandling is JsonUnmappedMemberHandling.Disallow)
                {
                    // Disallow unspecified properties.
                    additionalProperties = JsonSchema.False;
                }

                if (typeDiscriminator is { } typeDiscriminatorPair)
                {
                    (properties = new()).Add(typeDiscriminatorPair);
                    if (parentPolymorphicTypeContainsTypesWithoutDiscriminator)
                    {
                        // Require the discriminator here since it's not common to all derived types.
                        (required = new()).Add(typeDiscriminatorPair.Key);
                    }
                }

                Func<JsonPropertyInfo, ParameterInfo?>? parameterInfoMapper = ResolveJsonConstructorParameterMapper(typeInfo.Type, typeInfo);

                state.PushSchemaNode(JsonSchemaConstants.PropertiesPropertyName);
                foreach (JsonPropertyInfo property in typeInfo.Properties)
                {
                    if (property is { Get: null, Set: null } or { IsExtensionData: true })
                    {
                        continue; // Skip JsonIgnored properties and extension data
                    }

                    JsonNumberHandling? propertyNumberHandling = property.NumberHandling ?? effectiveNumberHandling;
                    JsonTypeInfo propertyTypeInfo = typeInfo.Options.GetTypeInfo(property.PropertyType);

                    // Resolve the attribute provider for the property.
                    ICustomAttributeProvider? attributeProvider = ResolveAttributeProvider(typeInfo.Type, property);

                    // Declare the property as nullable if either getter or setter are nullable.
                    bool isNonNullableProperty = false;
                    if (attributeProvider is MemberInfo memberInfo)
                    {
                        NullabilityInfo nullabilityInfo = state.NullabilityInfoContext.GetMemberNullability(memberInfo);
                        isNonNullableProperty =
                            (property.Get is null || nullabilityInfo.ReadState is NullabilityState.NotNull) &&
                            (property.Set is null || nullabilityInfo.WriteState is NullabilityState.NotNull);
                    }

                    bool isRequired = property.IsRequired;
                    bool hasDefaultValue = false;
                    JsonNode? defaultValue = null;

                    ParameterInfo? associatedParameter = parameterInfoMapper?.Invoke(property);
                    if (associatedParameter != null)
                    {
                        ResolveParameterInfo(
                            associatedParameter,
                            propertyTypeInfo,
                            state.NullabilityInfoContext,
                            out hasDefaultValue,
                            out defaultValue,
                            out bool isNonNullableParameter,
                            ref isRequired);

                        isNonNullableProperty &= isNonNullableParameter;
                    }

                    state.PushSchemaNode(property.Name);
                    JsonSchema propertySchema = MapJsonSchemaCore(
                        ref state,
                        propertyTypeInfo,
                        parentType: typeInfo.Type,
                        propertyInfo: property,
                        parameterInfo: associatedParameter,
                        propertyAttributeProvider: attributeProvider,
                        isNonNullableType: isNonNullableProperty,
                        customConverter: property.CustomConverter,
                        customNumberHandling: propertyNumberHandling);

                    state.PopSchemaNode();

                    if (hasDefaultValue)
                    {
                        JsonSchema.EnsureMutable(ref propertySchema);
                        propertySchema.DefaultValue = defaultValue;
                        propertySchema.HasDefaultValue = true;
                    }

                    (properties ??= new()).Add(new(property.Name, propertySchema));

                    if (isRequired)
                    {
                        (required ??= new()).Add(property.Name);
                    }
                }

                state.PopSchemaNode();
                return CompleteSchema(ref state, new()
                {
                    Type = JsonSchemaType.Object,
                    Properties = properties,
                    Required = required,
                    AdditionalProperties = additionalProperties,
                });

            case JsonTypeInfoKind.Enumerable:
                Type elementType = StjReflectionProxy.GetElementType(typeInfo);
                JsonTypeInfo elementTypeInfo = typeInfo.Options.GetTypeInfo(elementType);

                if (typeDiscriminator is null)
                {
                    state.PushSchemaNode(JsonSchemaConstants.ItemsPropertyName);
                    JsonSchema items = MapJsonSchemaCore(ref state, elementTypeInfo, customNumberHandling: effectiveNumberHandling);
                    state.PopSchemaNode();

                    return CompleteSchema(ref state, new()
                    {
                        Type = JsonSchemaType.Array,
                        Items = items.IsTrue ? null : items,
                    });
                }
                else
                {
                    // Polymorphic enumerable types are represented using a wrapping object:
                    // { "$type" : "discriminator", "$values" : [element1, element2, ...] }
                    // Which corresponds to the schema
                    // { "properties" : { "$type" : { "const" : "discriminator" }, "$values" : { "type" : "array", "items" : { ... } } } }
                    const string ValuesKeyword = "$values";

                    state.PushSchemaNode(JsonSchemaConstants.PropertiesPropertyName);
                    state.PushSchemaNode(ValuesKeyword);
                    state.PushSchemaNode(JsonSchemaConstants.ItemsPropertyName);

                    JsonSchema items = MapJsonSchemaCore(ref state, elementTypeInfo, customNumberHandling: effectiveNumberHandling);

                    state.PopSchemaNode();
                    state.PopSchemaNode();
                    state.PopSchemaNode();

                    return CompleteSchema(ref state, new()
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new()
                        {
                            typeDiscriminator.Value,
                            new(ValuesKeyword,
                                new JsonSchema
                                {
                                    Type = JsonSchemaType.Array,
                                    Items = items.IsTrue ? null : items,
                                }),
                        },
                        Required = parentPolymorphicTypeContainsTypesWithoutDiscriminator ? new() { typeDiscriminator.Value.Key } : null,
                    });
                }

            case JsonTypeInfoKind.Dictionary:
                Type valueType = StjReflectionProxy.GetElementType(typeInfo);
                JsonTypeInfo valueTypeInfo = typeInfo.Options.GetTypeInfo(valueType);

                List<KeyValuePair<string, JsonSchema>>? dictProps = null;
                List<string>? dictRequired = null;

                if (typeDiscriminator is { } dictDiscriminator)
                {
                    dictProps = new() { dictDiscriminator };
                    if (parentPolymorphicTypeContainsTypesWithoutDiscriminator)
                    {
                        // Require the discriminator here since it's not common to all derived types.
                        dictRequired = new() { dictDiscriminator.Key };
                    }
                }

                state.PushSchemaNode(JsonSchemaConstants.AdditionalPropertiesPropertyName);
                JsonSchema valueSchema = MapJsonSchemaCore(ref state, valueTypeInfo, customNumberHandling: effectiveNumberHandling);
                state.PopSchemaNode();

                return CompleteSchema(ref state, new()
                {
                    Type = JsonSchemaType.Object,
                    Properties = dictProps,
                    Required = dictRequired,
                    AdditionalProperties = valueSchema.IsTrue ? null : valueSchema,
                });

            default:
                Debug.Assert(typeInfo.Kind is JsonTypeInfoKind.None, "The default case should handle unrecognize type kinds.");

                if (_simpleTypeSchemaFactories.TryGetValue(typeInfo.Type, out Func<JsonNumberHandling, JsonSchema>? simpleTypeSchemaFactory))
                {
                    schema = simpleTypeSchemaFactory(effectiveNumberHandling);
                }
                else if (typeInfo.Type.IsEnum)
                {
                    schema = GetEnumConverterSchema(typeInfo, effectiveConverter);
                }
                else
                {
                    schema = JsonSchema.True;
                }

                return CompleteSchema(ref state, schema);
        }

        JsonSchema CompleteSchema(ref GenerationState state, JsonSchema schema)
        {
            if (schema.Ref is null)
            {
                // A schema is marked as nullable if either
                // 1. We have a schema for a property where either the getter or setter are marked as nullable.
                // 2. We have a schema for a reference type, unless we're explicitly treating null-oblivious types as non-nullable.
                bool isNullableSchema = (propertyInfo != null || parameterInfo != null)
                    ? !isNonNullableType
                    : CanBeNull(typeInfo.Type) && !parentPolymorphicTypeIsNonNullable && !state.ExporterOptions.TreatNullObliviousAsNonNullable;

                if (isNullableSchema)
                {
                    schema.MakeNullable();
                }
            }

            if (state.ExporterOptions.TransformSchemaNode != null)
            {
                // Prime the schema for invocation by the JsonNode transformer.
                schema.GenerationContext = exporterContext;
            }

            return schema;
        }
    }

    private readonly ref struct GenerationState
    {
        private const int DefaultMaxDepth = 64;
        private readonly List<string> _currentPath = new();
        private readonly Dictionary<(JsonTypeInfo, JsonPropertyInfo?), string[]> _generated = new();
        private readonly int _maxDepth;

        public GenerationState(JsonSchemaExporterOptions exporterOptions, JsonSerializerOptions options, NullabilityInfoContext? nullabilityInfoContext = null)
        {
            ExporterOptions = exporterOptions;
            NullabilityInfoContext = nullabilityInfoContext ?? new();
            _maxDepth = options.MaxDepth is 0 ? DefaultMaxDepth : options.MaxDepth;
        }

        public JsonSchemaExporterOptions ExporterOptions { get; }
        public NullabilityInfoContext NullabilityInfoContext { get; }
        public int CurrentDepth => _currentPath.Count;

        public void PushSchemaNode(string nodeId)
        {
            if (CurrentDepth == _maxDepth)
            {
                ThrowHelpers.ThrowInvalidOperationException_MaxDepthReached();
            }

            _currentPath.Add(nodeId);
        }

        public void PopSchemaNode()
        {
            _currentPath.RemoveAt(_currentPath.Count - 1);
        }

        /// <summary>
        /// Registers the current schema node generation context; if it has already been generated return a JSON pointer to its location.
        /// </summary>
        public bool TryGetExistingJsonPointer(in JsonSchemaExporterContext context, [NotNullWhen(true)] out string? existingJsonPointer)
        {
            (JsonTypeInfo, JsonPropertyInfo?) key = (context.TypeInfo, context.PropertyInfo);
#if NET
            ref string[]? pathToSchema = ref CollectionsMarshal.GetValueRefOrAddDefault(_generated, key, out bool exists);
#else
            bool exists = _generated.TryGetValue(key, out string[]? pathToSchema);
#endif
            if (exists)
            {
                existingJsonPointer = FormatJsonPointer(pathToSchema);
                return true;
            }
#if NET
            pathToSchema = context._path;
#else
            _generated[key] = context._path;
#endif
            existingJsonPointer = null;
            return false;
        }

        public JsonSchemaExporterContext CreateContext(
            JsonTypeInfo typeInfo,
            JsonTypeInfo? baseTypeInfo,
            Type? declaringType,
            JsonPropertyInfo? propertyInfo,
            ParameterInfo? parameterInfo,
            ICustomAttributeProvider? propertyAttributeProvider)
        {
            return new JsonSchemaExporterContext(typeInfo, baseTypeInfo, declaringType, propertyInfo, parameterInfo, propertyAttributeProvider, _currentPath.ToArray());
        }

        private static string FormatJsonPointer(ReadOnlySpan<string> path)
        {
            if (path.IsEmpty)
            {
                return "#";
            }

            StringBuilder sb = new();
            _ = sb.Append('#');

            for (int i = 0; i < path.Length; i++)
            {
                string segment = path[i];
                if (segment.AsSpan().IndexOfAny('~', '/') != -1)
                {
#pragma warning disable CA1307 // Specify StringComparison for clarity
                    segment = segment.Replace("~", "~0").Replace("/", "~1");
#pragma warning restore CA1307
                }

                _ = sb.Append('/');
                _ = sb.Append(segment);
            }

            return sb.ToString();
        }
    }

    private static readonly Dictionary<Type, Func<JsonNumberHandling, JsonSchema>> _simpleTypeSchemaFactories = new()
    {
        [typeof(object)] = _ => JsonSchema.True,
        [typeof(bool)] = _ => new JsonSchema { Type = JsonSchemaType.Boolean },
        [typeof(byte)] = numberHandling => GetSchemaForNumericType(JsonSchemaType.Integer, numberHandling),
        [typeof(ushort)] = numberHandling => GetSchemaForNumericType(JsonSchemaType.Integer, numberHandling),
        [typeof(uint)] = numberHandling => GetSchemaForNumericType(JsonSchemaType.Integer, numberHandling),
        [typeof(ulong)] = numberHandling => GetSchemaForNumericType(JsonSchemaType.Integer, numberHandling),
        [typeof(sbyte)] = numberHandling => GetSchemaForNumericType(JsonSchemaType.Integer, numberHandling),
        [typeof(short)] = numberHandling => GetSchemaForNumericType(JsonSchemaType.Integer, numberHandling),
        [typeof(int)] = numberHandling => GetSchemaForNumericType(JsonSchemaType.Integer, numberHandling),
        [typeof(long)] = numberHandling => GetSchemaForNumericType(JsonSchemaType.Integer, numberHandling),
        [typeof(float)] = numberHandling => GetSchemaForNumericType(JsonSchemaType.Number, numberHandling, isIeeeFloatingPoint: true),
        [typeof(double)] = numberHandling => GetSchemaForNumericType(JsonSchemaType.Number, numberHandling, isIeeeFloatingPoint: true),
        [typeof(decimal)] = numberHandling => GetSchemaForNumericType(JsonSchemaType.Number, numberHandling),
#if NET6_0_OR_GREATER
        [typeof(Half)] = numberHandling => GetSchemaForNumericType(JsonSchemaType.Number, numberHandling, isIeeeFloatingPoint: true),
#endif
#if NET7_0_OR_GREATER
        [typeof(UInt128)] = numberHandling => GetSchemaForNumericType(JsonSchemaType.Integer, numberHandling),
        [typeof(Int128)] = numberHandling => GetSchemaForNumericType(JsonSchemaType.Integer, numberHandling),
#endif
        [typeof(char)] = _ => new JsonSchema { Type = JsonSchemaType.String, MinLength = 1, MaxLength = 1 },
        [typeof(string)] = _ => new JsonSchema { Type = JsonSchemaType.String },
        [typeof(byte[])] = _ => new JsonSchema { Type = JsonSchemaType.String },
        [typeof(Memory<byte>)] = _ => new JsonSchema { Type = JsonSchemaType.String },
        [typeof(ReadOnlyMemory<byte>)] = _ => new JsonSchema { Type = JsonSchemaType.String },
        [typeof(DateTime)] = _ => new JsonSchema { Type = JsonSchemaType.String, Format = "date-time" },
        [typeof(DateTimeOffset)] = _ => new JsonSchema { Type = JsonSchemaType.String, Format = "date-time" },
        [typeof(TimeSpan)] = _ => new JsonSchema
        {
            Comment = "Represents a System.TimeSpan value.",
            Type = JsonSchemaType.String,
            Pattern = @"^-?(\d+\.)?\d{2}:\d{2}:\d{2}(\.\d{1,7})?$",
        },

#if NET6_0_OR_GREATER
        [typeof(DateOnly)] = _ => new JsonSchema { Type = JsonSchemaType.String, Format = "date" },
        [typeof(TimeOnly)] = _ => new JsonSchema { Type = JsonSchemaType.String, Format = "time" },
#endif
        [typeof(Guid)] = _ => new JsonSchema { Type = JsonSchemaType.String, Format = "uuid" },
        [typeof(Uri)] = _ => new JsonSchema { Type = JsonSchemaType.String, Format = "uri" },
        [typeof(Version)] = _ => new JsonSchema
        {
            Comment = "Represents a version string.",
            Type = JsonSchemaType.String,
            Pattern = @"^\d+(\.\d+){1,3}$",
        },

        [typeof(JsonDocument)] = _ => JsonSchema.True,
        [typeof(JsonElement)] = _ => JsonSchema.True,
        [typeof(JsonNode)] = _ => JsonSchema.True,
        [typeof(JsonValue)] = _ => JsonSchema.True,
        [typeof(JsonObject)] = _ => new JsonSchema { Type = JsonSchemaType.Object },
        [typeof(JsonArray)] = _ => new JsonSchema { Type = JsonSchemaType.Array },
    };

    // Adapted from https://github.com/dotnet/runtime/blob/release/9.0/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Converters/Value/JsonPrimitiveConverter.cs#L36-L69
    private static JsonSchema GetSchemaForNumericType(JsonSchemaType schemaType, JsonNumberHandling numberHandling, bool isIeeeFloatingPoint = false)
    {
        Debug.Assert(schemaType is JsonSchemaType.Integer or JsonSchemaType.Number, "schema type must be number or integer");
        Debug.Assert(!isIeeeFloatingPoint || schemaType is JsonSchemaType.Number, "If specifying IEEE the schema type must be number");

        string? pattern = null;

        if ((numberHandling & (JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)) != 0)
        {
            pattern = schemaType is JsonSchemaType.Integer
                ? @"^-?(?:0|[1-9]\d*)$"
                : isIeeeFloatingPoint
                    ? @"^-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?$"
                    : @"^-?(?:0|[1-9]\d*)(?:\.\d+)?$";

            schemaType |= JsonSchemaType.String;
        }

        if (isIeeeFloatingPoint && (numberHandling & JsonNumberHandling.AllowNamedFloatingPointLiterals) != 0)
        {
            return new JsonSchema
            {
                AnyOf = new()
                {
                    new JsonSchema { Type = schemaType, Pattern = pattern },
                    new JsonSchema { Enum = new() { (JsonNode)"NaN", (JsonNode)"Infinity", (JsonNode)"-Infinity" } },
                },
            };
        }

        return new JsonSchema { Type = schemaType, Pattern = pattern };
    }

    // The .NET 8 source generator doesn't populate attribute providers for properties
    // cf. https://github.com/dotnet/runtime/issues/100095
    // Work around the issue by running a query for the relevant MemberInfo using the internal MemberName property
    // https://github.com/dotnet/runtime/blob/de774ff9ee1a2c06663ab35be34b755cd8d29731/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Metadata/JsonPropertyInfo.cs#L206
    private static ICustomAttributeProvider? ResolveAttributeProvider(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        Type? declaringType,
        JsonPropertyInfo? propertyInfo)
    {
        if (declaringType is null || propertyInfo is null)
        {
            return null;
        }

        if (propertyInfo.AttributeProvider is { } provider)
        {
            return provider;
        }

        string? memberName = StjReflectionProxy.GetMemberName(propertyInfo);
        if (memberName is not null)
        {
            const BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            return (MemberInfo?)declaringType.GetProperty(memberName, AllInstance) ??
                                declaringType.GetField(memberName, AllInstance);
        }

        return null;
    }

    private static JsonConverter? ExtractCustomNullableConverter(JsonConverter? converter)
    {
        Debug.Assert(converter is null || IsBuiltInConverter(converter), "If specified the converter must be built-in.");

        if (converter is null)
        {
            return null;
        }

        return StjReflectionProxy.GetElementConverter(converter);
    }

    private static void ValidateOptions(JsonSerializerOptions options)
    {
        if (options.ReferenceHandler == ReferenceHandler.Preserve)
        {
            ThrowHelpers.ThrowNotSupportedException_ReferenceHandlerPreserveNotSupported();
        }

        options.MakeReadOnly();
    }

    private static void ResolveParameterInfo(
        ParameterInfo parameter,
        JsonTypeInfo parameterTypeInfo,
        NullabilityInfoContext nullabilityInfoContext,
        out bool hasDefaultValue,
        out JsonNode? defaultValue,
        out bool isNonNullable,
        ref bool isRequired)
    {
        Debug.Assert(parameterTypeInfo.Type == parameter.ParameterType, "The typeInfo type must match the ParameterInfo type.");

        // Incorporate the nullability information from the parameter.
        isNonNullable = nullabilityInfoContext.GetParameterNullability(parameter) is NullabilityState.NotNull;

        if (parameter.HasDefaultValue)
        {
            // Append the default value to the description.
            object? defaultVal = parameter.GetNormalizedDefaultValue();
            defaultValue = JsonSerializer.SerializeToNode(defaultVal, parameterTypeInfo);
            hasDefaultValue = true;
        }
        else
        {
            // Parameter is not optional, mark as required.
            isRequired = true;
            defaultValue = null;
            hasDefaultValue = false;
        }
    }

    // Adapted from https://github.com/dotnet/runtime/blob/release/9.0/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Converters/Value/EnumConverter.cs#L498-L521
    private static JsonSchema GetEnumConverterSchema(JsonTypeInfo typeInfo, JsonConverter converter)
    {
        Debug.Assert(typeInfo.Type.IsEnum && IsBuiltInConverter(converter), "must be using a built-in enum converter.");

        if (converter is JsonConverterFactory factory)
        {
            converter = factory.CreateConverter(typeInfo.Type, typeInfo.Options)!;
        }

        StjReflectionProxy.GetEnumConverterConfig(converter, out JsonNamingPolicy? namingPolicy, out bool allowString);

        if (allowString)
        {
            // This explicitly ignores the integer component in converters configured as AllowNumbers | AllowStrings
            // which is the default for JsonStringEnumConverter. This sacrifices some precision in the schema for simplicity.

            if (typeInfo.Type.GetCustomAttribute<FlagsAttribute>() is not null)
            {
                // Do not report enum values in case of flags.
                return new() { Type = JsonSchemaType.String };
            }

            JsonArray enumValues = new();
            foreach (string name in Enum.GetNames(typeInfo.Type))
            {
                // This does not account for custom names specified via the new
                // JsonStringEnumMemberNameAttribute introduced in .NET 9.
                string effectiveName = namingPolicy?.ConvertName(name) ?? name;
                enumValues.Add((JsonNode)effectiveName);
            }

            return new() { Enum = enumValues };
        }

        return new() { Type = JsonSchemaType.Integer };
    }

    private static NullabilityState GetParameterNullability(this NullabilityInfoContext context, ParameterInfo parameterInfo)
    {
#if NET8_0
        // Workaround for https://github.com/dotnet/runtime/issues/92487
        // The fix has been incorporated into .NET 9 (and the polyfilled implementations in netfx).
        // Should be removed once .NET 8 support is dropped.
        if (GetGenericParameterDefinition(parameterInfo) is { ParameterType: { IsGenericParameter: true } typeParam })
        {
            // Step 1. Look for nullable annotations on the type parameter.
            if (GetNullableFlags(typeParam) is byte[] flags)
            {
                return TranslateByte(flags[0]);
            }

            // Step 2. Look for nullable annotations on the generic method declaration.
            if (typeParam.DeclaringMethod != null && GetNullableContextFlag(typeParam.DeclaringMethod) is byte flag)
            {
                return TranslateByte(flag);
            }

            // Step 3. Look for nullable annotations on the generic method declaration.
            if (GetNullableContextFlag(typeParam.DeclaringType!) is byte flag2)
            {
                return TranslateByte(flag2);
            }

            // Default to nullable.
            return NullabilityState.Nullable;

            static byte[]? GetNullableFlags(MemberInfo member)
            {
                foreach (CustomAttributeData attr in member.GetCustomAttributesData())
                {
                    Type attrType = attr.AttributeType;
                    if (attrType.Name == "NullableAttribute" && attrType.Namespace == "System.Runtime.CompilerServices")
                    {
                        foreach (CustomAttributeTypedArgument ctorArg in attr.ConstructorArguments)
                        {
                            switch (ctorArg.Value)
                            {
                                case byte flag:
                                    return [flag];
                                case byte[] flags:
                                    return flags;
                            }
                        }
                    }
                }

                return null;
            }

            static byte? GetNullableContextFlag(MemberInfo member)
            {
                foreach (CustomAttributeData attr in member.GetCustomAttributesData())
                {
                    Type attrType = attr.AttributeType;
                    if (attrType.Name == "NullableContextAttribute" && attrType.Namespace == "System.Runtime.CompilerServices")
                    {
                        foreach (CustomAttributeTypedArgument ctorArg in attr.ConstructorArguments)
                        {
                            if (ctorArg.Value is byte flag)
                            {
                                return flag;
                            }
                        }
                    }
                }

                return null;
            }

#pragma warning disable S109 // Magic numbers should not be used
            static NullabilityState TranslateByte(byte b) => b switch
            {
                1 => NullabilityState.NotNull,
                2 => NullabilityState.Nullable,
                _ => NullabilityState.Unknown
            };
#pragma warning restore S109 // Magic numbers should not be used
        }

        static ParameterInfo GetGenericParameterDefinition(ParameterInfo parameter)
        {
            if (parameter.Member is { DeclaringType.IsConstructedGenericType: true }
                                    or MethodInfo { IsGenericMethod: true, IsGenericMethodDefinition: false })
            {
                var genericMethod = (MethodBase)GetGenericMemberDefinition(parameter.Member);
                return genericMethod.GetParameters()[parameter.Position];
            }

            return parameter;
        }

        static MemberInfo GetGenericMemberDefinition(MemberInfo member)
        {
            if (member is Type type)
            {
                return type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;
            }

            if (member.DeclaringType?.IsConstructedGenericType is true)
            {
                return member.DeclaringType.GetGenericTypeDefinition().GetMemberWithSameMetadataDefinitionAs(member);
            }

            if (member is MethodInfo { IsGenericMethod: true, IsGenericMethodDefinition: false } method)
            {
                return method.GetGenericMethodDefinition();
            }

            return member;
        }
#endif
        return context.Create(parameterInfo).WriteState;
    }

    // Taken from https://github.com/dotnet/runtime/blob/903bc019427ca07080530751151ea636168ad334/src/libraries/System.Text.Json/Common/ReflectionExtensions.cs#L288-L317
    private static object? GetNormalizedDefaultValue(this ParameterInfo parameterInfo)
    {
        Type parameterType = parameterInfo.ParameterType;
        object? defaultValue = parameterInfo.DefaultValue;

        if (defaultValue is null)
        {
            return null;
        }

        // DBNull.Value is sometimes used as the default value (returned by reflection) of nullable params in place of null.
        if (defaultValue == DBNull.Value && parameterType != typeof(DBNull))
        {
            return null;
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

    // Resolves the parameters of the deserialization constructor for a type, if they exist.
    private static Func<JsonPropertyInfo, ParameterInfo?>? ResolveJsonConstructorParameterMapper(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        Type type,
        JsonTypeInfo typeInfo)
    {
        Debug.Assert(type == typeInfo.Type, "The declaring type must match the typeInfo type.");
        Debug.Assert(typeInfo.Kind is JsonTypeInfoKind.Object, "Should only be passed object JSON kinds.");

        if (typeInfo.Properties.Count > 0 &&
            typeInfo.CreateObject is null && // Ensure that a default constructor isn't being used
            type.TryGetDeserializationConstructor(useDefaultCtorInAnnotatedStructs: true, out ConstructorInfo? ctor))
        {
            ParameterInfo[]? parameters = ctor?.GetParameters();
            if (parameters?.Length > 0)
            {
                Dictionary<ParameterLookupKey, ParameterInfo> dict = new(parameters.Length);
                foreach (ParameterInfo parameter in parameters)
                {
                    if (parameter.Name is not null)
                    {
                        // We don't care about null parameter names or conflicts since they
                        // would have already been rejected by JsonTypeInfo exporterOptions.
                        dict[new(parameter.Name, parameter.ParameterType)] = parameter;
                    }
                }

                return prop => dict.TryGetValue(new(prop.Name, prop.PropertyType), out ParameterInfo? parameter) ? parameter : null;
            }
        }

        return null;
    }

    // Parameter to property matching semantics as declared in
    // https://github.com/dotnet/runtime/blob/12d96ccfaed98e23c345188ee08f8cfe211c03e7/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Metadata/JsonTypeInfo.cs#L1007-L1030
    private readonly struct ParameterLookupKey : IEquatable<ParameterLookupKey>
    {
        public ParameterLookupKey(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public Type Type { get; }

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
        public bool Equals(ParameterLookupKey other) => Type == other.Type && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        public override bool Equals(object? obj) => obj is ParameterLookupKey key && Equals(key);
    }

    // Resolves the deserialization constructor for a type using logic copied from
    // https://github.com/dotnet/runtime/blob/e12e2fa6cbdd1f4b0c8ad1b1e2d960a480c21703/src/libraries/System.Text.Json/Common/ReflectionExtensions.cs#L227-L286
    private static bool TryGetDeserializationConstructor(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        this Type type,
        bool useDefaultCtorInAnnotatedStructs,
        out ConstructorInfo? deserializationCtor)
    {
        ConstructorInfo? ctorWithAttribute = null;
        ConstructorInfo? publicParameterlessCtor = null;
        ConstructorInfo? lonePublicCtor = null;

        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        if (constructors.Length == 1)
        {
            lonePublicCtor = constructors[0];
        }

        foreach (ConstructorInfo constructor in constructors)
        {
            if (HasJsonConstructorAttribute(constructor))
            {
                if (ctorWithAttribute != null)
                {
                    deserializationCtor = null;
                    return false;
                }

                ctorWithAttribute = constructor;
            }
            else if (constructor.GetParameters().Length == 0)
            {
                publicParameterlessCtor = constructor;
            }
        }

        // Search for non-public ctors with [JsonConstructor].
        foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (HasJsonConstructorAttribute(constructor))
            {
                if (ctorWithAttribute != null)
                {
                    deserializationCtor = null;
                    return false;
                }

                ctorWithAttribute = constructor;
            }
        }

        // Structs will use default constructor if attribute isn't used.
        if (useDefaultCtorInAnnotatedStructs && type.IsValueType && ctorWithAttribute == null)
        {
            deserializationCtor = null;
            return true;
        }

        deserializationCtor = ctorWithAttribute ?? publicParameterlessCtor ?? lonePublicCtor;
        return true;

        static bool HasJsonConstructorAttribute(ConstructorInfo constructorInfo) =>
            constructorInfo.GetCustomAttribute<JsonConstructorAttribute>() != null;
    }

    private static bool IsBuiltInConverter(JsonConverter converter) =>
        converter.GetType().Assembly == typeof(JsonConverter).Assembly;

    // Resolves the nullable reference type annotations for a property or field,
    // additionally addressing a few known bugs of the NullabilityInfo pre .NET 9.
    private static NullabilityInfo GetMemberNullability(this NullabilityInfoContext context, MemberInfo memberInfo)
    {
        Debug.Assert(memberInfo is PropertyInfo or FieldInfo, "Member must be property or field.");
        return memberInfo is PropertyInfo prop
            ? context.Create(prop)
            : context.Create((FieldInfo)memberInfo);
    }

    private static bool CanBeNull(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    private static class StjReflectionProxy
    {
        private const BindingFlags InstanceBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static PropertyInfo? _jsonTypeInfo_ElementType;
        private static PropertyInfo? _jsonPropertyInfo_MemberName;
        private static FieldInfo? _nullableConverter_ElementConverter_Generic;
        private static FieldInfo? _enumConverter_Options_Generic;
        private static FieldInfo? _enumConverter_NamingPolicy_Generic;

        public static Type GetElementType(JsonTypeInfo typeInfo)
        {
            Debug.Assert(typeInfo.Kind is JsonTypeInfoKind.Enumerable or JsonTypeInfoKind.Dictionary, "TypeInfo must be of collection type");

            // Uses reflection to access the element type encapsulated by a JsonTypeInfo.
            if (_jsonTypeInfo_ElementType is null)
            {
                PropertyInfo? elementTypeProperty = typeof(JsonTypeInfo).GetProperty("ElementType", InstanceBindingFlags);
                _jsonTypeInfo_ElementType = Throw.IfNull(elementTypeProperty);
            }

            return (Type)_jsonTypeInfo_ElementType.GetValue(typeInfo)!;
        }

        public static string? GetMemberName(JsonPropertyInfo propertyInfo)
        {
            // Uses reflection to the member name encapsulated by a JsonPropertyInfo.
            if (_jsonPropertyInfo_MemberName is null)
            {
                PropertyInfo? memberName = typeof(JsonPropertyInfo).GetProperty("MemberName", InstanceBindingFlags);
                _jsonPropertyInfo_MemberName = Throw.IfNull(memberName);
            }

            return (string?)_jsonPropertyInfo_MemberName.GetValue(propertyInfo);
        }

        public static JsonConverter GetElementConverter(JsonConverter nullableConverter)
        {
            // Uses reflection to access the element converter encapsulated by a nullable converter.
            if (_nullableConverter_ElementConverter_Generic is null)
            {
                FieldInfo? genericFieldInfo = Type
                    .GetType("System.Text.Json.Serialization.Converters.NullableConverter`1, System.Text.Json")!
                    .GetField("_elementConverter", InstanceBindingFlags);

                _nullableConverter_ElementConverter_Generic = Throw.IfNull(genericFieldInfo);
            }

            Type converterType = nullableConverter.GetType();
            var thisFieldInfo = (FieldInfo)converterType.GetMemberWithSameMetadataDefinitionAs(_nullableConverter_ElementConverter_Generic);
            return (JsonConverter)thisFieldInfo.GetValue(nullableConverter)!;
        }

        public static void GetEnumConverterConfig(JsonConverter enumConverter, out JsonNamingPolicy? namingPolicy, out bool allowString)
        {
            // Uses reflection to access configuration encapsulated by an enum converter.
            if (_enumConverter_Options_Generic is null)
            {
                FieldInfo? genericFieldInfo = Type
                    .GetType("System.Text.Json.Serialization.Converters.EnumConverter`1, System.Text.Json")!
                    .GetField("_converterOptions", InstanceBindingFlags);

                _enumConverter_Options_Generic = Throw.IfNull(genericFieldInfo);
            }

            if (_enumConverter_NamingPolicy_Generic is null)
            {
                FieldInfo? genericFieldInfo = Type
                    .GetType("System.Text.Json.Serialization.Converters.EnumConverter`1, System.Text.Json")!
                    .GetField("_namingPolicy", InstanceBindingFlags);

                _enumConverter_NamingPolicy_Generic = Throw.IfNull(genericFieldInfo);
            }

            const int EnumConverterOptionsAllowStrings = 1;
            Type converterType = enumConverter.GetType();
            var converterOptionsField = (FieldInfo)converterType.GetMemberWithSameMetadataDefinitionAs(_enumConverter_Options_Generic);
            var namingPolicyField = (FieldInfo)converterType.GetMemberWithSameMetadataDefinitionAs(_enumConverter_NamingPolicy_Generic);

            namingPolicy = (JsonNamingPolicy?)namingPolicyField.GetValue(enumConverter);
            int converterOptions = (int)converterOptionsField.GetValue(enumConverter)!;
            allowString = (converterOptions & EnumConverterOptionsAllowStrings) != 0;
        }
    }

#if !NET
    private static MemberInfo GetMemberWithSameMetadataDefinitionAs(this Type specializedType, MemberInfo member)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        return specializedType.GetMember(member.Name, member.MemberType, All).First(m => m.MetadataToken == member.MetadataToken);
    }
#endif

    private static class JsonSchemaConstants
    {
        public const string SchemaPropertyName = "$schema";
        public const string RefPropertyName = "$ref";
        public const string CommentPropertyName = "$comment";
        public const string TitlePropertyName = "title";
        public const string DescriptionPropertyName = "description";
        public const string TypePropertyName = "type";
        public const string FormatPropertyName = "format";
        public const string PatternPropertyName = "pattern";
        public const string PropertiesPropertyName = "properties";
        public const string RequiredPropertyName = "required";
        public const string ItemsPropertyName = "items";
        public const string AdditionalPropertiesPropertyName = "additionalProperties";
        public const string EnumPropertyName = "enum";
        public const string NotPropertyName = "not";
        public const string AnyOfPropertyName = "anyOf";
        public const string ConstPropertyName = "const";
        public const string DefaultPropertyName = "default";
        public const string MinLengthPropertyName = "minLength";
        public const string MaxLengthPropertyName = "maxLength";
    }

    private static class ThrowHelpers
    {
        [DoesNotReturn]
        public static void ThrowInvalidOperationException_MaxDepthReached() =>
            throw new InvalidOperationException("The depth of the generated JSON schema exceeds the JsonSerializerOptions.MaxDepth setting.");

        [DoesNotReturn]
        public static void ThrowNotSupportedException_ReferenceHandlerPreserveNotSupported() =>
            throw new NotSupportedException("Schema generation not supported with ReferenceHandler.Preserve enabled.");
    }
}
#endif
