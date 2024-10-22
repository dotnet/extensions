// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.JsonSchemaExporter;
using Xunit;

#pragma warning disable S2197 // Modulus results should not be checked for direct equality

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
        Assert.True(options.IncludeTypeInEnumSchemas);
        Assert.True(options.DisallowAdditionalProperties);
        Assert.False(options.IncludeSchemaKeyword);
        Assert.True(options.RequireAllProperties);
        Assert.True(options.FilterDisallowedKeywords);
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

    [Fact]
    public static void CreateJsonSchema_FilterDisallowedKeywords_Disabled()
    {
        JsonElement expected = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "Date": {
                        "type": "string",
                        "format": "date-time"
                    },
                    "TimeSpan": {
                        "$comment": "Represents a System.TimeSpan value.",
                        "type": "string",
                        "pattern": "^-?(\\d+\\.)?\\d{2}:\\d{2}:\\d{2}(\\.\\d{1,7})?$"
                    },
                    "Char" : {
                        "type": "string",
                        "minLength": 1,
                        "maxLength": 1
                    }
                },
                "required": ["Date","TimeSpan","Char"],
                "additionalProperties": false
            }
            """).RootElement;

        AIJsonSchemaCreateOptions inferenceOptions = new()
        {
            FilterDisallowedKeywords = false
        };

        JsonElement actual = AIJsonUtilities.CreateJsonSchema(
            typeof(PocoWithTypesWithOpenAIUnsupportedKeywords),
            serializerOptions: JsonSerializerOptions.Default,
            inferenceOptions: inferenceOptions);

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
    public static async Task SerializeAsSseAsync_HasExpectedOutput()
    {
        using MemoryStream stream = new();
        await AIJsonUtilities.SerializeAsSseAsync(stream, CreateEvents());
        string output = Encoding.UTF8.GetString(stream.ToArray());

        Assert.Equal("""
            data: {"value":1}

            event: eventType1
            data: {"value":2}

            data: {"value":3}
            id: 3

            event: eventType2
            data: {"value":4}
            id: 4


            """,
            output);

        static async IAsyncEnumerable<SseEvent<SseValue>> CreateEvents()
        {
            yield return new SseEvent<SseValue>(new SseValue(1));
            yield return new SseEvent<SseValue>(new SseValue(2)) { EventType = "eventType1" };
            await Task.CompletedTask;
            yield return new SseEvent<SseValue>(new SseValue(3)) { Id = "3" };
            yield return new SseEvent<SseValue>(new SseValue(4)) { Id = "4", EventType = "eventType2" };
        }
    }

    [Fact]
    public static async Task SerializeAsSseAsync_CanRoundtripValues()
    {
        using MemoryStream stream = new();
        await AIJsonUtilities.SerializeAsSseAsync(stream, CreateEvents(100), cancellationToken: default);
        stream.Position = 0;

        var parser = SseParser.Create(stream, (_, data) => JsonSerializer.Deserialize<SseValue>(data, AIJsonUtilities.DefaultOptions)!);
        string expectedLastEventId = "";
        int i = 0;

        foreach (var parsedEvent in parser.Enumerate())
        {
            Assert.NotNull(parsedEvent.Data);
            Assert.Equal(i, parsedEvent.Data.Value);
            Assert.Equal((i % 7) switch { 3 => "A", 5 => "B", _ => "message" }, parsedEvent.EventType);

            if (i % 10 == 9)
            {
                expectedLastEventId = i.ToString();
            }

            Assert.Equal(expectedLastEventId, parser.LastEventId);
            i++;
        }

        static async IAsyncEnumerable<SseEvent<SseValue>> CreateEvents(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new SseEvent<SseValue>(new SseValue(i))
                {
                    Id = i % 10 == 9 ? i.ToString() : null,
                    EventType = (i % 7) switch { 3 => "A", 5 => "B", _ => null },
                };
            }

            await Task.CompletedTask;
        }
    }

    public record SseValue(int Value);
}
