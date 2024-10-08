// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ChatCompletionTests
{
    [Fact]
    public void Constructor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("message", () => new ChatCompletion((ChatMessage)null!));
        Assert.Throws<ArgumentNullException>("choices", () => new ChatCompletion((IList<ChatMessage>)null!));
    }

    [Fact]
    public void Constructor_Message_Roundtrips()
    {
        ChatMessage message = new();

        ChatCompletion completion = new(message);
        Assert.Same(message, completion.Message);
        Assert.Same(message, Assert.Single(completion.Choices));
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

        ChatCompletion completion = new(messages);
        Assert.Same(messages, completion.Choices);
        Assert.Equal(3, messages.Count);
    }

    [Fact]
    public void Message_EmptyChoices_Throws()
    {
        ChatCompletion completion = new([]);

        Assert.Empty(completion.Choices);
        Assert.Throws<InvalidOperationException>(() => completion.Message);
    }

    [Fact]
    public void Message_SingleChoice_Returned()
    {
        ChatMessage message = new();
        ChatCompletion completion = new([message]);

        Assert.Same(message, completion.Message);
        Assert.Same(message, completion.Choices[0]);
    }

    [Fact]
    public void Message_MultipleChoices_ReturnsFirst()
    {
        ChatMessage first = new();
        ChatCompletion completion = new([
            first,
            new ChatMessage(),
        ]);

        Assert.Same(first, completion.Message);
        Assert.Same(first, completion.Choices[0]);
    }

    [Fact]
    public void Choices_SetNull_Throws()
    {
        ChatCompletion completion = new([]);
        Assert.Throws<ArgumentNullException>("value", () => completion.Choices = null!);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        ChatCompletion completion = new([]);

        Assert.Null(completion.CompletionId);
        completion.CompletionId = "id";
        Assert.Equal("id", completion.CompletionId);

        Assert.Null(completion.ModelId);
        completion.ModelId = "modelId";
        Assert.Equal("modelId", completion.ModelId);

        Assert.Null(completion.CreatedAt);
        completion.CreatedAt = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), completion.CreatedAt);

        Assert.Null(completion.FinishReason);
        completion.FinishReason = ChatFinishReason.ContentFilter;
        Assert.Equal(ChatFinishReason.ContentFilter, completion.FinishReason);

        Assert.Null(completion.Usage);
        UsageDetails usage = new();
        completion.Usage = usage;
        Assert.Same(usage, completion.Usage);

        Assert.Null(completion.RawRepresentation);
        object raw = new();
        completion.RawRepresentation = raw;
        Assert.Same(raw, completion.RawRepresentation);

        Assert.Null(completion.AdditionalProperties);
        AdditionalPropertiesDictionary additionalProps = [];
        completion.AdditionalProperties = additionalProps;
        Assert.Same(additionalProps, completion.AdditionalProperties);

        List<ChatMessage> newChoices = [new ChatMessage(), new ChatMessage()];
        completion.Choices = newChoices;
        Assert.Same(newChoices, completion.Choices);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        ChatCompletion original = new(
        [
            new ChatMessage(ChatRole.Assistant, "Choice1"),
            new ChatMessage(ChatRole.Assistant, "Choice2"),
            new ChatMessage(ChatRole.Assistant, "Choice3"),
            new ChatMessage(ChatRole.Assistant, "Choice4"),
        ])
        {
            CompletionId = "id",
            ModelId = "modelId",
            CreatedAt = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero),
            FinishReason = ChatFinishReason.ContentFilter,
            Usage = new UsageDetails(),
            RawRepresentation = new(),
            AdditionalProperties = new() { ["key"] = "value" },
        };

        string json = JsonSerializer.Serialize(original, TestJsonSerializerContext.Default.ChatCompletion);

        ChatCompletion? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ChatCompletion);

        Assert.NotNull(result);
        Assert.Equal(4, result.Choices.Count);

        for (int i = 0; i < original.Choices.Count; i++)
        {
            Assert.Equal(ChatRole.Assistant, result.Choices[i].Role);
            Assert.Equal($"Choice{i + 1}", result.Choices[i].Text);
        }

        Assert.Equal("id", result.CompletionId);
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
}
