// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AIContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        AIContent c = new();
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        AIContent c = new();

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
        AIContent original = new()
        {
            RawRepresentation = new object(),
            AdditionalProperties = new AdditionalPropertiesDictionary { { "key", "value" } }
        };

        Assert.NotNull(original.RawRepresentation);
        Assert.Single(original.AdditionalProperties);

        string json = JsonSerializer.Serialize(original, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(AIContent)));
        Assert.NotNull(json);

        AIContent? deserialized = (AIContent?)JsonSerializer.Deserialize(json, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(AIContent)));
        Assert.NotNull(deserialized);
        Assert.Null(deserialized.RawRepresentation);
        Assert.NotNull(deserialized.AdditionalProperties);
        Assert.Single(deserialized.AdditionalProperties);
    }
}
