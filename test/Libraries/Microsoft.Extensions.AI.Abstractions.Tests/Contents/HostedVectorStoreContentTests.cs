// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedVectorStoreContentTests
{
    [Fact]
    public void Constructor_InvalidInput_Throws()
    {
        Assert.Throws<ArgumentNullException>("vectorStoreId", () => new HostedVectorStoreContent(null!));
        Assert.Throws<ArgumentException>("vectorStoreId", () => new HostedVectorStoreContent(string.Empty));
        Assert.Throws<ArgumentException>("vectorStoreId", () => new HostedVectorStoreContent(" "));
    }

    [Fact]
    public void Constructor_String_PropsDefault()
    {
        HostedVectorStoreContent c = new("id123");
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Equal("id123", c.VectorStoreId);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        HostedVectorStoreContent c = new("id123");

        Assert.Equal("id123", c.VectorStoreId);
        c.VectorStoreId = "id456";
        Assert.Equal("id456", c.VectorStoreId);

        Assert.Throws<ArgumentNullException>("value", () => c.VectorStoreId = null!);
        Assert.Throws<ArgumentException>("value", () => c.VectorStoreId = string.Empty);
        Assert.Throws<ArgumentException>("value", () => c.VectorStoreId = " ");
        Assert.Equal("id456", c.VectorStoreId);

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
        var content = new HostedVectorStoreContent("vectorstore123");

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedContent = JsonSerializer.Deserialize<HostedVectorStoreContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedContent);
        Assert.Equal(content.VectorStoreId, deserializedContent.VectorStoreId);
    }
}
