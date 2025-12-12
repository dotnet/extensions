// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class UsageDetailsTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        UsageDetails details = new();
        Assert.Null(details.InputTokenCount);
        Assert.Null(details.OutputTokenCount);
        Assert.Null(details.TotalTokenCount);
        Assert.Null(details.CachedInputTokenCount);
        Assert.Null(details.ReasoningTokenCount);
        Assert.Null(details.AdditionalCounts);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        UsageDetails details = new()
        {
            InputTokenCount = 10,
            OutputTokenCount = 20,
            TotalTokenCount = 30,
            CachedInputTokenCount = 5,
            ReasoningTokenCount = 8,
            AdditionalCounts = new() { ["custom"] = 100 }
        };

        Assert.Equal(10, details.InputTokenCount);
        Assert.Equal(20, details.OutputTokenCount);
        Assert.Equal(30, details.TotalTokenCount);
        Assert.Equal(5, details.CachedInputTokenCount);
        Assert.Equal(8, details.ReasoningTokenCount);
        Assert.NotNull(details.AdditionalCounts);
        Assert.Equal(100, details.AdditionalCounts["custom"]);
    }

    [Fact]
    public void Add_NullUsage_Throws()
    {
        UsageDetails details = new();
        Assert.Throws<ArgumentNullException>("usage", () => details.Add(null!));
    }

    [Fact]
    public void Add_SumsAllProperties()
    {
        UsageDetails details1 = new()
        {
            InputTokenCount = 10,
            OutputTokenCount = 20,
            TotalTokenCount = 30,
            CachedInputTokenCount = 5,
            ReasoningTokenCount = 8,
        };

        UsageDetails details2 = new()
        {
            InputTokenCount = 15,
            OutputTokenCount = 25,
            TotalTokenCount = 40,
            CachedInputTokenCount = 7,
            ReasoningTokenCount = 12,
        };

        details1.Add(details2);

        Assert.Equal(25, details1.InputTokenCount);
        Assert.Equal(45, details1.OutputTokenCount);
        Assert.Equal(70, details1.TotalTokenCount);
        Assert.Equal(12, details1.CachedInputTokenCount);
        Assert.Equal(20, details1.ReasoningTokenCount);
    }

    [Fact]
    public void Add_WithNullValues_HandlesCorrectly()
    {
        UsageDetails details1 = new()
        {
            InputTokenCount = 10,
            CachedInputTokenCount = 5,
        };

        UsageDetails details2 = new()
        {
            OutputTokenCount = 25,
            ReasoningTokenCount = 12,
        };

        details1.Add(details2);

        Assert.Equal(10, details1.InputTokenCount);
        Assert.Equal(25, details1.OutputTokenCount);
        Assert.Null(details1.TotalTokenCount);
        Assert.Equal(5, details1.CachedInputTokenCount);
        Assert.Equal(12, details1.ReasoningTokenCount);
    }

    [Fact]
    public void Add_FromNullToValue_SetsValue()
    {
        UsageDetails details1 = new();

        UsageDetails details2 = new()
        {
            CachedInputTokenCount = 5,
            ReasoningTokenCount = 10,
        };

        details1.Add(details2);

        Assert.Equal(5, details1.CachedInputTokenCount);
        Assert.Equal(10, details1.ReasoningTokenCount);
    }

    [Fact]
    public void Add_AdditionalCounts_MergesCorrectly()
    {
        UsageDetails details1 = new()
        {
            AdditionalCounts = new() { ["key1"] = 10, ["key2"] = 20 }
        };

        UsageDetails details2 = new()
        {
            AdditionalCounts = new() { ["key2"] = 30, ["key3"] = 40 }
        };

        details1.Add(details2);

        Assert.NotNull(details1.AdditionalCounts);
        Assert.Equal(10, details1.AdditionalCounts["key1"]);
        Assert.Equal(50, details1.AdditionalCounts["key2"]);
        Assert.Equal(40, details1.AdditionalCounts["key3"]);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        UsageDetails details = new()
        {
            InputTokenCount = 10,
            OutputTokenCount = 20,
            TotalTokenCount = 30,
            CachedInputTokenCount = 5,
            ReasoningTokenCount = 8,
            AdditionalCounts = new() { ["custom"] = 100 }
        };

        string json = JsonSerializer.Serialize(details, AIJsonUtilities.DefaultOptions);
        UsageDetails? deserialized = JsonSerializer.Deserialize<UsageDetails>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(details.InputTokenCount, deserialized.InputTokenCount);
        Assert.Equal(details.OutputTokenCount, deserialized.OutputTokenCount);
        Assert.Equal(details.TotalTokenCount, deserialized.TotalTokenCount);
        Assert.Equal(details.CachedInputTokenCount, deserialized.CachedInputTokenCount);
        Assert.Equal(details.ReasoningTokenCount, deserialized.ReasoningTokenCount);
        Assert.NotNull(deserialized.AdditionalCounts);
        Assert.Equal(100, deserialized.AdditionalCounts["custom"]);
    }

    [Fact]
    public void Serialization_WithNullProperties_Roundtrips()
    {
        UsageDetails details = new()
        {
            InputTokenCount = 10,
            OutputTokenCount = 20,
        };

        string json = JsonSerializer.Serialize(details, AIJsonUtilities.DefaultOptions);
        UsageDetails? deserialized = JsonSerializer.Deserialize<UsageDetails>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(10, deserialized.InputTokenCount);
        Assert.Equal(20, deserialized.OutputTokenCount);
        Assert.Null(deserialized.TotalTokenCount);
        Assert.Null(deserialized.CachedInputTokenCount);
        Assert.Null(deserialized.ReasoningTokenCount);
    }
}
