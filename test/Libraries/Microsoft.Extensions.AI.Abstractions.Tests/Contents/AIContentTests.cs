﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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

    [Fact]
    public void Serialization_DerivedTypes_Roundtrips()
    {
        ChatMessage message = new(ChatRole.User,
        [
            new TextContent("a"),
            new TextReasoningContent("reasoning text"),
            new DataContent(new byte[] { 1, 2, 3 }, "application/octet-stream"),
            new UriContent("http://example.com", "application/json"),
            new ErrorContent("error message"),
            new FunctionCallContent("call123", "functionName", new Dictionary<string, object?> { { "param1", 123 } }),
            new FunctionResultContent("call123", "result data"),
            new HostedFileContent("file123"),
            new HostedVectorStoreContent("vectorStore123"),
            new UsageContent(new UsageDetails { InputTokenCount = 10, OutputTokenCount = 20, TotalTokenCount = 30 }),
            new FunctionApprovalRequestContent("request123", new FunctionCallContent("call123", "functionName", new Dictionary<string, object?> { { "param1", 123 } })),
            new FunctionApprovalResponseContent("request123", approved: true, new FunctionCallContent("call123", "functionName", new Dictionary<string, object?> { { "param1", 123 } })),
            new McpServerToolCallContent("call123", "myTool", "myServer"),
            new McpServerToolResultContent("call123"),
            new McpServerToolApprovalRequestContent("request123", new McpServerToolCallContent("call123", "myTool", "myServer")),
            new McpServerToolApprovalResponseContent("request123", approved: true)
        ]);

        var serialized = JsonSerializer.Serialize(message, AIJsonUtilities.DefaultOptions);
        ChatMessage? deserialized = JsonSerializer.Deserialize<ChatMessage>(serialized, AIJsonUtilities.DefaultOptions);
        Assert.NotNull(deserialized);

        Assert.Equal(message.Role, deserialized.Role);
        Assert.Equal(message.Contents.Count, deserialized.Contents.Count);
        for (int i = 0; i < message.Contents.Count; i++)
        {
            Assert.NotNull(message.Contents[i]);
            Assert.Equal(message.Contents[i].GetType(), deserialized.Contents[i].GetType());
        }
    }
}
