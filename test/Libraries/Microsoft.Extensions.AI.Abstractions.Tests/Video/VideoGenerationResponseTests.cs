// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class VideoGenerationResponseTests
{
    [Fact]
    public void Constructor_Defaults()
    {
        var response = new VideoGenerationResponse();
        Assert.NotNull(response.Contents);
        Assert.Empty(response.Contents);
        Assert.Null(response.RawRepresentation);
        Assert.Null(response.Usage);
    }

    [Fact]
    public void Constructor_WithContents()
    {
        var contents = new List<AIContent> { new DataContent("dGVzdA=="u8.ToArray(), "video/mp4") };
        var response = new VideoGenerationResponse(contents);
        Assert.Same(contents, response.Contents);
    }

    [Fact]
    public void Contents_NullSetter_ReturnsEmptyList()
    {
        var response = new VideoGenerationResponse { Contents = null! };
        Assert.NotNull(response.Contents);
        Assert.Empty(response.Contents);
    }

    [Fact]
    public void RawRepresentation_Roundtrip()
    {
        var raw = new object();
        var response = new VideoGenerationResponse { RawRepresentation = raw };
        Assert.Same(raw, response.RawRepresentation);
    }

    [Fact]
    public void Usage_Roundtrip()
    {
        var usage = new UsageDetails { InputTokenCount = 100, OutputTokenCount = 200 };
        var response = new VideoGenerationResponse { Usage = usage };
        Assert.Same(usage, response.Usage);
        Assert.Equal(100, response.Usage.InputTokenCount);
        Assert.Equal(200, response.Usage.OutputTokenCount);
    }

    [Fact]
    public void JsonSerialization_WithUriContent()
    {
        var response = new VideoGenerationResponse(
            [new UriContent("https://example.com/video.mp4", "video/mp4")]);

        string json = JsonSerializer.Serialize(response, AIJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<VideoGenerationResponse>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.Single(deserialized!.Contents);
    }

    [Fact]
    public void JsonSerialization_EmptyResponse()
    {
        var response = new VideoGenerationResponse();
        string json = JsonSerializer.Serialize(response, AIJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<VideoGenerationResponse>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.Empty(deserialized!.Contents);
    }
}
