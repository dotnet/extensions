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
        Assert.Throws<ArgumentNullException>("choices", () => new ChatResponse((IList<ChatMessage>)null!));
    }

    [Fact]
    public void Constructor_Message_Roundtrips()
    {
        ChatMessage message = new();

        ChatResponse response = new(message);
        Assert.Same(message, response.Message);
        Assert.Same(message, Assert.Single(response.Choices));
    }

    [Fact]
    public void Constructor_Choices_Roundtrips()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(),
            new ChatMessage(),
            new ChatMessage(),
        ];

        ChatResponse response = new(messages);
        Assert.Same(messages, response.Choices);
        Assert.Equal(3, messages.Count);
    }

    [Fact]
    public void Message_EmptyChoices_Throws()
    {
        ChatResponse response = new([]);

        Assert.Empty(response.Choices);
        Assert.Throws<InvalidOperationException>(() => response.Message);
    }

    [Fact]
    public void Message_SingleChoice_Returned()
    {
        ChatMessage message = new();
        ChatResponse response = new([message]);

        Assert.Same(message, response.Message);
        Assert.Same(message, response.Choices[0]);
    }

    [Fact]
    public void Message_MultipleChoices_ReturnsFirst()
    {
        ChatMessage first = new();
        ChatResponse response = new([
            first,
            new ChatMessage(),
        ]);

        Assert.Same(first, response.Message);
        Assert.Same(first, response.Choices[0]);
    }

    [Fact]
    public void Choices_SetNull_Throws()
    {
        ChatResponse response = new([]);
        Assert.Throws<ArgumentNullException>("value", () => response.Choices = null!);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        ChatResponse response = new([]);

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

        List<ChatMessage> newChoices = [new ChatMessage(), new ChatMessage()];
        response.Choices = newChoices;
        Assert.Same(newChoices, response.Choices);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        ChatResponse original = new(
        [
            new ChatMessage(ChatRole.Assistant, "Choice1"),
            new ChatMessage(ChatRole.Assistant, "Choice2"),
            new ChatMessage(ChatRole.Assistant, "Choice3"),
            new ChatMessage(ChatRole.Assistant, "Choice4"),
        ])
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
        Assert.Equal(4, result.Choices.Count);

        for (int i = 0; i < original.Choices.Count; i++)
        {
            Assert.Equal(ChatRole.Assistant, result.Choices[i].Role);
            Assert.Equal($"Choice{i + 1}", result.Choices[i].Text);
        }

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
    public void ToString_OneChoice_OutputsChatMessageToString()
    {
        ChatResponse response = new(
        [
            new ChatMessage(ChatRole.Assistant, "This is a test." + Environment.NewLine + "It's multiple lines.")
        ]);

        Assert.Equal(response.Choices[0].Text, response.ToString());
    }

    [Fact]
    public void ToString_MultipleChoices_OutputsAllChoicesWithPrefix()
    {
        ChatResponse response = new(
        [
            new ChatMessage(ChatRole.Assistant, "This is a test." + Environment.NewLine + "It's multiple lines."),
            new ChatMessage(ChatRole.Assistant, "So is" + Environment.NewLine + " this."),
            new ChatMessage(ChatRole.Assistant, "And this."),
        ]);

        Assert.Equal(
            "Choice 0:" + Environment.NewLine +
            response.Choices[0] + Environment.NewLine + Environment.NewLine +

            "Choice 1:" + Environment.NewLine +
            response.Choices[1] + Environment.NewLine + Environment.NewLine +

            "Choice 2:" + Environment.NewLine +
            response.Choices[2],

            response.ToString());
    }

    [Fact]
    public void ToChatResponseUpdates_SingleChoice()
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

    [Fact]
    public void ToChatResponseUpdates_MultiChoice()
    {
        ChatResponse response = new(
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new TextContent("Hello, "),
                new DataContent("http://localhost/image.png", mediaType: "image/png"),
                new TextContent("world!"),
            ])
            {
                AdditionalProperties = new() { ["choice1Key"] = "choice1Value" },
            },

            new ChatMessage(ChatRole.System,
            [
                new FunctionCallContent("call123", "name"),
                new FunctionResultContent("call123", 42),
            ])
            {
                AdditionalProperties = new() { ["choice2Key"] = "choice2Value" },
            },
        ])
        {
            ResponseId = "12345",
            ModelId = "someModel",
            FinishReason = ChatFinishReason.ContentFilter,
            CreatedAt = new DateTimeOffset(2024, 11, 10, 9, 20, 0, TimeSpan.Zero),
            AdditionalProperties = new() { ["key1"] = "value1", ["key2"] = 42 },
            Usage = new UsageDetails { TotalTokenCount = 123 },
        };

        ChatResponseUpdate[] updates = response.ToChatResponseUpdates();
        Assert.NotNull(updates);
        Assert.Equal(3, updates.Length);

        ChatResponseUpdate update0 = updates[0];
        Assert.Equal("12345", update0.ResponseId);
        Assert.Equal("someModel", update0.ModelId);
        Assert.Equal(ChatFinishReason.ContentFilter, update0.FinishReason);
        Assert.Equal(new DateTimeOffset(2024, 11, 10, 9, 20, 0, TimeSpan.Zero), update0.CreatedAt);
        Assert.Equal("assistant", update0.Role?.Value);
        Assert.Equal("Hello, ", Assert.IsType<TextContent>(update0.Contents[0]).Text);
        Assert.Equal("image/png", Assert.IsType<DataContent>(update0.Contents[1]).MediaType);
        Assert.Equal("world!", Assert.IsType<TextContent>(update0.Contents[2]).Text);
        Assert.Equal("choice1Value", update0.AdditionalProperties?["choice1Key"]);

        ChatResponseUpdate update1 = updates[1];
        Assert.Equal("12345", update1.ResponseId);
        Assert.Equal("someModel", update1.ModelId);
        Assert.Equal(ChatFinishReason.ContentFilter, update1.FinishReason);
        Assert.Equal(new DateTimeOffset(2024, 11, 10, 9, 20, 0, TimeSpan.Zero), update1.CreatedAt);
        Assert.Equal("system", update1.Role?.Value);
        Assert.IsType<FunctionCallContent>(update1.Contents[0]);
        Assert.IsType<FunctionResultContent>(update1.Contents[1]);
        Assert.Equal("choice2Value", update1.AdditionalProperties?["choice2Key"]);

        ChatResponseUpdate update2 = updates[2];
        Assert.Equal("value1", update2.AdditionalProperties?["key1"]);
        Assert.Equal(42, update2.AdditionalProperties?["key2"]);
        Assert.Equal(123, Assert.IsType<UsageContent>(Assert.Single(update2.Contents)).Details.TotalTokenCount);
    }
}
