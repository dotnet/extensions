// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ChatResponseTests
{
    [Fact]
    public void Constructor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("message", () => new ChatResponse((ChatMessage)null!));
        Assert.Throws<ArgumentNullException>("messages", () => new ChatResponse((List<ChatMessage>)null!));
    }

    [Fact]
    public void Constructor_Message_Roundtrips()
    {
        ChatResponse response = new();
        Assert.NotNull(response.Message);
        Assert.Same(response.Message, response.Message);

        ChatMessage message = new();
        response = new(message);
        Assert.Same(message, response.Message);

        message = new();
        response.Message = message;
        Assert.Same(message, response.Message);
    }

    [Fact]
    public void Constructor_Messages_Roundtrips()
    {
        ChatResponse response = new();
        Assert.NotNull(response.Messages);
        Assert.Same(response.Messages, response.Messages);

        List<ChatMessage> messages = new();
        response = new(messages);
        Assert.Same(messages, response.Messages);

        messages = new();
        response.Messages = messages;
        Assert.Same(messages, response.Messages);
    }

    [Fact]
    public void Message_LastMessageOfMessages()
    {
        ChatResponse response = new();

        Assert.Empty(response.Messages);
        Assert.NotNull(response.Message);
        Assert.NotEmpty(response.Messages);

        for (int i = 1; i < 3; i++)
        {
            Assert.Same(response.Messages[response.Messages.Count - 1], response.Message);
            response.Messages.Add(new ChatMessage(ChatRole.User, $"Message {i}"));
        }
    }

    [Fact]
    public void Message_SetterSetsLast()
    {
        ChatResponse response = new();

        Assert.Empty(response.Messages);
        ChatMessage message = new();
        response.Message = message;
        Assert.NotEmpty(response.Messages);
        Assert.Same(message, response.Messages[0]);

        message = new();
        response.Message = message;
        Assert.Single(response.Messages);
        Assert.Same(message, response.Messages[0]);
        Assert.Same(message, response.Message);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        ChatResponse response = new();

        Assert.Null(response.ResponseId);
        response.ResponseId = "id";
        Assert.Equal("id", response.ResponseId);

        Assert.Null(response.ModelId);
        response.ModelId = "modelId";
        Assert.Equal("modelId", response.ModelId);

        Assert.Null(response.CreatedAt);
        response.CreatedAt = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), response.CreatedAt);

        Assert.Null(response.FinishReason);
        response.FinishReason = ChatFinishReason.ContentFilter;
        Assert.Equal(ChatFinishReason.ContentFilter, response.FinishReason);

        Assert.Null(response.Usage);
        UsageDetails usage = new();
        response.Usage = usage;
        Assert.Same(usage, response.Usage);

        Assert.Null(response.RawRepresentation);
        object raw = new();
        response.RawRepresentation = raw;
        Assert.Same(raw, response.RawRepresentation);

        Assert.Null(response.AdditionalProperties);
        AdditionalPropertiesDictionary additionalProps = [];
        response.AdditionalProperties = additionalProps;
        Assert.Same(additionalProps, response.AdditionalProperties);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        ChatResponse original = new(new ChatMessage(ChatRole.Assistant, "the message"))
        {
            ResponseId = "id",
            ModelId = "modelId",
            CreatedAt = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero),
            FinishReason = ChatFinishReason.ContentFilter,
            Usage = new UsageDetails(),
            RawRepresentation = new(),
            AdditionalProperties = new() { ["key"] = "value" },
        };

        string json = JsonSerializer.Serialize(original, TestJsonSerializerContext.Default.ChatResponse);

        ChatResponse? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ChatResponse);

        Assert.NotNull(result);
        Assert.Equal(ChatRole.Assistant, result.Message.Role);
        Assert.Equal("the message", result.Message.Text);

        Assert.Equal("id", result.ResponseId);
        Assert.Equal("modelId", result.ModelId);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), result.CreatedAt);
        Assert.Equal(ChatFinishReason.ContentFilter, result.FinishReason);
        Assert.NotNull(result.Usage);

        Assert.NotNull(result.AdditionalProperties);
        Assert.Single(result.AdditionalProperties);
        Assert.True(result.AdditionalProperties.TryGetValue("key", out object? value));
        Assert.IsType<JsonElement>(value);
        Assert.Equal("value", ((JsonElement)value!).GetString());
    }

    [Fact]
    public void ToString_OutputsChatMessageToString()
    {
        ChatResponse response = new(new ChatMessage(ChatRole.Assistant, $"This is a test.{Environment.NewLine}It's multiple lines."));

        Assert.Equal(response.Message.ToString(), response.ToString());
    }

    [Fact]
    public void ToChatResponseUpdates()
    {
        ChatResponse response = new(new ChatMessage(new ChatRole("customRole"), "Text"))
        {
            ResponseId = "12345",
            ModelId = "someModel",
            FinishReason = ChatFinishReason.ContentFilter,
            CreatedAt = new DateTimeOffset(2024, 11, 10, 9, 20, 0, TimeSpan.Zero),
            AdditionalProperties = new() { ["key1"] = "value1", ["key2"] = 42 },
        };

        ChatResponseUpdate[] updates = response.ToChatResponseUpdates();
        Assert.NotNull(updates);
        Assert.Equal(2, updates.Length);

        ChatResponseUpdate update0 = updates[0];
        Assert.Equal("12345", update0.ResponseId);
        Assert.Equal("someModel", update0.ModelId);
        Assert.Equal(ChatFinishReason.ContentFilter, update0.FinishReason);
        Assert.Equal(new DateTimeOffset(2024, 11, 10, 9, 20, 0, TimeSpan.Zero), update0.CreatedAt);
        Assert.Equal("customRole", update0.Role?.Value);
        Assert.Equal("Text", update0.Text);

        ChatResponseUpdate update1 = updates[1];
        Assert.Equal("value1", update1.AdditionalProperties?["key1"]);
        Assert.Equal(42, update1.AdditionalProperties?["key2"]);
    }
}
