// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ChatResponseUpdateTests
{
    [Fact]
    public void Constructor_PropsDefaulted()
    {
        ChatResponseUpdate update = new();
        Assert.Null(update.AuthorName);
        Assert.Null(update.Role);
        Assert.Null(update.Text);
        Assert.Empty(update.Contents);
        Assert.Null(update.RawRepresentation);
        Assert.Null(update.AdditionalProperties);
        Assert.Null(update.ResponseId);
        Assert.Null(update.CreatedAt);
        Assert.Null(update.FinishReason);
        Assert.Equal(0, update.ChoiceIndex);
        Assert.Equal(string.Empty, update.ToString());
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        ChatResponseUpdate update = new();

        Assert.Null(update.AuthorName);
        update.AuthorName = "author";
        Assert.Equal("author", update.AuthorName);

        Assert.Null(update.Role);
        update.Role = ChatRole.Assistant;
        Assert.Equal(ChatRole.Assistant, update.Role);

        Assert.Empty(update.Contents);
        update.Contents.Add(new TextContent("text"));
        Assert.Single(update.Contents);
        Assert.Equal("text", update.Text);
        Assert.Same(update.Contents, update.Contents);
        IList<AIContent> newList = [new TextContent("text")];
        update.Contents = newList;
        Assert.Same(newList, update.Contents);
        update.Contents = null;
        Assert.NotNull(update.Contents);
        Assert.Empty(update.Contents);

        Assert.Null(update.Text);
        update.Text = "text";
        Assert.Equal("text", update.Text);

        Assert.Null(update.RawRepresentation);
        object raw = new();
        update.RawRepresentation = raw;
        Assert.Same(raw, update.RawRepresentation);

        Assert.Null(update.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { ["key"] = "value" };
        update.AdditionalProperties = props;
        Assert.Same(props, update.AdditionalProperties);

        Assert.Null(update.ResponseId);
        update.ResponseId = "id";
        Assert.Equal("id", update.ResponseId);

        Assert.Null(update.CreatedAt);
        update.CreatedAt = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), update.CreatedAt);

        Assert.Equal(0, update.ChoiceIndex);
        update.ChoiceIndex = 42;
        Assert.Equal(42, update.ChoiceIndex);

        Assert.Null(update.FinishReason);
        update.FinishReason = ChatFinishReason.ContentFilter;
        Assert.Equal(ChatFinishReason.ContentFilter, update.FinishReason);
    }

    [Fact]
    public void Text_GetSet_UsesFirstTextContent()
    {
        ChatResponseUpdate update = new()
        {
            Role = ChatRole.User,
            Contents =
            [
                new DataContent("http://localhost/audio"),
                new DataContent("http://localhost/image"),
                new FunctionCallContent("callId1", "fc1"),
                new TextContent("text-1"),
                new TextContent("text-2"),
                new FunctionResultContent("callId1", "result"),
            ],
        };

        TextContent textContent = Assert.IsType<TextContent>(update.Contents[3]);
        Assert.Equal("text-1", textContent.Text);
        Assert.Equal("text-1", update.Text);
        Assert.Equal("text-1text-2", update.ToString());

        update.Text = "text-3";
        Assert.Equal("text-3", update.Text);
        Assert.Equal("text-3", update.Text);
        Assert.Same(textContent, update.Contents[3]);
        Assert.Equal("text-3text-2", update.ToString());
    }

    [Fact]
    public void Text_Set_AddsTextMessageToEmptyList()
    {
        ChatResponseUpdate update = new()
        {
            Role = ChatRole.User,
        };
        Assert.Empty(update.Contents);

        update.Text = "text-1";
        Assert.Equal("text-1", update.Text);

        Assert.Single(update.Contents);
        TextContent textContent = Assert.IsType<TextContent>(update.Contents[0]);
        Assert.Equal("text-1", textContent.Text);
    }

    [Fact]
    public void Text_Set_AddsTextMessageToListWithNoText()
    {
        ChatResponseUpdate update = new()
        {
            Contents =
            [
                new DataContent("http://localhost/audio"),
                new DataContent("http://localhost/image"),
                new FunctionCallContent("callId1", "fc1"),
            ]
        };
        Assert.Equal(3, update.Contents.Count);

        update.Text = "text-1";
        Assert.Equal("text-1", update.Text);
        Assert.Equal(4, update.Contents.Count);

        update.Text = "text-2";
        Assert.Equal("text-2", update.Text);
        Assert.Equal(4, update.Contents.Count);

        update.Contents.RemoveAt(3);
        Assert.Equal(3, update.Contents.Count);

        update.Text = "text-3";
        Assert.Equal("text-3", update.Text);
        Assert.Equal(4, update.Contents.Count);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        ChatResponseUpdate original = new()
        {
            AuthorName = "author",
            Role = ChatRole.Assistant,
            Contents =
            [
                new TextContent("text-1"),
                new DataContent("http://localhost/image"),
                new FunctionCallContent("callId1", "fc1"),
                new DataContent("data"u8.ToArray()),
                new TextContent("text-2"),
            ],
            RawRepresentation = new object(),
            ResponseId = "id",
            CreatedAt = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero),
            FinishReason = ChatFinishReason.ContentFilter,
            AdditionalProperties = new() { ["key"] = "value" },
            ChoiceIndex = 42,
        };

        string json = JsonSerializer.Serialize(original, TestJsonSerializerContext.Default.ChatResponseUpdate);

        ChatResponseUpdate? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ChatResponseUpdate);

        Assert.NotNull(result);
        Assert.Equal(5, result.Contents.Count);

        Assert.IsType<TextContent>(result.Contents[0]);
        Assert.Equal("text-1", ((TextContent)result.Contents[0]).Text);

        Assert.IsType<DataContent>(result.Contents[1]);
        Assert.Equal("http://localhost/image", ((DataContent)result.Contents[1]).Uri);

        Assert.IsType<FunctionCallContent>(result.Contents[2]);
        Assert.Equal("fc1", ((FunctionCallContent)result.Contents[2]).Name);

        Assert.IsType<DataContent>(result.Contents[3]);
        Assert.Equal("data"u8.ToArray(), ((DataContent)result.Contents[3]).Data?.ToArray());

        Assert.IsType<TextContent>(result.Contents[4]);
        Assert.Equal("text-2", ((TextContent)result.Contents[4]).Text);

        Assert.Equal("author", result.AuthorName);
        Assert.Equal(ChatRole.Assistant, result.Role);
        Assert.Equal("id", result.ResponseId);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), result.CreatedAt);
        Assert.Equal(ChatFinishReason.ContentFilter, result.FinishReason);
        Assert.Equal(42, result.ChoiceIndex);

        Assert.NotNull(result.AdditionalProperties);
        Assert.Single(result.AdditionalProperties);
        Assert.True(result.AdditionalProperties.TryGetValue("key", out object? value));
        Assert.IsType<JsonElement>(value);
        Assert.Equal("value", ((JsonElement)value!).GetString());
    }
}
