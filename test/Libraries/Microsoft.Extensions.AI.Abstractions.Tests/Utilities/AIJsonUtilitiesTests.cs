// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using Microsoft.Extensions.AI.JsonSchemaExporter;
using Xunit;

namespace Microsoft.Extensions.AI;

public static partial class AIJsonUtilitiesTests
{
    [Fact]
    public static void DefaultOptions_HasExpectedConfiguration()
    {
        var options = AIJsonUtilities.DefaultOptions;

        // Must be read-only singleton.
        Assert.NotNull(options);
        Assert.Same(options, AIJsonUtilities.DefaultOptions);
        Assert.True(options.IsReadOnly);

        // Must conform to JsonSerializerDefaults.Web
        Assert.Equal(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy);
        Assert.True(options.PropertyNameCaseInsensitive);
        Assert.Equal(JsonNumberHandling.AllowReadingFromString, options.NumberHandling);

        // Additional settings
        Assert.Equal(JsonIgnoreCondition.WhenWritingNull, options.DefaultIgnoreCondition);
        Assert.True(options.WriteIndented);
        Assert.Same(JavaScriptEncoder.UnsafeRelaxedJsonEscaping, options.Encoder);
    }

    [Theory]
    [InlineData("<script>alert('XSS')</script>", "<script>alert('XSS')</script>")]
    [InlineData("""{"forecast":"sunny", "temperature":"75"}""", """{\"forecast\":\"sunny\", \"temperature\":\"75\"}""")]
    [InlineData("""{"message":"Πάντα ῥεῖ."}""", """{\"message\":\"Πάντα ῥεῖ.\"}""")]
    [InlineData("""{"message":"七転び八起き"}""", """{\"message\":\"七転び八起き\"}""")]
    [InlineData("""☺️🤖🌍𝄞""", """☺️\uD83E\uDD16\uD83C\uDF0D\uD834\uDD1E""")]
    public static void DefaultOptions_UsesExpectedEscaping(string input, string expectedJsonString)
    {
        var options = AIJsonUtilities.DefaultOptions;
        string json = JsonSerializer.Serialize(input, options);
        Assert.Equal($@"""{expectedJsonString}""", json);
    }

    [Fact]
    public static void DefaultOptions_UsesReflectionWhenDefault()
    {
        // Reflection is only turned off in .NET Core test environments.
        bool isDotnetCore = Type.GetType("System.Half") is not null;
        var options = AIJsonUtilities.DefaultOptions;
        Type anonType = new { Name = 42 }.GetType();

        Assert.Equal(!isDotnetCore, JsonSerializer.IsReflectionEnabledByDefault);
        Assert.Equal(JsonSerializer.IsReflectionEnabledByDefault, AIJsonUtilities.DefaultOptions.TryGetTypeInfo(anonType, out _));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void AIJsonSchemaCreateOptions_DefaultInstance_ReturnsExpectedValues(bool useSingleton)
    {
        AIJsonSchemaCreateOptions options = useSingleton ? AIJsonSchemaCreateOptions.Default : new AIJsonSchemaCreateOptions();
        Assert.False(options.IncludeSchemaKeyword);
        Assert.Null(options.TransformSchemaNode);
        Assert.Null(options.TransformOptions);
    }

    [Fact]
    public static void AIJsonSchemaCreateOptions_UsesStructuralEquality()
    {
        AssertEqual(new AIJsonSchemaCreateOptions(), new AIJsonSchemaCreateOptions());

        foreach (PropertyInfo property in typeof(AIJsonSchemaCreateOptions).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            AIJsonSchemaCreateOptions options1 = new AIJsonSchemaCreateOptions();
            AIJsonSchemaCreateOptions options2 = new AIJsonSchemaCreateOptions();
            switch (property.GetValue(AIJsonSchemaCreateOptions.Default))
            {
                case bool booleanFlag:
                    property.SetValue(options1, !booleanFlag);
                    property.SetValue(options2, !booleanFlag);
                    break;

                case null when property.PropertyType == typeof(Func<AIJsonSchemaCreateContext, JsonNode, JsonNode>):
                    Func<AIJsonSchemaCreateContext, JsonNode, JsonNode> transformer = static (context, schema) => (JsonNode)true;
                    property.SetValue(options1, transformer);
                    property.SetValue(options2, transformer);
                    break;

                case null when property.PropertyType == typeof(Func<ParameterInfo, bool>):
                    Func<ParameterInfo, bool> includeParameter = static (parameter) => true;
                    property.SetValue(options1, includeParameter);
                    property.SetValue(options2, includeParameter);
                    break;

                case null when property.PropertyType == typeof(AIJsonSchemaTransformOptions):
                    AIJsonSchemaTransformOptions transformOptions = new AIJsonSchemaTransformOptions { RequireAllProperties = true };
                    property.SetValue(options1, transformOptions);
                    property.SetValue(options2, transformOptions);
                    break;

                default:
                    Assert.Fail($"Unexpected property type: {property.PropertyType}");
                    break;
            }

            AssertEqual(options1, options2);
            AssertNotEqual(AIJsonSchemaCreateOptions.Default, options1);
        }

        static void AssertEqual(AIJsonSchemaCreateOptions x, AIJsonSchemaCreateOptions y)
        {
            Assert.Equal(x.GetHashCode(), y.GetHashCode());
            Assert.Equal(x, x);
            Assert.Equal(y, y);
            Assert.Equal(x, y);
            Assert.Equal(y, x);
        }

        static void AssertNotEqual(AIJsonSchemaCreateOptions x, AIJsonSchemaCreateOptions y)
        {
            Assert.NotEqual(x, y);
            Assert.NotEqual(y, x);
        }
    }

    [Fact]
    public static void CreateJsonSchema_DefaultParameters_GeneratesExpectedJsonSchema()
    {
        JsonElement expected = JsonDocument.Parse("""
            {
                "description": "The type",
                "type": "object",
                "properties": {
                    "Key": {
                        "description": "The parameter",
                        "type": "integer"
                    },
                    "EnumValue": {
                        "type": "string",
                        "enum": ["A", "B"]
                    },
                    "Value": {
                        "type": ["string", "null"],
                        "default": "defaultValue"
                    }
                },
                "required": ["Key", "EnumValue"]
            }
            """).RootElement;

        JsonElement actual = AIJsonUtilities.CreateJsonSchema(typeof(MyPoco), serializerOptions: JsonContext.Default.Options);

        AssertDeepEquals(expected, actual);
    }

    [Fact]
    public static void CreateJsonSchema_OverriddenParameters_GeneratesExpectedJsonSchema()
    {
        JsonElement expected = JsonDocument.Parse("""
            {
                "$schema": "https://json-schema.org/draft/2020-12/schema",
                "description": "alternative description (Default value: null)",
                "type": "object",
                "properties": {
                    "Key": {
                        "description": "The parameter",
                        "type": "integer"
                    },
                    "EnumValue": {
                        "type": "string",
                        "enum": ["A", "B"]
                    },
                    "Value": {
                        "description": "Default value: \"defaultValue\"",
                        "type": ["string", "null"]
                    }
                },
                "required": ["Key", "EnumValue", "Value"],
                "additionalProperties": false
            }
            """).RootElement;

        AIJsonSchemaCreateOptions inferenceOptions = new AIJsonSchemaCreateOptions
        {
            IncludeSchemaKeyword = true,
            TransformOptions = new()
            {
                DisallowAdditionalProperties = true,
                RequireAllProperties = true,
                MoveDefaultKeywordToDescription = true,
            }
        };

        JsonElement actual = AIJsonUtilities.CreateJsonSchema(
            typeof(MyPoco),
            description: "alternative description",
            hasDefaultValue: true,
            defaultValue: null,
            serializerOptions: JsonContext.Default.Options,
            inferenceOptions: inferenceOptions);

        AssertDeepEquals(expected, actual);
    }

    [Fact]
    public static void CreateJsonSchema_UserDefinedTransformer()
    {
        JsonElement expected = JsonDocument.Parse("""
            {
                "description": "The type",
                "type": "object",
                "properties": {
                    "Key": {
                        "$comment": "Contains a DescriptionAttribute declaration with the text 'The parameter'.",
                        "type": "integer"
                    },
                    "EnumValue": {
                        "type": "string",
                        "enum": ["A", "B"]
                    },
                    "Value": {
                        "type": ["string", "null"],
                        "default": "defaultValue"
                    }
                },
                "required": ["Key", "EnumValue"]
            }
            """).RootElement;

        AIJsonSchemaCreateOptions inferenceOptions = new()
        {
            TransformSchemaNode = static (context, schema) =>
            {
                return context.TypeInfo.Type == typeof(int) && context.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute attr
                ? new JsonObject
                {
                    ["$comment"] = $"Contains a DescriptionAttribute declaration with the text '{attr.Description}'.",
                    ["type"] = "integer",
                }
                : schema;
            }
        };

        JsonElement actual = AIJsonUtilities.CreateJsonSchema(typeof(MyPoco), serializerOptions: JsonContext.Default.Options, inferenceOptions: inferenceOptions);

        AssertDeepEquals(expected, actual);
    }

    [Fact]
    public static void CreateJsonSchema_FiltersDisallowedKeywords()
    {
        JsonElement expected = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "Date": {
                        "type": "string"
                    },
                    "TimeSpan": {
                        "$comment": "Represents a System.TimeSpan value.",
                        "type": "string"
                    },
                    "Char" : {
                        "type": "string"
                    }
                }
            }
            """).RootElement;

        JsonElement actual = AIJsonUtilities.CreateJsonSchema(typeof(PocoWithTypesWithOpenAIUnsupportedKeywords), serializerOptions: JsonContext.Default.Options);

        AssertDeepEquals(expected, actual);
    }

    public class PocoWithTypesWithOpenAIUnsupportedKeywords
    {
        // Uses the unsupported "format" keyword
        public DateTimeOffset Date { get; init; }

        // Uses the unsupported "pattern" keyword
        public TimeSpan TimeSpan { get; init; }

        // Uses the unsupported "minLength" and "maxLength" keywords
        public char Char { get; init; }
    }

    [Fact]
    public static void CreateFunctionJsonSchema_ReturnsExpectedValue()
    {
        JsonSerializerOptions options = new(AIJsonUtilities.DefaultOptions);
        AIFunction func = AIFunctionFactory.Create((int x, int y) => x + y, serializerOptions: options);

        Assert.NotNull(func.UnderlyingMethod);

        JsonElement resolvedSchema = AIJsonUtilities.CreateFunctionJsonSchema(func.UnderlyingMethod, title: string.Empty);
        AssertDeepEquals(resolvedSchema, func.JsonSchema);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public static void CreateFunctionJsonSchema_OptionalParameters(bool requireAllProperties)
    {
        string unitJsonSchema = requireAllProperties ? """
            {
                "description": "The unit to calculate the current temperature to (Default value: \u0022celsius\u0022)",
                "type": "string"
            }
            """ :
            """
            {
                "description": "The unit to calculate the current temperature to",
                "type": "string",
                "default": "celsius"
            }
            """;

        string requiredParamsJsonSchema = requireAllProperties ?
            """["city", "unit"]""" :
            """["city"]""";

        JsonElement expected = JsonDocument.Parse($$"""
            {
              "type": "object",
              "properties": {
                "city": {
                  "description": "The city to get the weather for",
                  "type": "string"
                },
                "unit": {{unitJsonSchema}}
              },
              "required": {{requiredParamsJsonSchema}}
            }
            """).RootElement;

        AIJsonSchemaCreateOptions inferenceOptions = new()
        {
            TransformOptions = new()
            {
                RequireAllProperties = requireAllProperties,
                MoveDefaultKeywordToDescription = requireAllProperties,
            }
        };

        AIFunction func = AIFunctionFactory.Create((
            [Description("The city to get the weather for")] string city,
            [Description("The unit to calculate the current temperature to")] string unit = "celsius") => "sunny",
            new AIFunctionFactoryOptions
            {
                Name = "get_weather",
                Description = "Gets the current weather for a current location",
                JsonSchemaCreateOptions = inferenceOptions
            });

        Assert.NotNull(func.UnderlyingMethod);
        AssertDeepEquals(expected, func.JsonSchema);

        JsonElement resolvedSchema = AIJsonUtilities.CreateFunctionJsonSchema(func.UnderlyingMethod, title: string.Empty, description: string.Empty, inferenceOptions: inferenceOptions);
        AssertDeepEquals(expected, resolvedSchema);
    }

    [Fact]
    public static void CreateFunctionJsonSchema_TreatsIntegralTypesAsInteger_EvenWithAllowReadingFromString()
    {
        JsonSerializerOptions options = new(AIJsonUtilities.DefaultOptions) { NumberHandling = JsonNumberHandling.AllowReadingFromString };
        AIFunction func = AIFunctionFactory.Create((int a, int? b, long c, short d, float e, double f, decimal g) => { }, serializerOptions: options);

        JsonElement schemaParameters = func.JsonSchema.GetProperty("properties");
        Assert.NotNull(func.UnderlyingMethod);
        ParameterInfo[] parameters = func.UnderlyingMethod.GetParameters();
#if NET9_0_OR_GREATER
        Assert.Equal(parameters.Length, schemaParameters.GetPropertyCount());
#endif

        int i = 0;
        foreach (JsonProperty property in schemaParameters.EnumerateObject())
        {
            string numericType = Type.GetTypeCode(parameters[i].ParameterType) is TypeCode.Double or TypeCode.Single or TypeCode.Decimal
                ? "number"
                : "integer";

            JsonElement expected = JsonDocument.Parse($$"""
                {
                  "type": "{{numericType}}"
                }
                """).RootElement;

            JsonElement actualSchema = property.Value;
            AssertDeepEquals(expected, actualSchema);
            i++;
        }
    }

    [Description("The type")]
    public record MyPoco([Description("The parameter")] int Key, MyEnumValue EnumValue, string? Value = "defaultValue");

    [JsonConverter(typeof(JsonStringEnumConverter<MyEnumValue>))]
    public enum MyEnumValue
    {
        A = 1,
        B = 2
    }

    [Fact]
    public static void CreateJsonSchema_CanBeBoolean()
    {
        JsonElement schema = AIJsonUtilities.CreateJsonSchema(typeof(object));
        Assert.Equal(JsonValueKind.True, schema.ValueKind);
    }

    [Theory]
    [MemberData(nameof(TestTypes.GetTestDataUsingAllValues), MemberType = typeof(TestTypes))]
    public static void CreateJsonSchema_ValidateWithTestData(ITestData testData)
    {
        // Stress tests the schema generation method using types from the JsonSchemaExporter test battery.

        JsonSerializerOptions options = testData.Options is { } opts
            ? new(opts) { TypeInfoResolver = TestTypes.TestTypesContext.Default }
            : TestTypes.TestTypesContext.Default.Options;

        JsonTypeInfo typeInfo = options.GetTypeInfo(testData.Type);
        AIJsonSchemaCreateOptions? createOptions = typeInfo.Properties.Any(prop => prop.IsExtensionData)
            ? new() { TransformOptions = new() { DisallowAdditionalProperties = false } } // Do not append additionalProperties: false to the schema if the type has extension data.
            : null;

        JsonElement schema = AIJsonUtilities.CreateJsonSchema(testData.Type, serializerOptions: options, inferenceOptions: createOptions);
        JsonNode? schemaAsNode = JsonSerializer.SerializeToNode(schema, options);

        Assert.NotNull(schemaAsNode);
        Assert.Equal(testData.ExpectedJsonSchema.GetValueKind(), schemaAsNode.GetValueKind());

        if (testData.Value is null || testData.WritesNumbersAsStrings)
        {
            // By design, our generated schema does not accept null root values
            // or numbers formatted as strings, so we skip schema validation.
            return;
        }

        JsonNode? serializedValue = JsonSerializer.SerializeToNode(testData.Value, testData.Type, options);
        SchemaTestHelpers.AssertDocumentMatchesSchema(schemaAsNode, serializedValue);
    }

    [Fact]
    public static void CreateJsonSchema_AcceptsOptionsWithoutResolver()
    {
        JsonSerializerOptions options = new() { WriteIndented = true };
        Assert.Null(options.TypeInfoResolver);
        Assert.False(options.IsReadOnly);

        JsonElement schema = AIJsonUtilities.CreateJsonSchema(typeof(AIContent), serializerOptions: options);
        Assert.Equal(JsonValueKind.Object, schema.ValueKind);

        Assert.True(options.IsReadOnly);
        Assert.Same(options.TypeInfoResolver, AIJsonUtilities.DefaultOptions.TypeInfoResolver);
    }

    [Fact]
    public static void CreateJsonSchema_NullableEnum_IncludesTypeKeyword()
    {
        JsonElement expectedSchema = JsonDocument.Parse("""
        {
            "type": ["string", "null"],
            "enum": ["A", "B", null]
        }
        """).RootElement;

        JsonElement schema = AIJsonUtilities.CreateJsonSchema(typeof(MyEnumValue?), serializerOptions: JsonContext.Default.Options);
        AssertDeepEquals(expectedSchema, schema);
    }

    [Fact]
    public static void AddAIContentType_DerivedAIContent()
    {
        JsonSerializerOptions options = new()
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(AIJsonUtilities.DefaultOptions.TypeInfoResolver, JsonContext.Default),
        };

        options.AddAIContentType<DerivedAIContent>("derivativeContent");

        AIContent c = new DerivedAIContent { DerivedValue = 42 };
        string json = JsonSerializer.Serialize(c, options);
        Assert.Equal("""{"$type":"derivativeContent","DerivedValue":42,"AdditionalProperties":null}""", json);

        AIContent? deserialized = JsonSerializer.Deserialize<AIContent>(json, options);
        Assert.IsType<DerivedAIContent>(deserialized);
    }

    [Fact]
    public static void AddAIContentType_ReadOnlyJsonSerializerOptions_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => AIJsonUtilities.DefaultOptions.AddAIContentType<DerivedAIContent>("derivativeContent"));
    }

    [Fact]
    public static void AddAIContentType_NonAIContent_ThrowsArgumentException()
    {
        JsonSerializerOptions options = new();
        Assert.Throws<ArgumentException>("contentType", () => options.AddAIContentType(typeof(int), "discriminator"));
        Assert.Throws<ArgumentException>("contentType", () => options.AddAIContentType(typeof(object), "discriminator"));
        Assert.Throws<ArgumentException>("contentType", () => options.AddAIContentType(typeof(ChatMessage), "discriminator"));
    }

    [Fact]
    public static void AddAIContentType_BuiltInAIContent_ThrowsArgumentException()
    {
        JsonSerializerOptions options = new();
        Assert.Throws<ArgumentException>(() => options.AddAIContentType<AIContent>("discriminator"));
        Assert.Throws<ArgumentException>(() => options.AddAIContentType<TextContent>("discriminator"));
    }

    [Fact]
    public static void AddAIContentType_ConflictingIdentifier_ThrowsInvalidOperationException()
    {
        JsonSerializerOptions options = new();
        options.AddAIContentType<DerivedAIContent>("text");
        options.AddAIContentType<DerivedAIContent>("audio");

        AIContent c = new DerivedAIContent();
        Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(c, options));
    }

    [Fact]
    public static void AddAIContentType_NullArguments_ThrowsArgumentNullException()
    {
        JsonSerializerOptions options = new();
        Assert.Throws<ArgumentNullException>("options", () => ((JsonSerializerOptions)null!).AddAIContentType<DerivedAIContent>("discriminator"));
        Assert.Throws<ArgumentNullException>("options", () => ((JsonSerializerOptions)null!).AddAIContentType(typeof(DerivedAIContent), "discriminator"));
        Assert.Throws<ArgumentNullException>("typeDiscriminatorId", () => options.AddAIContentType<DerivedAIContent>(null!));
        Assert.Throws<ArgumentNullException>("typeDiscriminatorId", () => options.AddAIContentType(typeof(DerivedAIContent), null!));
        Assert.Throws<ArgumentNullException>("contentType", () => options.AddAIContentType(null!, "discriminator"));
    }

    [Fact]
    public static void HashData_Idempotent()
    {
        JsonSerializerOptions customOptions = new()
        {
            TypeInfoResolver = AIJsonUtilities.DefaultOptions.TypeInfoResolver
        };

        foreach (JsonSerializerOptions? options in new[] { AIJsonUtilities.DefaultOptions, null, customOptions })
        {
            string key1 = AIJsonUtilities.HashDataToString(["a", 'b', 42], options);
            string key2 = AIJsonUtilities.HashDataToString(["a", 'b', 42], options);
            string key3 = AIJsonUtilities.HashDataToString([TimeSpan.FromSeconds(1), null, 1.23], options);
            string key4 = AIJsonUtilities.HashDataToString([TimeSpan.FromSeconds(1), null, 1.23], options);
            string key5 = AIJsonUtilities.HashDataToString([new Dictionary<string, object> { ["key1"] = 1, ["key2"] = 2 }], options);
            string key6 = AIJsonUtilities.HashDataToString([new Dictionary<string, object> { ["key2"] = 2, ["key1"] = 1 }], options);

            Assert.Equal(key1, key2);
            Assert.Equal(key3, key4);
            Assert.Equal(key5, key6);
            Assert.NotEqual(key1, key3);
            Assert.NotEqual(key1, key5);
        }
    }

    [Fact]
    public static void HashData_IndentationInvariant()
    {
        JsonSerializerOptions indentOptions = new(AIJsonUtilities.DefaultOptions) { WriteIndented = true };
        JsonSerializerOptions noIndentOptions = new(AIJsonUtilities.DefaultOptions) { WriteIndented = false };

        Dictionary<string, object> dict = new() { ["key1"] = 1, ["key2"] = 2 };
        string key1 = AIJsonUtilities.HashDataToString([dict], indentOptions);
        string key2 = AIJsonUtilities.HashDataToString([dict], noIndentOptions);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public static void CreateFunctionJsonSchema_InvokesIncludeParameterCallbackForEveryParameter()
    {
        Delegate method = (int first, string second, bool third, CancellationToken fourth, DateTime fifth) => { };

        List<string?> names = [];
        JsonElement schema = AIJsonUtilities.CreateFunctionJsonSchema(method.Method, inferenceOptions: new()
        {
            IncludeParameter = p =>
            {
                names.Add(p.Name);
                return p.Name is "first" or "fifth";
            },
        });

        Assert.Equal(["first", "second", "third", "fifth"], names);

        string schemaString = schema.ToString();
        Assert.Contains("first", schemaString);
        Assert.DoesNotContain("second", schemaString);
        Assert.DoesNotContain("third", schemaString);
        Assert.DoesNotContain("fourth", schemaString);
        Assert.Contains("fifth", schemaString);
    }

    [Fact]
    public static void TransformJsonSchema_ConvertBooleanSchemas()
    {
        JsonElement schema = JsonDocument.Parse("""
            {
                "properties" : {
                    "foo": { "type": "string" },
                    "bar": { "properties": { "x": false } },
                    "baz": true
                },
                "required": ["foo"]
            }
            """).RootElement;

        JsonElement expectedSchema = JsonDocument.Parse("""
            {
                "properties" : {
                    "foo": { "type": "string" },
                    "bar": { "properties": { "x": { "not": true } } },
                    "baz": { }
                },
                "required": ["foo"]
            }
            """).RootElement;

        AIJsonSchemaTransformOptions options = new()
        {
            ConvertBooleanSchemas = true,
        };

        JsonElement transformedSchema = AIJsonUtilities.TransformSchema(schema, options);
        AssertDeepEquals(expectedSchema, transformedSchema);
    }

    [Fact]
    public static void TransformJsonSchema_RequireAllProperties()
    {
        JsonElement schema = JsonDocument.Parse("""
            {
                "properties" : {
                    "foo": { "type": "string" },
                    "bar": { "properties": { "x": false } },
                    "baz": true
                },
                "required": ["foo"]
            }
            """).RootElement;

        JsonElement expectedSchema = JsonDocument.Parse("""
            {
                "properties" : {
                    "foo": { "type": "string" },
                    "bar": { "properties": { "x": false }, "required": ["x"] },
                    "baz": true
                },
                "required": ["foo", "bar", "baz"]
            }
            """).RootElement;

        AIJsonSchemaTransformOptions options = new()
        {
            RequireAllProperties = true,
        };

        JsonElement transformedSchema = AIJsonUtilities.TransformSchema(schema, options);
        AssertDeepEquals(expectedSchema, transformedSchema);
    }

    [Fact]
    public static void TransformJsonSchema_DisallowAdditionalProperties()
    {
        JsonElement schema = JsonDocument.Parse("""
            {
                "properties" : {
                    "foo": { "type": "string" },
                    "bar": { "properties": { "x": false } },
                    "baz": true
                },
                "required": ["foo"]
            }
            """).RootElement;

        JsonElement expectedSchema = JsonDocument.Parse("""
            {
                "properties" : {
                    "foo": { "type": "string" },
                    "bar": { "properties": { "x": false }, "additionalProperties": false },
                    "baz": true
                },
                "required": ["foo"],
                "additionalProperties": false
            }
            """).RootElement;

        AIJsonSchemaTransformOptions options = new()
        {
            DisallowAdditionalProperties = true,
        };

        JsonElement transformedSchema = AIJsonUtilities.TransformSchema(schema, options);
        AssertDeepEquals(expectedSchema, transformedSchema);
    }

    [Fact]
    public static void TransformJsonSchema_UseNullableKeyword()
    {
        JsonElement schema = JsonDocument.Parse("""
            {
                "type": ["object","null"],
                "properties" : {
                    "foo": { "type": ["null","string"] },
                    "bar": { "type":"object", "properties": { "x": false } },
                    "baz": { "type" : ["string","array","null"] }
                },
                "required": ["foo"]
            }
            """).RootElement;

        JsonElement expectedSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties" : {
                    "foo": { "type": "string", "nullable": true },
                    "bar": { "type":"object", "properties": { "x": false } },
                    "baz": { "type" : ["string","array","null"] }
                },
                "required": ["foo"],
                "nullable": true
            }
            """).RootElement;

        AIJsonSchemaTransformOptions options = new()
        {
            UseNullableKeyword = true,
        };

        JsonElement transformedSchema = AIJsonUtilities.TransformSchema(schema, options);
        AssertDeepEquals(expectedSchema, transformedSchema);
    }

    [Fact]
    public static void TransformJsonSchema_MoveDefaultKeywordToDescription()
    {
        JsonElement schema = JsonDocument.Parse("""
            {
                "description": "My awesome schema",
                "type": "array",
                "default": [1,2,3]
            }
            """).RootElement;

        JsonElement expectedSchema = JsonDocument.Parse("""
            {
                "description": "My awesome schema (Default value: [1,2,3])",
                "type": "array"
            }
            """).RootElement;

        AIJsonSchemaTransformOptions options = new()
        {
            MoveDefaultKeywordToDescription = true,
        };

        JsonElement transformedSchema = AIJsonUtilities.TransformSchema(schema, options);
        AssertDeepEquals(expectedSchema, transformedSchema);
    }

    [Theory]
    [MemberData(nameof(TestTypes.GetTestDataUsingAllValues), MemberType = typeof(TestTypes))]
    public static void TransformJsonSchema_ValidateWithTestData(ITestData testData)
    {
        // Stress tests the schema generation method using types from the JsonSchemaExporter test battery.

        JsonSerializerOptions options = testData.Options is { } opts
            ? new(opts) { TypeInfoResolver = TestTypes.TestTypesContext.Default }
            : TestTypes.TestTypesContext.Default.Options;

        JsonTypeInfo typeInfo = options.GetTypeInfo(testData.Type);
        AIJsonSchemaCreateOptions? createOptions = typeInfo.Properties.Any(prop => prop.IsExtensionData)
            ? new() { TransformOptions = new() { DisallowAdditionalProperties = false } } // Do not append additionalProperties: false to the schema if the type has extension data.
            : null;

        JsonElement schema = AIJsonUtilities.CreateJsonSchema(testData.Type, serializerOptions: options, inferenceOptions: createOptions);

        int totalSchemaNodes = 0;
        AIJsonSchemaTransformOptions transformOptions = new()
        {
            ConvertBooleanSchemas = true,
            RequireAllProperties = true,
            DisallowAdditionalProperties = true,
            UseNullableKeyword = true,
            TransformSchemaNode = (context, schema) =>
            {
                totalSchemaNodes++;
                var schemaObj = Assert.IsType<JsonObject>(schema);
                schemaObj.Add("myAwesomeKeyword", (JsonNode)42);
                return schemaObj;
            }
        };

        JsonElement transformedSchema = AIJsonUtilities.TransformSchema(schema, transformOptions);
        Assert.True(totalSchemaNodes > 0, "TransformSchema was not invoked.");

        int totalSchemaNodes2 = 0;
        transformOptions = new()
        {
            TransformSchemaNode = (context, schema) =>
            {
                totalSchemaNodes2++;
                var schemaObj = Assert.IsType<JsonObject>(schema);
                Assert.Contains("myAwesomeKeyword", schemaObj);
                if (schemaObj.TryGetPropertyValue("properties", out JsonNode? props))
                {
                    Assert.Contains("required", schemaObj);
                    Assert.Contains("additionalProperties", schemaObj);
                    Assert.Equal(((JsonArray)schemaObj["required"]!).Count, ((JsonObject)props!).Count);
                }

                if (schemaObj.TryGetPropertyValue("type", out JsonNode? type) && type is JsonArray typeArray)
                {
                    Assert.DoesNotContain("null", typeArray);
                }

                return schemaObj;
            }
        };

        AIJsonUtilities.TransformSchema(transformedSchema, transformOptions);
        Assert.Equal(totalSchemaNodes, totalSchemaNodes2);
    }

    [Fact]
    public static void TransformJsonSchema_InvalidOptions_ThrowsArgumentException()
    {
        JsonElement schema = JsonDocument.Parse("{}").RootElement;
        Assert.Throws<ArgumentNullException>(() => AIJsonUtilities.TransformSchema(schema, transformOptions: null!));
        Assert.Throws<ArgumentException>(() => AIJsonUtilities.TransformSchema(schema, transformOptions: new()));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("42")]
    [InlineData("[1,2,3]")]
    [InlineData("""{"properties":{"x": 42 }}""")]
    [InlineData("""{"oneOf":[42]}""")]
    public static void TransformJsonSchema_InvalidInput_ThrowsArgumentException(string invalidSchema)
    {
        JsonElement schema = JsonDocument.Parse(invalidSchema).RootElement;
        AIJsonSchemaTransformOptions transformOptions = new() { ConvertBooleanSchemas = true };
        Assert.Throws<ArgumentException>(() => AIJsonUtilities.TransformSchema(schema, transformOptions));
    }

    private class DerivedAIContent : AIContent
    {
        public int DerivedValue { get; set; }
    }

    [JsonSerializable(typeof(DerivedAIContent))]
    [JsonSerializable(typeof(MyPoco))]
    [JsonSerializable(typeof(PocoWithTypesWithOpenAIUnsupportedKeywords))]
    [JsonSerializable(typeof(MyEnumValue?))]
    private partial class JsonContext : JsonSerializerContext;

    private static bool DeepEquals(JsonElement element1, JsonElement element2)
    {
#if NET9_0_OR_GREATER
        return JsonElement.DeepEquals(element1, element2);
#else
        return JsonNode.DeepEquals(
            JsonSerializer.SerializeToNode(element1, AIJsonUtilities.DefaultOptions),
            JsonSerializer.SerializeToNode(element2, AIJsonUtilities.DefaultOptions));
#endif
    }

    private static void AssertDeepEquals(JsonElement element1, JsonElement element2)
    {
#pragma warning disable SA1118 // Parameter should not span multiple lines
        Assert.True(DeepEquals(element1, element2), $"""
            Elements are not equal.
            Expected:
            {element1}
            Actual:
            {element2}
            """);
#pragma warning restore SA1118 // Parameter should not span multiple lines
    }
}
