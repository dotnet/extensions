// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ImageGenerationToolResultContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        ImageGenerationToolResultContent c = new();
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Null(c.ImageId);
        Assert.Null(c.Outputs);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        ImageGenerationToolResultContent c = new();

        Assert.Null(c.ImageId);
        c.ImageId = "img123";
        Assert.Equal("img123", c.ImageId);

        Assert.Null(c.Outputs);
        IList<AIContent> outputs = [new DataContent(new byte[] { 1, 2, 3 }, "image/png")];
        c.Outputs = outputs;
        Assert.Same(outputs, c.Outputs);

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
    public void Outputs_SupportsMultipleContentTypes()
    {
        ImageGenerationToolResultContent c = new()
        {
            ImageId = "img456",
            Outputs =
            [
                new DataContent(new byte[] { 1, 2, 3 }, "image/png"),
                new UriContent("http://example.com/image.jpg", "image/jpeg"),
                new DataContent(new byte[] { 4, 5, 6 }, "image/gif")
            ]
        };

        Assert.NotNull(c.Outputs);
        Assert.Equal(3, c.Outputs.Count);
        Assert.IsType<DataContent>(c.Outputs[0]);
        Assert.IsType<UriContent>(c.Outputs[1]);
        Assert.IsType<DataContent>(c.Outputs[2]);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        ImageGenerationToolResultContent content = new()
        {
            ImageId = "img123",
            Outputs =
            [
                new DataContent(new byte[] { 1, 2, 3 }, "image/png"),
                new UriContent("http://example.com/image.jpg", "image/jpeg")
            ]
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedSut = JsonSerializer.Deserialize<ImageGenerationToolResultContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedSut);
        Assert.Equal("img123", deserializedSut.ImageId);
        Assert.NotNull(deserializedSut.Outputs);
        Assert.Equal(2, deserializedSut.Outputs.Count);
        Assert.IsType<DataContent>(deserializedSut.Outputs[0]);
        Assert.Equal("image/png", ((DataContent)deserializedSut.Outputs[0]).MediaType);
        Assert.IsType<UriContent>(deserializedSut.Outputs[1]);
        Assert.Equal("http://example.com/image.jpg", ((UriContent)deserializedSut.Outputs[1]).Uri.ToString());
    }

    [Fact]
    public void Serialization_PolymorphicAsAIContent_Roundtrips()
    {
        AIContent content = new ImageGenerationToolResultContent
        {
            ImageId = "img789",
            Outputs =
            [
                new DataContent(new byte[] { 7, 8, 9 }, "image/png"),
                new UriContent("http://example.com/another.jpg", "image/jpeg")
            ]
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        Assert.Contains("\"$type\"", json);
        Assert.Contains("\"imageGenerationToolResult\"", json);

        var deserialized = JsonSerializer.Deserialize<AIContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.IsType<ImageGenerationToolResultContent>(deserialized);

        var imageResult = (ImageGenerationToolResultContent)deserialized;
        Assert.Equal("img789", imageResult.ImageId);
        Assert.NotNull(imageResult.Outputs);
        Assert.Equal(2, imageResult.Outputs.Count);
        Assert.IsType<DataContent>(imageResult.Outputs[0]);
        Assert.IsType<UriContent>(imageResult.Outputs[1]);
    }
}
