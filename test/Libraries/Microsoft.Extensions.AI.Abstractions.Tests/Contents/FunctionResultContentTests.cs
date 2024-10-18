// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class FunctionResultContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        FunctionResultContent c = new("callId1", "functionName", null);
        Assert.Equal("callId1", c.CallId);
        Assert.Equal("functionName", c.Name);
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.ModelId);
        Assert.Null(c.AdditionalProperties);
        Assert.Null(c.Result);
        Assert.Null(c.Exception);
    }

    [Fact]
    public void Constructor_String_PropsRoundtrip()
    {
        Exception e = new();

        FunctionResultContent c = new("id", "name", "result", e);
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.ModelId);
        Assert.Null(c.AdditionalProperties);
        Assert.Equal("name", c.Name);
        Assert.Equal("id", c.CallId);
        Assert.Equal("result", c.Result);
        Assert.Same(e, c.Exception);
    }

    [Fact]
    public void Constructor_FunctionCallContent_PropsRoundtrip()
    {
        Exception e = new();

        FunctionResultContent c = new(new FunctionCallContent("id", "name"), "result", e);
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.ModelId);
        Assert.Null(c.AdditionalProperties);
        Assert.Equal("id", c.CallId);
        Assert.Equal("result", c.Result);
        Assert.Same(e, c.Exception);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        FunctionResultContent c = new("callId1", "functionName", null);

        Assert.Null(c.RawRepresentation);
        object raw = new();
        c.RawRepresentation = raw;
        Assert.Same(raw, c.RawRepresentation);

        Assert.Null(c.ModelId);
        c.ModelId = "modelId";
        Assert.Equal("modelId", c.ModelId);

        Assert.Null(c.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        c.AdditionalProperties = props;
        Assert.Same(props, c.AdditionalProperties);

        Assert.Equal("callId1", c.CallId);
        c.CallId = "id";
        Assert.Equal("id", c.CallId);

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
        var sut = new FunctionResultContent(new FunctionCallContent("id", "p1-f1"), "result");

        // Act
        var json = JsonSerializer.Serialize(sut, TestJsonSerializerContext.Default.Options);

        var deserializedSut = JsonSerializer.Deserialize<FunctionResultContent>(json, TestJsonSerializerContext.Default.Options);

        // Assert
        Assert.NotNull(deserializedSut);
        Assert.Equal(sut.Name, deserializedSut.Name);
        Assert.Equal(sut.CallId, deserializedSut.CallId);
        Assert.Equal(sut.Result, deserializedSut.Result?.ToString());
    }

    [Fact]
    public void ItShouldBeSerializableAndDeserializableWithException()
    {
        // Arrange
        var sut = new FunctionResultContent("callId1", "functionName", null, new InvalidOperationException("hello"));

        // Act
        var json = JsonSerializer.Serialize(sut, TestJsonSerializerContext.Default.Options);
        var deserializedSut = JsonSerializer.Deserialize<FunctionResultContent>(json, TestJsonSerializerContext.Default.Options);

        // Assert
        Assert.NotNull(deserializedSut);
        Assert.Equal(sut.Name, deserializedSut.Name);
        Assert.Equal(sut.CallId, deserializedSut.CallId);
        Assert.Equal(sut.Result, deserializedSut.Result?.ToString());
        Assert.Null(deserializedSut.Exception);
    }
}
