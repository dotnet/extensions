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
        ImageGenerationToolCallContent c = new();
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Null(c.ImageId);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        ImageGenerationToolCallContent c = new();

        Assert.Null(c.ImageId);
        c.ImageId = "img123";
        Assert.Equal("img123", c.ImageId);

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
        ImageGenerationToolCallContent content = new()
        {
            ImageId = "img123"
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedSut = JsonSerializer.Deserialize<ImageGenerationToolCallContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedSut);
        Assert.Equal("img123", deserializedSut.ImageId);
    }

    [Fact]
    public void Serialization_PolymorphicAsAIContent_Roundtrips()
    {
        AIContent content = new ImageGenerationToolCallContent
        {
            ImageId = "img456"
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        Assert.Contains("\"$type\"", json);
        Assert.Contains("\"imageGenerationToolCall\"", json);

        var deserialized = JsonSerializer.Deserialize<AIContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.IsType<ImageGenerationToolCallContent>(deserialized);
        Assert.Equal("img456", ((ImageGenerationToolCallContent)deserialized).ImageId);
    }
}
