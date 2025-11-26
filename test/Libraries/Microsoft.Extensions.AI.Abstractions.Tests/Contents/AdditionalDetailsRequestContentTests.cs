// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI.Contents;

public class AdditionalDetailsRequestContentTests
{
    [Fact]
    public void Constructor_InvalidArguments_Throws()
    {
        Assert.Throws<ArgumentNullException>("id", () => new AdditionalDetailsRequestContent(null!, new TextContent("request")));
        Assert.Throws<ArgumentException>("id", () => new AdditionalDetailsRequestContent("", new TextContent("request")));
        Assert.Throws<ArgumentException>("id", () => new AdditionalDetailsRequestContent("\r\t\n ", new TextContent("request")));

        Assert.Throws<ArgumentNullException>("request", () => new AdditionalDetailsRequestContent("id", null!));
    }

    [Fact]
    public void Constructor_Roundtrips()
    {
        string id = "abc";
        TextContent request = new("What is your name?");
        AdditionalDetailsRequestContent content = new(id, request);

        Assert.Same(id, content.Id);
        Assert.Same(request, content.Request);
    }

    [Fact]
    public void CreateResponse_ReturnsExpectedResponse()
    {
        string id = "req-1";
        string request = "What is your name?";
        TextContent response = new TextContent("My name is John");

        AdditionalDetailsRequestContent content = new(id, new TextContent(request));

        var textResponse = content.CreateResponse(response);

        Assert.NotNull(textResponse);
        Assert.Same(id, textResponse.Id);
        Assert.Same(response, textResponse.Response);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        var content = new AdditionalDetailsRequestContent("request123", new TextContent("What is your name?"));

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedContent = JsonSerializer.Deserialize<AdditionalDetailsRequestContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedContent);
        Assert.Equal(content.Id, deserializedContent.Id);

        TextContent originalRequest = Assert.IsType<TextContent>(content.Request);
        TextContent deserializedRequest = Assert.IsType<TextContent>(deserializedContent.Request);

        Assert.Equal(originalRequest.Text, deserializedRequest.Text);
    }
}
