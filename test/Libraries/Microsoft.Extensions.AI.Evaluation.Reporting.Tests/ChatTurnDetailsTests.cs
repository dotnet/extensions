// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Tests;

public class ChatTurnDetailsTests
{
    [Fact]
    public void DeserializeWithLatencyOnly()
    {
        string json =
            """
            {
              "latency": 5
            }
            """;

        JsonSerializerOptions options = JsonUtilities.Default.Options;
        ChatTurnDetails? details = JsonSerializer.Deserialize<ChatTurnDetails>(json, options);

        Assert.NotNull(details);
        Assert.Equal(TimeSpan.FromSeconds(5), details!.Latency);
        Assert.Null(details.Model);
        Assert.Null(details.ModelProvider);
        Assert.Null(details.Usage);
        Assert.Null(details.CacheKey);
        Assert.Null(details.CacheHit);

        string roundTripJson = JsonSerializer.Serialize(details, options);
        ChatTurnDetails? deserializedDetails = JsonSerializer.Deserialize<ChatTurnDetails>(roundTripJson, options);

        Assert.NotNull(deserializedDetails);
        Assert.Equal(details.Latency, deserializedDetails!.Latency);
        Assert.Null(deserializedDetails.Model);
        Assert.Null(deserializedDetails.ModelProvider);
        Assert.Null(deserializedDetails.Usage);
        Assert.Null(deserializedDetails.CacheKey);
        Assert.Null(deserializedDetails.CacheHit);
    }

    [Fact]
    public void DeserializeWithLatencyAndModel()
    {
        string json =
            """
            {
              "latency": 5,
              "model": "gpt-4"
            }
            """;

        JsonSerializerOptions options = JsonUtilities.Default.Options;
        ChatTurnDetails? details = JsonSerializer.Deserialize<ChatTurnDetails>(json, options);

        Assert.NotNull(details);
        Assert.Equal(TimeSpan.FromSeconds(5), details!.Latency);
        Assert.Equal("gpt-4", details.Model);
        Assert.Null(details.ModelProvider);
        Assert.Null(details.Usage);
        Assert.Null(details.CacheKey);
        Assert.Null(details.CacheHit);

        string roundTripJson = JsonSerializer.Serialize(details, options);
        ChatTurnDetails? deserializedDetails = JsonSerializer.Deserialize<ChatTurnDetails>(roundTripJson, options);

        Assert.NotNull(deserializedDetails);
        Assert.Equal(details.Latency, deserializedDetails!.Latency);
        Assert.Equal(details.Model, deserializedDetails!.Model);
        Assert.Null(deserializedDetails.ModelProvider);
        Assert.Null(deserializedDetails.Usage);
        Assert.Null(deserializedDetails.CacheKey);
        Assert.Null(deserializedDetails.CacheHit);
    }

    [Fact]
    public void DeserializeWithLatencyModelAndModelProvider()
    {
        string json =
            """
            {
              "latency": 5,
              "model": "gpt-4",
              "modelProvider": "azure.openai"
            }
            """;

        JsonSerializerOptions options = JsonUtilities.Default.Options;
        ChatTurnDetails? details = JsonSerializer.Deserialize<ChatTurnDetails>(json, options);

        Assert.NotNull(details);
        Assert.Equal(TimeSpan.FromSeconds(5), details!.Latency);
        Assert.Equal("gpt-4", details.Model);
        Assert.Equal("azure.openai", details.ModelProvider);
        Assert.Null(details.Usage);
        Assert.Null(details.CacheKey);
        Assert.Null(details.CacheHit);

        string roundTripJson = JsonSerializer.Serialize(details, options);
        ChatTurnDetails? deserializedDetails = JsonSerializer.Deserialize<ChatTurnDetails>(roundTripJson, options);

        Assert.NotNull(deserializedDetails);
        Assert.Equal(details.Latency, deserializedDetails!.Latency);
        Assert.Equal(details.Model, deserializedDetails!.Model);
        Assert.Equal(details.ModelProvider, deserializedDetails!.ModelProvider);
        Assert.Null(deserializedDetails.Usage);
        Assert.Null(deserializedDetails.CacheKey);
        Assert.Null(deserializedDetails.CacheHit);
    }

    [Fact]
    public void DeserializeWithoutModelAndModelProvider()
    {
        string json =
            """
            {
              "latency": 1,
              "usage": { "inputTokenCount": 10, "outputTokenCount": 20, "totalTokenCount": 30 },
              "cacheKey": "cache-key",
              "cacheHit": true
            }
            """;

        JsonSerializerOptions options = JsonUtilities.Default.Options;
        ChatTurnDetails? details = JsonSerializer.Deserialize<ChatTurnDetails>(json, options);

        Assert.NotNull(details);
        Assert.Equal(TimeSpan.FromSeconds(1), details!.Latency);
        Assert.Null(details.Model);
        Assert.Null(details.ModelProvider);
        Assert.NotNull(details.Usage);
        Assert.Equal(10, details.Usage!.InputTokenCount);
        Assert.Equal(20, details.Usage.OutputTokenCount);
        Assert.Equal(30, details.Usage.TotalTokenCount);
        Assert.Equal("cache-key", details.CacheKey);
        Assert.True(details.CacheHit);

        string roundTripJson = JsonSerializer.Serialize(details, options);
        ChatTurnDetails? deserializedDetails = JsonSerializer.Deserialize<ChatTurnDetails>(roundTripJson, options);

        Assert.NotNull(deserializedDetails);
        Assert.Equal(details.Latency, deserializedDetails!.Latency);
        Assert.Null(deserializedDetails.Model);
        Assert.Null(deserializedDetails.ModelProvider);
        Assert.Equal(details.Usage!.InputTokenCount, deserializedDetails.Usage!.InputTokenCount);
        Assert.Equal(details.Usage.OutputTokenCount, deserializedDetails.Usage.OutputTokenCount);
        Assert.Equal(details.Usage.TotalTokenCount, deserializedDetails.Usage.TotalTokenCount);
        Assert.Equal(details.CacheKey, deserializedDetails.CacheKey);
        Assert.Equal(details.CacheHit, deserializedDetails.CacheHit);
    }

    [Fact]
    public void DeserializeWithoutModelProvider()
    {
        string json =
            """
            {
              "latency": 1,
              "model": "gpt-4",
              "usage": { "inputTokenCount": 10, "outputTokenCount": 20, "totalTokenCount": 30 },
              "cacheKey": "cache-key",
              "cacheHit": true
            }
            """;

        JsonSerializerOptions options = JsonUtilities.Default.Options;
        ChatTurnDetails? details = JsonSerializer.Deserialize<ChatTurnDetails>(json, options);

        Assert.NotNull(details);
        Assert.Equal(TimeSpan.FromSeconds(1), details!.Latency);
        Assert.Equal("gpt-4", details.Model);
        Assert.Null(details.ModelProvider);
        Assert.NotNull(details.Usage);
        Assert.Equal(10, details.Usage!.InputTokenCount);
        Assert.Equal(20, details.Usage.OutputTokenCount);
        Assert.Equal(30, details.Usage.TotalTokenCount);
        Assert.Equal("cache-key", details.CacheKey);
        Assert.True(details.CacheHit);

        string roundTripJson = JsonSerializer.Serialize(details, options);
        ChatTurnDetails? deserializedDetails = JsonSerializer.Deserialize<ChatTurnDetails>(roundTripJson, options);

        Assert.NotNull(deserializedDetails);
        Assert.Equal(details.Latency, deserializedDetails!.Latency);
        Assert.Equal(details.Model, deserializedDetails.Model);
        Assert.Null(deserializedDetails.ModelProvider);
        Assert.Equal(details.Usage!.InputTokenCount, deserializedDetails.Usage!.InputTokenCount);
        Assert.Equal(details.Usage.OutputTokenCount, deserializedDetails.Usage.OutputTokenCount);
        Assert.Equal(details.Usage.TotalTokenCount, deserializedDetails.Usage.TotalTokenCount);
        Assert.Equal(details.CacheKey, deserializedDetails.CacheKey);
        Assert.Equal(details.CacheHit, deserializedDetails.CacheHit);
    }

    [Fact]
    public void DeserializeWithModelProvider()
    {
        string json =
            """
            {
              "latency": 2,
              "model": "gpt-4",
              "modelProvider": "azure.openai",
              "usage": { "inputTokenCount": 5, "outputTokenCount": 7, "totalTokenCount": 12 },
              "cacheKey": "cache-key-2",
              "cacheHit": false
            }
            """;

        JsonSerializerOptions options = JsonUtilities.Default.Options;
        ChatTurnDetails? details = JsonSerializer.Deserialize<ChatTurnDetails>(json, options);

        Assert.NotNull(details);
        Assert.Equal(TimeSpan.FromSeconds(2), details!.Latency);
        Assert.Equal("gpt-4", details.Model);
        Assert.Equal("azure.openai", details.ModelProvider);
        Assert.NotNull(details.Usage);
        Assert.Equal(5, details.Usage!.InputTokenCount);
        Assert.Equal(7, details.Usage.OutputTokenCount);
        Assert.Equal(12, details.Usage.TotalTokenCount);
        Assert.Equal("cache-key-2", details.CacheKey);
        Assert.False(details.CacheHit);

        string roundTripJson = JsonSerializer.Serialize(details, options);
        ChatTurnDetails? deserializedDetails = JsonSerializer.Deserialize<ChatTurnDetails>(roundTripJson, options);

        Assert.NotNull(deserializedDetails);
        Assert.Equal(details.Latency, deserializedDetails!.Latency);
        Assert.Equal(details.Model, deserializedDetails.Model);
        Assert.Equal(details.ModelProvider, deserializedDetails.ModelProvider);
        Assert.Equal(details.Usage!.InputTokenCount, deserializedDetails.Usage!.InputTokenCount);
        Assert.Equal(details.Usage.OutputTokenCount, deserializedDetails.Usage.OutputTokenCount);
        Assert.Equal(details.Usage.TotalTokenCount, deserializedDetails.Usage.TotalTokenCount);
        Assert.Equal(details.CacheKey, deserializedDetails.CacheKey);
        Assert.Equal(details.CacheHit, deserializedDetails.CacheHit);
    }
}
