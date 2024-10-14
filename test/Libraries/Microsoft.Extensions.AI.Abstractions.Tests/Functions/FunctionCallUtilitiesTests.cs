// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Microsoft.Extensions.AI.Functions;

public static class FunctionCallUtilitiesTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData(" \t\n", "___")]
    [InlineData("MethodName42", "MethodName42")]
    [InlineData("MethodName`3", "MethodName_3")]
    [InlineData("<<Main>$>g__Test", "__Main___g__Test")]
    public static void SanitizeMemberName_ReturnsExpectedValue(string name, string expected)
    {
        string actual = JsonFunctionCallUtilities.SanitizeMemberName(name);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void SanitizeMemberName_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => JsonFunctionCallUtilities.SanitizeMemberName(null!));
    }

    [Fact]
    public static void ParseFunctionCallArguments_NullJsonInput_ReturnsNullDictionary()
    {
        var result = JsonFunctionCallUtilities.ParseFunctionCallArguments("null", out Exception? parsingException);
        Assert.Null(parsingException);
        Assert.Null(result);
    }

    [Fact]
    public static void ParseFunctionCallArguments_ObjectJsonInput_ReturnsElementDictionary()
    {
        var result = JsonFunctionCallUtilities.ParseFunctionCallArguments("""{"Key1":{}, "Key2":null, "Key3" : [], "Key4" : 42, "Key5" : true }""", out Exception? parsingException);
        Assert.Null(parsingException);
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        Assert.Collection(result,
            kvp =>
            {
                Assert.Equal("Key1", kvp.Key);
                Assert.True(kvp.Value is JsonElement { ValueKind: JsonValueKind.Object });
            },
            kvp =>
            {
                Assert.Equal("Key2", kvp.Key);
                Assert.Null(kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("Key3", kvp.Key);
                Assert.True(kvp.Value is JsonElement { ValueKind: JsonValueKind.Array });
            },
            kvp =>
            {
                Assert.Equal("Key4", kvp.Key);
                Assert.True(kvp.Value is JsonElement { ValueKind: JsonValueKind.Number });
            },
            kvp =>
            {
                Assert.Equal("Key5", kvp.Key);
                Assert.True(kvp.Value is JsonElement { ValueKind: JsonValueKind.True });
            });
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("false")]
    [InlineData("[]")]
    public static void ParseFunctionCallArguments_InvalidJsonInput_ReturnsParsingException(string json)
    {
        var result = JsonFunctionCallUtilities.ParseFunctionCallArguments(json!, out Exception? parsingException);
        Assert.Null(result);
        Assert.IsType<InvalidOperationException>(parsingException);
        Assert.IsType<JsonException>(parsingException.InnerException);
    }

    [Fact]
    public static void ParseFunctionCallArguments_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => JsonFunctionCallUtilities.ParseFunctionCallArguments((string)null!, out Exception? parsingException));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void JsonSchemaInferenceOptions_DefaultInstance_ReturnsExpectedValues(bool useSingleton)
    {
        JsonSchemaInferenceOptions options = useSingleton ? JsonSchemaInferenceOptions.Default : new JsonSchemaInferenceOptions();
        Assert.False(options.IncludeTypeInEnumSchemas);
        Assert.False(options.DisallowAdditionalProperties);
        Assert.False(options.IncludeSchemaKeyword);
    }

    [Fact]
    public static void InferJsonSchema_DefaultParameters_GeneratesExpectedJsonSchema()
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

        JsonElement actual = JsonFunctionCallUtilities.InferJsonSchema(typeof(MyPoco), JsonSerializerOptions.Default);
        Assert.True(JsonElement.DeepEquals(expected, actual));
    }

    [Fact]
    public static void InferJsonSchema_OverriddenParameters_GeneratesExpectedJsonSchema()
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

        JsonSchemaInferenceOptions inferenceOptions = new JsonSchemaInferenceOptions
        {
            IncludeTypeInEnumSchemas = true,
            DisallowAdditionalProperties = true,
            IncludeSchemaKeyword = true
        };

        JsonElement actual = JsonFunctionCallUtilities.InferJsonSchema(typeof(MyPoco), JsonSerializerOptions.Default,
            description: "alternative description",
            hasDefaultValue: true,
            defaultValue: 42,
            inferenceOptions);

        Assert.True(JsonElement.DeepEquals(expected, actual));
    }

    [Description("The type")]
    public record MyPoco([Description("The parameter")] int Key, MyEnumValue EnumValue, string? Value = null);

    [JsonConverter(typeof(JsonStringEnumConverter<MyEnumValue>))]
    public enum MyEnumValue
    {
        A = 1,
        B = 2
    }
}
