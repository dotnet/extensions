// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ImageGenerationToolCallContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        ImageGenerationToolCallContent c = new("call123");
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Equal("call123", c.CallId);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        ImageGenerationToolCallContent c = new("img123");

        Assert.Equal("img123", c.CallId);

        Assert.Null(c.RawRepresentation);
        object raw = new();
        c.RawRepresentation = raw;
        Assert.Same(raw, c.RawRepresentation);

        Assert.Null(c.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        c.AdditionalProperties = props;
        Assert.Same(props, c.AdditionalProperties);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        ImageGenerationToolCallContent content = new("img123");

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedSut = JsonSerializer.Deserialize<ImageGenerationToolCallContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedSut);
        Assert.Equal("img123", deserializedSut.CallId);
    }

    [Fact]
    public void Serialization_PolymorphicAsAIContent_Roundtrips()
    {
        AIContent content = new ImageGenerationToolCallContent("img456");

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        Assert.Contains("\"$type\"", json);
        Assert.Contains("\"imageGenerationToolCall\"", json);

        var deserialized = JsonSerializer.Deserialize<AIContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.IsType<ImageGenerationToolCallContent>(deserialized);
        Assert.Equal("img456", ((ImageGenerationToolCallContent)deserialized).CallId);
    }

    [Fact]
    public void JsonDeserialization_KnownPayload()
    {
        const string Json = """
            {
              "$type": "imageGenerationToolCall",
              "callId": "img-call1",
              "additionalProperties": {
                "key": "val"
              }
            }
            """;

        AIContent? result = JsonSerializer.Deserialize<AIContent>(Json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(result);
        var imgCall = Assert.IsType<ImageGenerationToolCallContent>(result);
        Assert.Equal("img-call1", imgCall.CallId);
        Assert.NotNull(imgCall.AdditionalProperties);
        Assert.Equal("val", imgCall.AdditionalProperties["key"]?.ToString());
    }
}
