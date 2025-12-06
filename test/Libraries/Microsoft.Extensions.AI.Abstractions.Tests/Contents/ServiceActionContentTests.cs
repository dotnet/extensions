// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ServiceActionContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        ServiceActionContent c = new("action123");
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Equal("action123", c.Id);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        ServiceActionContent c = new("action123");

        Assert.Equal("action123", c.Id);

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
        ServiceActionContent content = new("action123")
        {
            AdditionalProperties = new AdditionalPropertiesDictionary { { "key", "value" } }
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedSut = JsonSerializer.Deserialize<ServiceActionContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedSut);
        Assert.Equal("action123", deserializedSut.Id);
        Assert.NotNull(deserializedSut.AdditionalProperties);
        Assert.Single(deserializedSut.AdditionalProperties);
        Assert.Equal("value", deserializedSut.AdditionalProperties["key"]?.ToString());
    }

    [Fact]
    public void Constructor_Throws()
    {
        Assert.Throws<ArgumentNullException>("id", () => new ServiceActionContent(null!));
        Assert.Throws<ArgumentException>("id", () => new ServiceActionContent(string.Empty));
    }
}
