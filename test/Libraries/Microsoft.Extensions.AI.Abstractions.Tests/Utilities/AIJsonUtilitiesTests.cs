// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.AI.JsonSchemaExporter;
using Xunit;

namespace Microsoft.Extensions.AI;

public static class AIJsonUtilitiesTests
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void AIJsonSchemaCreateOptions_DefaultInstance_ReturnsExpectedValues(bool useSingleton)
    {
        AIJsonSchemaCreateOptions options = useSingleton ? AIJsonSchemaCreateOptions.Default : new AIJsonSchemaCreateOptions();
        Assert.True(options.IncludeTypeInEnumSchemas);
        Assert.True(options.DisallowAdditionalProperties);
        Assert.False(options.IncludeSchemaKeyword);
        Assert.True(options.RequireAllProperties);
        Assert.Null(options.TransformSchemaNode);
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
                        "default": null
                    }
                },
                "required": ["Key", "EnumValue", "Value"],
                "additionalProperties": false
            }
            """).RootElement;

        JsonElement actual = AIJsonUtilities.CreateJsonSchema(typeof(MyPoco), serializerOptions: JsonSerializerOptions.Default);

        Assert.True(JsonElement.DeepEquals(expected, actual));
    }

    [Fact]
    public static void CreateJsonSchema_OverriddenParameters_GeneratesExpectedJsonSchema()
    {
        JsonElement expected = JsonDocument.Parse("""
            {
                "$schema": "https://json-schema.org/draft/2020-12/schema",
                "description": "alternative description",
                "type": "object",
                "properties": {
                    "Key": {
                        "description": "The parameter",
                        "type": "integer"
                    },
                    "EnumValue": {
                        "enum": ["A", "B"]
                    },
                    "Value": {
                        "type": ["string", "null"],
                        "default": null
                    }
                },
                "required": ["Key", "EnumValue"],
                "default": "42"
            }
            """).RootElement;

        AIJsonSchemaCreateOptions inferenceOptions = new AIJsonSchemaCreateOptions
        {
            IncludeTypeInEnumSchemas = false,
            DisallowAdditionalProperties = false,
            IncludeSchemaKeyword = true,
            RequireAllProperties = false,
        };

        JsonElement actual = AIJsonUtilities.CreateJsonSchema(
            typeof(MyPoco),
            description: "alternative description",
            hasDefaultValue: true,
            defaultValue: 42,
            serializerOptions: JsonSerializerOptions.Default,
            inferenceOptions: inferenceOptions);

        Assert.True(JsonElement.DeepEquals(expected, actual));
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
                        "default": null
                    }
                },
                "required": ["Key", "EnumValue", "Value"],
                "additionalProperties": false
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

        JsonElement actual = AIJsonUtilities.CreateJsonSchema(typeof(MyPoco), serializerOptions: JsonSerializerOptions.Default, inferenceOptions: inferenceOptions);

        Assert.True(JsonElement.DeepEquals(expected, actual));
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
                },
                "required": ["Date","TimeSpan","Char"],
                "additionalProperties": false
            }
            """).RootElement;

        JsonElement actual = AIJsonUtilities.CreateJsonSchema(typeof(PocoWithTypesWithOpenAIUnsupportedKeywords), serializerOptions: JsonSerializerOptions.Default);

        Assert.True(JsonElement.DeepEquals(expected, actual));
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
        JsonSerializerOptions options = new(JsonSerializerOptions.Default);
        AIFunction func = AIFunctionFactory.Create((int x, int y) => x + y, serializerOptions: options);

        AIFunctionMetadata metadata = func.Metadata;
        AIFunctionParameterMetadata param = metadata.Parameters[0];

        JsonElement resolvedSchema = AIJsonUtilities.CreateFunctionJsonSchema(title: func.Metadata.Name, description: func.Metadata.Description, parameters: func.Metadata.Parameters);
        Assert.True(JsonElement.DeepEquals(resolvedSchema, func.Metadata.Schema));
    }

    [Fact]
    public static void CreateFunctionJsonSchema_TreatsIntegralTypesAsInteger_EvenWithAllowReadingFromString()
    {
        JsonSerializerOptions options = new(JsonSerializerOptions.Default) { NumberHandling = JsonNumberHandling.AllowReadingFromString };
        AIFunction func = AIFunctionFactory.Create((int a, int? b, long c, short d, float e, double f, decimal g) => { }, serializerOptions: options);

        AIFunctionMetadata metadata = func.Metadata;
        JsonElement schemaParameters = func.Metadata.Schema.GetProperty("properties");
        Assert.Equal(metadata.Parameters.Count, schemaParameters.GetPropertyCount());

        int i = 0;
        foreach (JsonProperty property in schemaParameters.EnumerateObject())
        {
            string numericType = Type.GetTypeCode(metadata.Parameters[i].ParameterType) is TypeCode.Double or TypeCode.Single or TypeCode.Decimal
                ? "number"
                : "integer";

            JsonElement expected = JsonDocument.Parse($$"""
                {
                  "type": "{{numericType}}"
                }
                """).RootElement;

            JsonElement actualSchema = property.Value;
            Assert.True(JsonElement.DeepEquals(expected, actualSchema));
            i++;
        }
    }

    [Description("The type")]
    public record MyPoco([Description("The parameter")] int Key, MyEnumValue EnumValue, string? Value = null);

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
            ? new() { DisallowAdditionalProperties = false } // Do not append additionalProperties: false to the schema if the type has extension data.
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
    public static void AddAIContentType_DerivedAIContent()
    {
        JsonSerializerOptions options = new();
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
        Assert.Throws<ArgumentException>(() => options.AddAIContentType(typeof(int), "discriminator"));
        Assert.Throws<ArgumentException>(() => options.AddAIContentType(typeof(object), "discriminator"));
        Assert.Throws<ArgumentException>(() => options.AddAIContentType(typeof(ChatMessage), "discriminator"));
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
        Assert.Throws<ArgumentNullException>(() => ((JsonSerializerOptions)null!).AddAIContentType<DerivedAIContent>("discriminator"));
        Assert.Throws<ArgumentNullException>(() => ((JsonSerializerOptions)null!).AddAIContentType(typeof(DerivedAIContent), "discriminator"));
        Assert.Throws<ArgumentNullException>(() => options.AddAIContentType<DerivedAIContent>(null!));
        Assert.Throws<ArgumentNullException>(() => options.AddAIContentType(typeof(DerivedAIContent), null!));
        Assert.Throws<ArgumentNullException>(() => options.AddAIContentType(null!, "discriminator"));
    }

    private class DerivedAIContent : AIContent
    {
        public int DerivedValue { get; set; }
    }
}
