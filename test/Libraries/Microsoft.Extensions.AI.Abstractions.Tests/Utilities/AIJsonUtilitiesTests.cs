// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
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
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void AIJsonSchemaCreateOptions_DefaultInstance_ReturnsExpectedValues(bool useSingleton)
    {
        AIJsonSchemaCreateOptions options = useSingleton ? AIJsonSchemaCreateOptions.Default : new AIJsonSchemaCreateOptions();
        Assert.False(options.IncludeTypeInEnumSchemas);
        Assert.False(options.DisallowAdditionalProperties);
        Assert.False(options.IncludeSchemaKeyword);
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
                        "enum": ["A", "B"]
                    },
                    "Value": {
                        "type": ["string", "null"],
                        "default": null
                    }
                },
                "required": ["Key", "EnumValue"]
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
                        "type": "string",
                        "enum": ["A", "B"]
                    },
                    "Value": {
                        "type": ["string", "null"],
                        "default": null
                    }
                },
                "required": ["Key", "EnumValue"],
                "additionalProperties": false,
                "default": "42"
            }
            """).RootElement;

        AIJsonSchemaCreateOptions inferenceOptions = new AIJsonSchemaCreateOptions
        {
            IncludeTypeInEnumSchemas = true,
            DisallowAdditionalProperties = true,
            IncludeSchemaKeyword = true
        };

        JsonElement actual = AIJsonUtilities.CreateJsonSchema(typeof(MyPoco),
            description: "alternative description",
            hasDefaultValue: true,
            defaultValue: 42,
            JsonSerializerOptions.Default,
            inferenceOptions);

        Assert.True(JsonElement.DeepEquals(expected, actual));
    }

    [Fact]
    public static void ResolveParameterJsonSchema_ReturnsExpectedValue()
    {
        JsonSerializerOptions options = new(JsonSerializerOptions.Default);
        AIFunction func = AIFunctionFactory.Create((int x, int y) => x + y, serializerOptions: options);

        AIFunctionMetadata metadata = func.Metadata;
        AIFunctionParameterMetadata param = metadata.Parameters[0];
        JsonElement generatedSchema = Assert.IsType<JsonElement>(param.Schema);

        JsonElement resolvedSchema;
        resolvedSchema = AIJsonUtilities.ResolveParameterJsonSchema(param, metadata, options);
        Assert.True(JsonElement.DeepEquals(generatedSchema, resolvedSchema));
    }

    [Fact]
    public static void CreateParameterJsonSchema_TreatsIntegralTypesAsInteger_EvenWithAllowReadingFromString()
    {
        JsonElement expected = JsonDocument.Parse("""
            {
              "type": "integer"
            }
            """).RootElement;

        JsonSerializerOptions options = new(JsonSerializerOptions.Default) { NumberHandling = JsonNumberHandling.AllowReadingFromString };
        AIFunction func = AIFunctionFactory.Create((int a, int? b, long c, short d) => { }, serializerOptions: options);

        AIFunctionMetadata metadata = func.Metadata;
        foreach (var param in metadata.Parameters)
        {
            JsonElement actualSchema = Assert.IsType<JsonElement>(param.Schema);
            Assert.True(JsonElement.DeepEquals(expected, actualSchema));
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

        JsonElement schema = AIJsonUtilities.CreateJsonSchema(testData.Type, serializerOptions: options);
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
}
