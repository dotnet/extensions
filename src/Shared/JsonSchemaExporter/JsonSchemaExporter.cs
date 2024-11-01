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

#pragma warning disable LA0002 // Use 'Microsoft.Shared.Text.NumericExtensions.ToInvariantString' for improved performance
#pragma warning disable S107 // Methods should not have too many parameters
#pragma warning disable S1121 // Assignments should not be made from within sub-expressions

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

        if (!ReflectionHelpers.IsBuiltInConverter(effectiveConverter))
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

                Func<JsonPropertyInfo, ParameterInfo?>? parameterInfoMapper =
                    ReflectionHelpers.ResolveJsonConstructorParameterMapper(typeInfo.Type, typeInfo);

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
                    ICustomAttributeProvider? attributeProvider = ReflectionHelpers.ResolveAttributeProvider(typeInfo.Type, property);

                    // Declare the property as nullable if either getter or setter are nullable.
                    bool isNonNullableProperty = false;
                    if (attributeProvider is MemberInfo memberInfo)
                    {
                        NullabilityInfo nullabilityInfo = ReflectionHelpers.GetMemberNullability(state.NullabilityInfoContext, memberInfo);
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
                Type elementType = ReflectionHelpers.GetElementType(typeInfo);
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
                Type valueType = ReflectionHelpers.GetElementType(typeInfo);
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
                if (IsNullableSchema(ref state))
                {
                    schema.MakeNullable();
                }

                bool IsNullableSchema(ref GenerationState state)
                {
                    // A schema is marked as nullable if either
                    // 1. We have a schema for a property where either the getter or setter are marked as nullable.
                    // 2. We have a schema for a reference type, unless we're explicitly treating null-oblivious types as non-nullable

                    if (propertyInfo != null || parameterInfo != null)
                    {
                        return !isNonNullableType;
                    }
                    else
                    {
                        return ReflectionHelpers.CanBeNull(typeInfo.Type) &&
                            !parentPolymorphicTypeIsNonNullable &&
                            !state.ExporterOptions.TreatNullObliviousAsNonNullable;
                    }
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
            if (schemaType is JsonSchemaType.Integer)
            {
                pattern = @"^-?(?:0|[1-9]\d*)$";
            }
            else if (isIeeeFloatingPoint)
            {
                pattern = @"^-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?$";
            }
            else
            {
                pattern = @"^-?(?:0|[1-9]\d*)(?:\.\d+)?$";
            }

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

    private static JsonConverter? ExtractCustomNullableConverter(JsonConverter? converter)
    {
        Debug.Assert(converter is null || ReflectionHelpers.IsBuiltInConverter(converter), "If specified the converter must be built-in.");

        if (converter is null)
        {
            return null;
        }

        return ReflectionHelpers.GetElementConverter(converter);
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
        isNonNullable = ReflectionHelpers.GetParameterNullability(nullabilityInfoContext, parameter) is NullabilityState.NotNull;

        if (parameter.HasDefaultValue)
        {
            // Append the default value to the description.
            object? defaultVal = ReflectionHelpers.GetNormalizedDefaultValue(parameter);
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
        Debug.Assert(typeInfo.Type.IsEnum && ReflectionHelpers.IsBuiltInConverter(converter), "must be using a built-in enum converter.");

        if (converter is JsonConverterFactory factory)
        {
            converter = factory.CreateConverter(typeInfo.Type, typeInfo.Options)!;
        }

        ReflectionHelpers.GetEnumConverterConfig(converter, out JsonNamingPolicy? namingPolicy, out bool allowString);

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
