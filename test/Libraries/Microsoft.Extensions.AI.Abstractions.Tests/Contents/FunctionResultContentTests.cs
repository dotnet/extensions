// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class FunctionResultContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        FunctionResultContent c = new("callId1", null);
        Assert.Equal("callId1", c.CallId);
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Null(c.Result);
        Assert.Null(c.Outputs);
        Assert.Null(c.Exception);
    }

    [Fact]
    public void Constructor_String_PropsRoundtrip()
    {
        FunctionResultContent c = new("id", "result");
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Equal("id", c.CallId);
        Assert.Equal("result", c.Result);
        Assert.Null(c.Outputs);
        Assert.Null(c.Exception);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        FunctionResultContent c = new("callId1", null);

        Assert.Null(c.RawRepresentation);
        object raw = new();
        c.RawRepresentation = raw;
        Assert.Same(raw, c.RawRepresentation);

        Assert.Null(c.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        c.AdditionalProperties = props;
        Assert.Same(props, c.AdditionalProperties);

        Assert.Equal("callId1", c.CallId);

        Assert.Null(c.Result);
        c.Result = "result";
        Assert.Equal("result", c.Result);

        Assert.Null(c.Outputs);
        IList<AIContent> outputs = [new TextContent("hello")];
        c.Outputs = outputs;
        Assert.Same(outputs, c.Outputs);

        Assert.Null(c.Exception);
        Exception e = new();
        c.Exception = e;
        Assert.Same(e, c.Exception);
    }

    [Fact]
    public void ItShouldBeSerializableAndDeserializable()
    {
        // Arrange
        var sut = new FunctionResultContent("id", "result");

        // Act
        var json = JsonSerializer.Serialize(sut, TestJsonSerializerContext.Default.Options);

        var deserializedSut = JsonSerializer.Deserialize<FunctionResultContent>(json, TestJsonSerializerContext.Default.Options);

        // Assert
        Assert.NotNull(deserializedSut);
        Assert.Equal(sut.CallId, deserializedSut.CallId);
        Assert.Equal(sut.Result, deserializedSut.Result?.ToString());
    }

    [Fact]
    public void ItShouldBeSerializableAndDeserializableWithException()
    {
        // Arrange
        var sut = new FunctionResultContent("callId1", null) { Exception = new InvalidOperationException("hello") };

        // Act
        var json = JsonSerializer.Serialize(sut, TestJsonSerializerContext.Default.Options);
        var deserializedSut = JsonSerializer.Deserialize<FunctionResultContent>(json, TestJsonSerializerContext.Default.Options);

        // Assert
        Assert.NotNull(deserializedSut);
        Assert.Equal(sut.CallId, deserializedSut.CallId);
        Assert.Equal(sut.Result, deserializedSut.Result?.ToString());
        Assert.Null(deserializedSut.Exception);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        var content = new FunctionResultContent("call123", "result");

        AssertSerializationRoundtrips<FunctionResultContent>(content);
        AssertSerializationRoundtrips<ToolResultContent>(content);
        AssertSerializationRoundtrips<AIContent>(content);

        static void AssertSerializationRoundtrips<T>(FunctionResultContent content)
            where T : AIContent
        {
            T contentAsT = (T)(object)content;
            string json = JsonSerializer.Serialize(contentAsT, AIJsonUtilities.DefaultOptions);
            T? deserialized = JsonSerializer.Deserialize<T>(json, AIJsonUtilities.DefaultOptions);
            Assert.NotNull(deserialized);
            var deserializedContent = Assert.IsType<FunctionResultContent>(deserialized);
            Assert.Equal(content.CallId, deserializedContent.CallId);
            Assert.Equal("result", deserializedContent.Result?.ToString());
        }
    }

    [Fact]
    public void Serialization_ResultOnly()
    {
        var content = new FunctionResultContent("call1", "hello world");

        string json = JsonSerializer.Serialize<AIContent>(content, AIJsonUtilities.DefaultOptions);
        var deserialized = Assert.IsType<FunctionResultContent>(JsonSerializer.Deserialize<AIContent>(json, AIJsonUtilities.DefaultOptions));

        Assert.Equal("call1", deserialized.CallId);
        Assert.Equal("hello world", deserialized.Result?.ToString());
        Assert.Null(deserialized.Outputs);
    }

    [Fact]
    public void Serialization_OutputsOnly()
    {
        var content = new FunctionResultContent("call2", null)
        {
            Outputs = [new TextContent("line 1"), new TextContent("line 2")]
        };

        string json = JsonSerializer.Serialize<AIContent>(content, AIJsonUtilities.DefaultOptions);
        var deserialized = Assert.IsType<FunctionResultContent>(JsonSerializer.Deserialize<AIContent>(json, AIJsonUtilities.DefaultOptions));

        Assert.Equal("call2", deserialized.CallId);
        Assert.Null(deserialized.Result);
        Assert.NotNull(deserialized.Outputs);
        Assert.Equal(2, deserialized.Outputs.Count);
        Assert.Equal("line 1", Assert.IsType<TextContent>(deserialized.Outputs[0]).Text);
        Assert.Equal("line 2", Assert.IsType<TextContent>(deserialized.Outputs[1]).Text);
    }

    [Fact]
    public void Serialization_BothResultAndOutputs()
    {
        var content = new FunctionResultContent("call3", "raw result")
        {
            Outputs = [new TextContent("typed output")]
        };

        string json = JsonSerializer.Serialize<AIContent>(content, AIJsonUtilities.DefaultOptions);
        var deserialized = Assert.IsType<FunctionResultContent>(JsonSerializer.Deserialize<AIContent>(json, AIJsonUtilities.DefaultOptions));

        Assert.Equal("call3", deserialized.CallId);
        Assert.Equal("raw result", deserialized.Result?.ToString());
        Assert.NotNull(deserialized.Outputs);
        Assert.Single(deserialized.Outputs);
        Assert.Equal("typed output", Assert.IsType<TextContent>(deserialized.Outputs[0]).Text);
    }

    [Fact]
    public void Serialization_NeitherResultNorOutputs()
    {
        var content = new FunctionResultContent("call4", null);

        string json = JsonSerializer.Serialize<AIContent>(content, AIJsonUtilities.DefaultOptions);
        var deserialized = Assert.IsType<FunctionResultContent>(JsonSerializer.Deserialize<AIContent>(json, AIJsonUtilities.DefaultOptions));

        Assert.Equal("call4", deserialized.CallId);
        Assert.Null(deserialized.Result);
        Assert.Null(deserialized.Outputs);
    }

    [Fact]
    public void JsonDeserialization_KnownPayload()
    {
        const string Json = """
            {
              "$type": "functionResult",
              "callId": "call123",
              "result": "the result",
              "additionalProperties": {
                "key": "val"
              }
            }
            """;

        AIContent? result = JsonSerializer.Deserialize<AIContent>(Json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(result);
        var funcResult = Assert.IsType<FunctionResultContent>(result);
        Assert.Equal("call123", funcResult.CallId);
        Assert.Equal("the result", funcResult.Result?.ToString());
        Assert.Null(funcResult.Outputs);
        Assert.NotNull(funcResult.AdditionalProperties);
        Assert.Equal("val", funcResult.AdditionalProperties["key"]?.ToString());
    }

    [Fact]
    public void JsonDeserialization_WithOutputs()
    {
        const string Json = """
            {
              "$type": "functionResult",
              "callId": "call456",
              "result": "scalar value",
              "outputs": [
                {
                  "$type": "text",
                  "text": "typed output"
                }
              ]
            }
            """;

        var deserialized = Assert.IsType<FunctionResultContent>(
            JsonSerializer.Deserialize<AIContent>(Json, AIJsonUtilities.DefaultOptions));

        Assert.Equal("call456", deserialized.CallId);
        Assert.Equal("scalar value", deserialized.Result?.ToString());
        Assert.NotNull(deserialized.Outputs);
        Assert.Single(deserialized.Outputs);
        Assert.Equal("typed output", Assert.IsType<TextContent>(deserialized.Outputs[0]).Text);
    }
}
