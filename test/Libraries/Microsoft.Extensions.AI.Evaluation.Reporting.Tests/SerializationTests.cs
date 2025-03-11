// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Tests;

public class SerializationTests
{
    [Fact]
    public void SerializerChatResponsePropertiesViaAzureStorageSettings()
    {
        ChatResponse response = new ChatResponse();
        response.Messages.Add(new ChatMessage
        {
            Role = ChatRole.User,
            Contents = new List<AIContent>
            {
                new TextContent("A user message"),
            },
        });
        response.AdditionalProperties = new AdditionalPropertiesDictionary
        {
            { "model", "gpt-7" },
            { "data", JsonDocument.Parse("{\"some\":\"data\"}").RootElement },
        };

        string jsonString = JsonSerializer.Serialize(response, AzureStorageSerializerContext.Default.Options);

        ChatResponse? response2 = JsonSerializer.Deserialize<ChatResponse>(jsonString, AzureStorageSerializerContext.Default.Options);

        Assert.NotNull(response2);
        Assert.Equal(response.Messages.Count, response2.Messages.Count);
        Assert.Equal(ChatRole.User, response2.Messages[0].Role);
        Assert.Equal("A user message", response2.Messages[0].Text);
        Assert.NotNull(response2.AdditionalProperties);
        Assert.Equal("gpt-7", response2.AdditionalProperties?["model"]?.ToString());
        Assert.Equal("{\r\n      \"some\": \"data\"\r\n    }", response2.AdditionalProperties?["data"]?.ToString());
    }

    [Fact]
    public void SerializerChatResponsePropertiesViaReportingSettings()
    {
        ChatResponse response = new ChatResponse();
        response.Messages.Add(new ChatMessage
        {
            Role = ChatRole.User,
            Contents = new List<AIContent>
            {
                new TextContent("A user message"),
            },
        });
        response.AdditionalProperties = new AdditionalPropertiesDictionary
        {
            { "model", "gpt-7" },
            { "data", JsonDocument.Parse("{\"some\":\"data\"}").RootElement },
        };

        string jsonString = JsonSerializer.Serialize(response, SerializerContext.Default.Options);

        ChatResponse? response2 = JsonSerializer.Deserialize<ChatResponse>(jsonString, SerializerContext.Default.Options);

        Assert.NotNull(response2);
        Assert.Equal(response.Messages.Count, response2.Messages.Count);
        Assert.Equal(ChatRole.User, response2.Messages[0].Role);
        Assert.Equal("A user message", response2.Messages[0].Text);
        Assert.NotNull(response2.AdditionalProperties);
        Assert.Equal("gpt-7", response2.AdditionalProperties?["model"]?.ToString());
        Assert.Equal("{\r\n      \"some\": \"data\"\r\n    }", response2.AdditionalProperties?["data"]?.ToString());
    }

    [Fact]
    public void SerializerChatResponsePropertiesViaAIJsonUtilities()
    {
        ChatResponse response = new ChatResponse();
        response.Messages.Add(new ChatMessage
        {
            Role = ChatRole.User,
            Contents = new List<AIContent>
            {
                new TextContent("A user message"),
            },
        });
        response.AdditionalProperties = new AdditionalPropertiesDictionary
        {
            { "model", "gpt-7" },
            { "data", JsonDocument.Parse("{\"some\":\"data\"}").RootElement },
        };

        string jsonString = JsonSerializer.Serialize(response, AIJsonUtilities.DefaultOptions);

        ChatResponse? response2 = JsonSerializer.Deserialize<ChatResponse>(jsonString, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(response2);
        Assert.Equal(response.Messages.Count, response2.Messages.Count);
        Assert.Equal(ChatRole.User, response2.Messages[0].Role);
        Assert.Equal("A user message", response2.Messages[0].Text);
        Assert.NotNull(response2.AdditionalProperties);
        Assert.Equal("gpt-7", response2.AdditionalProperties?["model"]?.ToString());
        Assert.Equal("{\r\n      \"some\": \"data\"\r\n    }", response2.AdditionalProperties?["data"]?.ToString());
    }
}
