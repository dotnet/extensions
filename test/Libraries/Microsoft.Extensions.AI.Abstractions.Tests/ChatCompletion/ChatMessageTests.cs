// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ChatMessageTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        ChatMessage message = new();
        Assert.Null(message.AuthorName);
        Assert.Empty(message.Contents);
        Assert.Equal(ChatRole.User, message.Role);
        Assert.Null(message.Text);
        Assert.NotNull(message.Contents);
        Assert.Same(message.Contents, message.Contents);
        Assert.Empty(message.Contents);
        Assert.Null(message.RawRepresentation);
        Assert.Null(message.AdditionalProperties);
        Assert.Equal(string.Empty, message.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("text")]
    public void Constructor_RoleString_PropsRoundtrip(string? text)
    {
        ChatMessage message = new(ChatRole.Assistant, text);

        Assert.Equal(ChatRole.Assistant, message.Role);

        Assert.Same(message.Contents, message.Contents);
        if (text is null)
        {
            Assert.Empty(message.Contents);
        }
        else
        {
            Assert.Single(message.Contents);
            TextContent tc = Assert.IsType<TextContent>(message.Contents[0]);
            Assert.Equal(text, tc.Text);
        }

        Assert.Null(message.AuthorName);
        Assert.Null(message.RawRepresentation);
        Assert.Null(message.AdditionalProperties);
        Assert.Equal(text ?? string.Empty, message.ToString());
    }

    [Fact]
    public void Constructor_RoleList_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("contents", () => new ChatMessage(ChatRole.User, (IList<AIContent>)null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Constructor_RoleList_PropsRoundtrip(int messageCount)
    {
        List<AIContent> content = [];
        for (int i = 0; i < messageCount; i++)
        {
            content.Add(new TextContent($"text-{i}"));
        }

        ChatMessage message = new(ChatRole.System, content);

        Assert.Equal(ChatRole.System, message.Role);

        Assert.Same(message.Contents, message.Contents);
        if (messageCount == 0)
        {
            Assert.Empty(message.Contents);
            Assert.Null(message.Text);
        }
        else
        {
            Assert.Equal(messageCount, message.Contents.Count);
            for (int i = 0; i < messageCount; i++)
            {
                TextContent tc = Assert.IsType<TextContent>(message.Contents[i]);
                Assert.Equal($"text-{i}", tc.Text);
            }

            Assert.Equal("text-0", message.Text);
            Assert.Equal(string.Concat(Enumerable.Range(0, messageCount).Select(i => $"text-{i}")), message.ToString());
        }

        Assert.Null(message.AuthorName);
        Assert.Null(message.RawRepresentation);
        Assert.Null(message.AdditionalProperties);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \r\n\t\v ")]
    public void AuthorName_InvalidArg_UsesNull(string? authorName)
    {
        ChatMessage message = new()
        {
            AuthorName = authorName
        };
        Assert.Null(message.AuthorName);

        message.AuthorName = "author";
        Assert.Equal("author", message.AuthorName);

        message.AuthorName = authorName;
        Assert.Null(message.AuthorName);
    }

    [Fact]
    public void Text_GetSet_UsesFirstTextContent()
    {
        ChatMessage message = new(ChatRole.User,
        [
            new DataContent("http://localhost/audio"),
            new DataContent("http://localhost/image"),
            new FunctionCallContent("callId1", "fc1"),
            new TextContent("text-1"),
            new TextContent("text-2"),
            new FunctionResultContent("callId1", "result"),
        ]);

        TextContent textContent = Assert.IsType<TextContent>(message.Contents[3]);
        Assert.Equal("text-1", textContent.Text);
        Assert.Equal("text-1", message.Text);
        Assert.Equal("text-1text-2", message.ToString());

        message.Text = "text-3";
        Assert.Equal("text-3", message.Text);
        Assert.Equal("text-3", message.Text);
        Assert.Same(textContent, message.Contents[3]);
        Assert.Equal("text-3text-2", message.ToString());
    }

    [Fact]
    public void Text_Set_AddsTextMessageToEmptyList()
    {
        ChatMessage message = new(ChatRole.User, []);
        Assert.Empty(message.Contents);

        message.Text = "text-1";
        Assert.Equal("text-1", message.Text);

        Assert.Single(message.Contents);
        TextContent textContent = Assert.IsType<TextContent>(message.Contents[0]);
        Assert.Equal("text-1", textContent.Text);
    }

    [Fact]
    public void Text_Set_AddsTextMessageToListWithNoText()
    {
        ChatMessage message = new(ChatRole.User,
        [
            new DataContent("http://localhost/audio"),
            new DataContent("http://localhost/image"),
            new FunctionCallContent("callId1", "fc1"),
        ]);
        Assert.Equal(3, message.Contents.Count);

        message.Text = "text-1";
        Assert.Equal("text-1", message.Text);
        Assert.Equal(4, message.Contents.Count);

        message.Text = "text-2";
        Assert.Equal("text-2", message.Text);
        Assert.Equal(4, message.Contents.Count);

        message.Contents.RemoveAt(3);
        Assert.Equal(3, message.Contents.Count);

        message.Text = "text-3";
        Assert.Equal("text-3", message.Text);
        Assert.Equal(4, message.Contents.Count);
    }

    [Fact]
    public void Contents_InitializesToList()
    {
        // This is an implementation detail, but if this test starts failing, we need to ensure
        // tests are in place for whatever possibly-custom implementation of IList is being used.
        Assert.IsType<List<AIContent>>(new ChatMessage().Contents);
    }

    [Fact]
    public void Contents_Roundtrips()
    {
        ChatMessage message = new();
        Assert.Empty(message.Contents);

        List<AIContent> contents = [];
        message.Contents = contents;

        Assert.Same(contents, message.Contents);

        message.Contents = contents;
        Assert.Same(contents, message.Contents);

        message.Contents = null;
        Assert.NotNull(message.Contents);
        Assert.NotSame(contents, message.Contents);
        Assert.Empty(message.Contents);
    }

    [Fact]
    public void RawRepresentation_Roundtrips()
    {
        ChatMessage message = new();
        Assert.Null(message.RawRepresentation);

        object raw = new();

        message.RawRepresentation = raw;
        Assert.Same(raw, message.RawRepresentation);

        message.RawRepresentation = raw;
        Assert.Same(raw, message.RawRepresentation);

        message.RawRepresentation = null;
        Assert.Null(message.RawRepresentation);

        message.RawRepresentation = raw;
        Assert.Same(raw, message.RawRepresentation);
    }

    [Fact]
    public void AdditionalProperties_Roundtrips()
    {
        ChatMessage message = new();
        Assert.Null(message.RawRepresentation);

        AdditionalPropertiesDictionary props = [];

        message.AdditionalProperties = props;
        Assert.Same(props, message.AdditionalProperties);

        message.AdditionalProperties = props;
        Assert.Same(props, message.AdditionalProperties);

        message.AdditionalProperties = null;
        Assert.Null(message.AdditionalProperties);

        message.AdditionalProperties = props;
        Assert.Same(props, message.AdditionalProperties);
    }

    [Fact]
    public void ItCanBeSerializeAndDeserialized()
    {
        // Arrange
        IList<AIContent> items =
        [
            new TextContent("content-1")
            {
                AdditionalProperties = new() { ["metadata-key-1"] = "metadata-value-1" }
            },
            new DataContent(new Uri("https://fake-random-test-host:123"), "mime-type/2")
            {
                AdditionalProperties = new() { ["metadata-key-2"] = "metadata-value-2" }
            },
            new DataContent(new BinaryData(new[] { 1, 2, 3 }, options: TestJsonSerializerContext.Default.Options), "mime-type/3")
            {
                AdditionalProperties = new() { ["metadata-key-3"] = "metadata-value-3" }
            },
            new TextContent("content-4")
            {
                AdditionalProperties = new() { ["metadata-key-4"] = "metadata-value-4" }
            },
            new FunctionCallContent("function-id", "plugin-name-function-name", new Dictionary<string, object?> { ["parameter"] = "argument" }),
            new FunctionResultContent("function-id", "function-result"),
        ];

        // Act
        var chatMessageJson = JsonSerializer.Serialize(new ChatMessage(ChatRole.User, contents: items)
        {
            Text = "content-1-override", // Override the content of the first text content item that has the "content-1" content
            AuthorName = "Fred",
            AdditionalProperties = new() { ["message-metadata-key-1"] = "message-metadata-value-1" },
        }, TestJsonSerializerContext.Default.Options);

        var deserializedMessage = JsonSerializer.Deserialize<ChatMessage>(chatMessageJson, TestJsonSerializerContext.Default.Options)!;

        // Assert
        Assert.Equal("Fred", deserializedMessage.AuthorName);
        Assert.Equal("user", deserializedMessage.Role.Value);
        Assert.NotNull(deserializedMessage.AdditionalProperties);
        Assert.Single(deserializedMessage.AdditionalProperties);
        Assert.Equal("message-metadata-value-1", deserializedMessage.AdditionalProperties["message-metadata-key-1"]?.ToString());

        Assert.NotNull(deserializedMessage.Contents);
        Assert.Equal(items.Count, deserializedMessage.Contents.Count);

        var textContent = deserializedMessage.Contents[0] as TextContent;
        Assert.NotNull(textContent);
        Assert.Equal("content-1-override", textContent.Text);
        Assert.NotNull(textContent.AdditionalProperties);
        Assert.Single(textContent.AdditionalProperties);
        Assert.Equal("metadata-value-1", textContent.AdditionalProperties["metadata-key-1"]?.ToString());

        var dataContent = deserializedMessage.Contents[1] as DataContent;
        Assert.NotNull(dataContent);
        Assert.Equal("https://fake-random-test-host:123/", dataContent.Uri);
        Assert.Equal("mime-type/2", dataContent.MediaType);
        Assert.NotNull(dataContent.AdditionalProperties);
        Assert.Single(dataContent.AdditionalProperties);
        Assert.Equal("metadata-value-2", dataContent.AdditionalProperties["metadata-key-2"]?.ToString());

        dataContent = deserializedMessage.Contents[2] as DataContent;
        Assert.NotNull(dataContent);
        Assert.True(dataContent.Data!.Value.Span.SequenceEqual(new BinaryData(new[] { 1, 2, 3 }, TestJsonSerializerContext.Default.Options)));
        Assert.Equal("mime-type/3", dataContent.MediaType);
        Assert.NotNull(dataContent.AdditionalProperties);
        Assert.Single(dataContent.AdditionalProperties);
        Assert.Equal("metadata-value-3", dataContent.AdditionalProperties["metadata-key-3"]?.ToString());

        textContent = deserializedMessage.Contents[3] as TextContent;
        Assert.NotNull(textContent);
        Assert.Equal("content-4", textContent.Text);
        Assert.NotNull(textContent.AdditionalProperties);
        Assert.Single(textContent.AdditionalProperties);
        Assert.Equal("metadata-value-4", textContent.AdditionalProperties["metadata-key-4"]?.ToString());

        var functionCallContent = deserializedMessage.Contents[4] as FunctionCallContent;
        Assert.NotNull(functionCallContent);
        Assert.Equal("plugin-name-function-name", functionCallContent.Name);
        Assert.Equal("function-id", functionCallContent.CallId);
        Assert.NotNull(functionCallContent.Arguments);
        Assert.Single(functionCallContent.Arguments);
        Assert.Equal("argument", functionCallContent.Arguments["parameter"]?.ToString());

        var functionResultContent = deserializedMessage.Contents[5] as FunctionResultContent;
        Assert.NotNull(functionResultContent);
        Assert.Equal("function-result", functionResultContent.Result?.ToString());
        Assert.Equal("function-id", functionResultContent.CallId);
    }
}
