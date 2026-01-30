// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
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
    public void Serialization_DerivedTypes_Roundtrips()
    {
        FunctionResultContent[] contents =
        [
            new FunctionResultContent("call1", "result1"),
            new McpServerToolResultContent("call2"),
        ];

        // Verify each element roundtrips individually
        foreach (var content in contents)
        {
            var serialized = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
            var deserialized = JsonSerializer.Deserialize<FunctionResultContent>(serialized, AIJsonUtilities.DefaultOptions);
            Assert.NotNull(deserialized);
            Assert.Equal(content.GetType(), deserialized.GetType());
        }

        // Verify the array roundtrips
        // Note: Change back to TestJsonSerializerContext.Default.FunctionResultContentArray once McpServerToolResultContent is no longer [Experimental]
        // We need to create new options with reflection support for the array type since TestJsonSerializerContext can't include
        // FunctionResultContent[] without also referencing the [Experimental] McpServerToolResultContent type.
        var optionsWithArraySupport = new JsonSerializerOptions(AIJsonUtilities.DefaultOptions);
        optionsWithArraySupport.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());
        var serializedContents = JsonSerializer.Serialize(contents, optionsWithArraySupport);
        var deserializedContents = JsonSerializer.Deserialize<FunctionResultContent[]>(serializedContents, optionsWithArraySupport);
        Assert.NotNull(deserializedContents);
        Assert.Equal(contents.Length, deserializedContents.Length);
        for (int i = 0; i < deserializedContents.Length; i++)
        {
            Assert.NotNull(deserializedContents[i]);
            Assert.Equal(contents[i].GetType(), deserializedContents[i].GetType());
        }
    }
}
