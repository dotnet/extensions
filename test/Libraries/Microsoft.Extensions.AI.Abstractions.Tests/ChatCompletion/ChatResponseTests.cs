// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ChatResponseTests
{
    [Fact]
    public void Constructor_NullEmptyArgs_Valid()
    {
        ChatResponse response;

        response = new();
        Assert.Empty(response.Messages);
        Assert.Empty(response.Text);

        response = new((IList<ChatMessage>?)null);
        Assert.Empty(response.Messages);
        Assert.Empty(response.Text);

        Assert.Throws<ArgumentNullException>("message", () => new ChatResponse((ChatMessage)null!));
    }

    [Fact]
    public void Constructor_Messages_Roundtrips()
    {
        ChatResponse response = new();
        Assert.NotNull(response.Messages);
        Assert.Same(response.Messages, response.Messages);

        List<ChatMessage> messages = [];
        response = new(messages);
        Assert.Same(messages, response.Messages);

        messages = [];
        Assert.NotSame(messages, response.Messages);
        response.Messages = messages;
        Assert.Same(messages, response.Messages);
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
            ConversationId = "conv123",
            CreatedAt = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero),
            FinishReason = ChatFinishReason.ContentFilter,
            Usage = new UsageDetails(),
            RawRepresentation = new Dictionary<string, object?> { ["value"] = 42 },
            AdditionalProperties = new() { ["key"] = "value" },
        };

        string json = JsonSerializer.Serialize(original, TestJsonSerializerContext.Default.ChatResponse);

        ChatResponse? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ChatResponse);

        Assert.NotNull(result);
        Assert.Equal(ChatRole.Assistant, result.Messages.Single().Role);
        Assert.Equal("the message", result.Messages.Single().Text);

        Assert.Equal("id", result.ResponseId);
        Assert.Equal("modelId", result.ModelId);
        Assert.Equal("conv123", result.ConversationId);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), result.CreatedAt);
        Assert.Equal(ChatFinishReason.ContentFilter, result.FinishReason);
        Assert.NotNull(result.Usage);
        JsonElement rawRepresentation = Assert.IsType<JsonElement>(result.RawRepresentation);
        Assert.Equal(42, rawRepresentation.GetProperty("value").GetInt32());

        Assert.NotNull(result.AdditionalProperties);
        Assert.Single(result.AdditionalProperties);
        Assert.True(result.AdditionalProperties.TryGetValue("key", out object? value));
        Assert.IsType<JsonElement>(value);
        Assert.Equal("value", ((JsonElement)value!).GetString());
    }

    [Fact]
    public void JsonSerialization_Roundtrips_DefaultOptions()
    {
        ChatResponse original = new(new ChatMessage(ChatRole.Assistant, "the message"))
        {
            ResponseId = "id",
            ModelId = "modelId",
            ConversationId = "conv123",
            CreatedAt = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero),
            FinishReason = ChatFinishReason.ContentFilter,
            Usage = new UsageDetails(),
            RawRepresentation = new Dictionary<string, object?> { ["value"] = 42 },
            AdditionalProperties = new() { ["key"] = "value" },
            ContinuationToken = ResponseContinuationToken.FromBytes(new byte[] { 1, 2, 3 }),
        };

        string json = JsonSerializer.Serialize(original, AIJsonUtilities.DefaultOptions);

        ChatResponse? result = JsonSerializer.Deserialize<ChatResponse>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(result);
        Assert.Equal(ChatRole.Assistant, result.Messages.Single().Role);
        Assert.Equal("the message", result.Messages.Single().Text);

        Assert.Equal("id", result.ResponseId);
        Assert.Equal("modelId", result.ModelId);
        Assert.Equal("conv123", result.ConversationId);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), result.CreatedAt);
        Assert.Equal(ChatFinishReason.ContentFilter, result.FinishReason);
        Assert.NotNull(result.Usage);
        JsonElement rawRepresentation = Assert.IsType<JsonElement>(result.RawRepresentation);
        Assert.Equal(42, rawRepresentation.GetProperty("value").GetInt32());

        Assert.NotNull(result.ContinuationToken);
        Assert.Equal(new byte[] { 1, 2, 3 }, result.ContinuationToken.ToBytes().ToArray());

        Assert.NotNull(result.AdditionalProperties);
        Assert.Single(result.AdditionalProperties);
        Assert.True(result.AdditionalProperties.TryGetValue("key", out object? value));
        Assert.Equal("value", value?.ToString());
    }

    [Fact]
    public void JsonDeserialization_KnownPayload()
    {
        const string Json = """
            {
              "messages": [
                {
                  "role": "assistant",
                  "contents": [
                    {
                      "$type": "text",
                      "text": "the message"
                    }
                  ],
                  "authorName": "bot",
                  "messageId": "msg1",
                  "createdAt": "2022-01-01T00:00:00+00:00",
                  "additionalProperties": {
                    "msgKey": "msgVal"
                  }
                }
              ],
              "responseId": "id",
              "conversationId": "conv123",
              "modelId": "modelId",
              "createdAt": "2022-01-01T00:00:00+00:00",
              "finishReason": "content_filter",
              "usage": {
                "inputTokenCount": 10,
                "outputTokenCount": 20,
                "totalTokenCount": 30
              },
              "continuationToken": "AQID",
              "additionalProperties": {
                "key": "value"
              }
            }
            """;

        ChatResponse? result = JsonSerializer.Deserialize<ChatResponse>(Json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(result);
        Assert.Single(result.Messages);
        Assert.Equal(ChatRole.Assistant, result.Messages[0].Role);
        Assert.Equal("the message", result.Messages[0].Text);
        Assert.Equal("bot", result.Messages[0].AuthorName);
        Assert.Equal("msg1", result.Messages[0].MessageId);
        Assert.NotNull(result.Messages[0].AdditionalProperties);
        Assert.Equal("msgVal", result.Messages[0].AdditionalProperties!["msgKey"]?.ToString());

        Assert.Equal("id", result.ResponseId);
        Assert.Equal("conv123", result.ConversationId);
        Assert.Equal("modelId", result.ModelId);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), result.CreatedAt);
        Assert.Equal(ChatFinishReason.ContentFilter, result.FinishReason);

        Assert.NotNull(result.Usage);
        Assert.Equal(10, result.Usage.InputTokenCount);
        Assert.Equal(20, result.Usage.OutputTokenCount);
        Assert.Equal(30, result.Usage.TotalTokenCount);

        Assert.NotNull(result.ContinuationToken);
        Assert.Equal(new byte[] { 1, 2, 3 }, result.ContinuationToken.ToBytes().ToArray());

        Assert.NotNull(result.AdditionalProperties);
        Assert.Single(result.AdditionalProperties);
        Assert.Equal("value", result.AdditionalProperties["key"]?.ToString());
    }

    [Fact]
    public void JsonSerialization_WritesEmptyObjectForUnsupportedRawRepresentation()
    {
        Dictionary<string, object?> rawRepresentation = [];
        rawRepresentation["self"] = rawRepresentation;

        ChatResponse original = new(new ChatMessage(ChatRole.Assistant, "the message"))
        {
            RawRepresentation = rawRepresentation,
        };

        string json = JsonSerializer.Serialize(original, AIJsonUtilities.DefaultOptions);
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.TryGetProperty("rawRepresentation", out JsonElement rawRep));
        Assert.Equal(JsonValueKind.Object, rawRep.ValueKind);
        Assert.Empty(rawRep.EnumerateObject().ToArray());

        ChatResponse? result = JsonSerializer.Deserialize<ChatResponse>(json, AIJsonUtilities.DefaultOptions);
        Assert.NotNull(result);
        Assert.IsType<JsonElement>(result.RawRepresentation);
        Assert.Equal(JsonValueKind.Object, ((JsonElement)result.RawRepresentation).ValueKind);
    }

    [Fact]
    public void ToString_OutputsText()
    {
        ChatResponse response = new(new ChatMessage(ChatRole.Assistant, $"This is a test.{Environment.NewLine}It's multiple lines."));

        Assert.Equal(response.Text, response.ToString());
    }

    [Fact]
    public void ToChatResponseUpdates_SingleMessage()
    {
        ChatResponse response = new(new ChatMessage(new ChatRole("customRole"), "Text") { MessageId = "someMessage" })
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
        Assert.Equal("someMessage", update0.MessageId);
        Assert.Equal("someModel", update0.ModelId);
        Assert.Equal(ChatFinishReason.ContentFilter, update0.FinishReason);
        Assert.Equal(new DateTimeOffset(2024, 11, 10, 9, 20, 0, TimeSpan.Zero), update0.CreatedAt);
        Assert.Equal("customRole", update0.Role?.Value);
        Assert.Equal("Text", update0.Text);

        ChatResponseUpdate update1 = updates[1];
        Assert.Equal("value1", update1.AdditionalProperties?["key1"]);
        Assert.Equal(42, update1.AdditionalProperties?["key2"]);
    }

    [Fact]
    public void ToChatResponseUpdates_MultipleMessages()
    {
        ChatResponse response = new(
            [
                new ChatMessage(new ChatRole("customRole"), "Text")
                {
                    CreatedAt = new DateTimeOffset(2024, 11, 10, 9, 20, 0, TimeSpan.Zero),
                    MessageId = "someMessage"
                },
                new ChatMessage(new ChatRole("secondRole"), "Another message")
                {
                    CreatedAt = new DateTimeOffset(2025, 1, 1, 10, 30, 0, TimeSpan.Zero),
                    MessageId = "anotherMessage"
                }
            ])
        {
            ResponseId = "12345",
            ModelId = "someModel",
            FinishReason = ChatFinishReason.ContentFilter,
            CreatedAt = new DateTimeOffset(2024, 11, 10, 9, 20, 0, TimeSpan.Zero),
            AdditionalProperties = new() { ["key1"] = "value1", ["key2"] = 42 },
        };

        ChatResponseUpdate[] updates = response.ToChatResponseUpdates();
        Assert.NotNull(updates);
        Assert.Equal(3, updates.Length);

        ChatResponseUpdate update0 = updates[0];
        Assert.Equal("12345", update0.ResponseId);
        Assert.Equal("someMessage", update0.MessageId);
        Assert.Equal("someModel", update0.ModelId);
        Assert.Equal(ChatFinishReason.ContentFilter, update0.FinishReason);
        Assert.Equal(new DateTimeOffset(2024, 11, 10, 9, 20, 0, TimeSpan.Zero), update0.CreatedAt);
        Assert.Equal("customRole", update0.Role?.Value);
        Assert.Equal("Text", update0.Text);

        ChatResponseUpdate update1 = updates[1];
        Assert.Equal("12345", update1.ResponseId);
        Assert.Equal("anotherMessage", update1.MessageId);
        Assert.Equal("someModel", update1.ModelId);
        Assert.Equal(ChatFinishReason.ContentFilter, update1.FinishReason);
        Assert.Equal(new DateTimeOffset(2025, 1, 1, 10, 30, 0, TimeSpan.Zero), update1.CreatedAt);
        Assert.Equal("secondRole", update1.Role?.Value);
        Assert.Equal("Another message", update1.Text);

        ChatResponseUpdate update2 = updates[2];
        Assert.Equal("value1", update2.AdditionalProperties?["key1"]);
        Assert.Equal(42, update2.AdditionalProperties?["key2"]);
    }
}
