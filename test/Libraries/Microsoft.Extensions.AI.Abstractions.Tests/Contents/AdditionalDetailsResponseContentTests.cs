// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI.Contents;

public class AdditionalDetailsResponseContentTests
{
    [Fact]
    public void Constructor_InvalidArguments_Throws()
    {
        Assert.Throws<ArgumentNullException>("id", () => new AdditionalDetailsResponseContent(null!, new TextContent("response")));
        Assert.Throws<ArgumentException>("id", () => new AdditionalDetailsResponseContent("", new TextContent("response")));
        Assert.Throws<ArgumentException>("id", () => new AdditionalDetailsResponseContent("\r\t\n ", new TextContent("response")));

        Assert.Throws<ArgumentNullException>("response", () => new AdditionalDetailsResponseContent("id", null!));
    }

    [Fact]
    public void Constructor_Roundtrips()
    {
        string id = "test-id";
        TextContent response = new("test-response");
        AdditionalDetailsResponseContent content = new(id, response);

        Assert.Same(id, content.Id);
        Assert.Same(response, content.Response);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        var content = new AdditionalDetailsResponseContent("response123", new TextContent("This is my answer"));

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedContent = JsonSerializer.Deserialize<AdditionalDetailsResponseContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedContent);
        Assert.Equal(content.Id, deserializedContent.Id);

        TextContent originalResponse = Assert.IsType<TextContent>(content.Response);
        TextContent deserializedResponse = Assert.IsType<TextContent>(deserializedContent.Response);

        Assert.Equal(originalResponse.Text, deserializedResponse.Text);
    }
}
