// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class UsageContentTests
{
    [Fact]
    public void Constructor_InvalidArg_Throws()
    {
        Assert.Throws<ArgumentNullException>("details", () => new UsageContent(null!));
    }

    [Fact]
    public void Constructor_Parameterless_PropsDefault()
    {
        UsageContent c = new();
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);

        Assert.NotNull(c.Details);
        Assert.Same(c.Details, c.Details);
        Assert.Null(c.Details.InputTokenCount);
        Assert.Null(c.Details.OutputTokenCount);
        Assert.Null(c.Details.TotalTokenCount);
        Assert.Null(c.Details.AdditionalCounts);
    }

    [Fact]
    public void Constructor_UsageDetails_PropsRoundtrip()
    {
        UsageDetails details = new();

        UsageContent c = new(details);
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);

        Assert.Same(details, c.Details);

        UsageDetails details2 = new();
        c.Details = details2;
        Assert.Same(details2, c.Details);
    }

    [Fact]
    public void Details_SetNull_Throws()
    {
        UsageContent c = new();

        UsageDetails d = c.Details;
        Assert.NotNull(d);

        Assert.Throws<ArgumentNullException>("value", () => c.Details = null!);

        Assert.Same(d, c.Details);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        var content = new UsageContent(new UsageDetails
        {
            InputTokenCount = 10,
            OutputTokenCount = 20,
            TotalTokenCount = 30
        });

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedContent = JsonSerializer.Deserialize<UsageContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedContent);
        Assert.NotNull(deserializedContent.Details);
        Assert.Equal(content.Details.InputTokenCount, deserializedContent.Details.InputTokenCount);
        Assert.Equal(content.Details.OutputTokenCount, deserializedContent.Details.OutputTokenCount);
        Assert.Equal(content.Details.TotalTokenCount, deserializedContent.Details.TotalTokenCount);
    }
}
