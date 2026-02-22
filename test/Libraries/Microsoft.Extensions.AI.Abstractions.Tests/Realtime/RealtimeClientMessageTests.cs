// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Microsoft.Extensions.AI;

public class RealtimeClientMessageTests
{
    [Fact]
    public void RealtimeClientMessage_DefaultProperties()
    {
        var message = new RealtimeClientMessage();

        Assert.Null(message.EventId);
        Assert.Null(message.RawRepresentation);
    }

    [Fact]
    public void RealtimeClientMessage_Properties_Roundtrip()
    {
        var rawObj = new object();
        var message = new RealtimeClientMessage
        {
            EventId = "evt_001",
            RawRepresentation = rawObj,
        };

        Assert.Equal("evt_001", message.EventId);
        Assert.Same(rawObj, message.RawRepresentation);
    }

    [Fact]
    public void ConversationItemCreateMessage_Constructor_SetsProperties()
    {
        var contents = new List<AIContent> { new TextContent("Hello") };
        var item = new RealtimeContentItem(contents, "item_1", ChatRole.User);

        var message = new RealtimeClientConversationItemCreateMessage(item, "prev_1");

        Assert.Same(item, message.Item);
        Assert.Equal("prev_1", message.PreviousId);
    }

    [Fact]
    public void ConversationItemCreateMessage_Constructor_PreviousIdDefaults()
    {
        var item = new RealtimeContentItem([new TextContent("Hello")]);
        var message = new RealtimeClientConversationItemCreateMessage(item);

        Assert.Same(item, message.Item);
        Assert.Null(message.PreviousId);
    }

    [Fact]
    public void ConversationItemCreateMessage_Properties_Roundtrip()
    {
        var item = new RealtimeContentItem([new TextContent("Hello")]);
        var message = new RealtimeClientConversationItemCreateMessage(item);

        var newItem = new RealtimeContentItem([new TextContent("World")]);
        message.Item = newItem;
        message.PreviousId = "prev_2";

        Assert.Same(newItem, message.Item);
        Assert.Equal("prev_2", message.PreviousId);
    }

    [Fact]
    public void ConversationItemCreateMessage_InheritsClientMessage()
    {
        var item = new RealtimeContentItem([new TextContent("Hello")]);
        var message = new RealtimeClientConversationItemCreateMessage(item)
        {
            EventId = "evt_create_1",
        };

        Assert.Equal("evt_create_1", message.EventId);
        Assert.IsAssignableFrom<RealtimeClientMessage>(message);
    }

    [Fact]
    public void InputAudioBufferAppendMessage_Constructor_SetsContent()
    {
        var audioContent = new DataContent(new byte[] { 1, 2, 3 }, "audio/pcm");
        var message = new RealtimeClientInputAudioBufferAppendMessage(audioContent);

        Assert.Same(audioContent, message.Content);
    }

    [Fact]
    public void InputAudioBufferAppendMessage_Properties_Roundtrip()
    {
        var audioContent = new DataContent(new byte[] { 1, 2, 3 }, "audio/pcm");
        var message = new RealtimeClientInputAudioBufferAppendMessage(audioContent);

        var newContent = new DataContent(new byte[] { 4, 5, 6 }, "audio/wav");
        message.Content = newContent;

        Assert.Same(newContent, message.Content);
    }

    [Fact]
    public void InputAudioBufferAppendMessage_InheritsClientMessage()
    {
        var audioContent = new DataContent(new byte[] { 1, 2, 3 }, "audio/pcm");
        var message = new RealtimeClientInputAudioBufferAppendMessage(audioContent)
        {
            EventId = "evt_append_1",
        };

        Assert.Equal("evt_append_1", message.EventId);
        Assert.IsAssignableFrom<RealtimeClientMessage>(message);
    }

    [Fact]
    public void InputAudioBufferCommitMessage_Constructor()
    {
        var message = new RealtimeClientInputAudioBufferCommitMessage();

        Assert.IsAssignableFrom<RealtimeClientMessage>(message);
        Assert.Null(message.EventId);
    }

    [Fact]
    public void ResponseCreateMessage_DefaultProperties()
    {
        var message = new RealtimeClientResponseCreateMessage();

        Assert.Null(message.Items);
        Assert.Null(message.OutputAudioOptions);
        Assert.Null(message.OutputVoice);
        Assert.False(message.ExcludeFromConversation);
        Assert.Null(message.Instructions);
        Assert.Null(message.MaxOutputTokens);
        Assert.Null(message.Metadata);
        Assert.Null(message.OutputModalities);
        Assert.Null(message.ToolMode);
        Assert.Null(message.Tools);
    }

    [Fact]
    public void ResponseCreateMessage_Properties_Roundtrip()
    {
        var message = new RealtimeClientResponseCreateMessage();

        var items = new List<RealtimeContentItem>
        {
            new RealtimeContentItem([new TextContent("Hello")], "item_1", ChatRole.User),
        };
        var audioFormat = new RealtimeAudioFormat("audio/pcm", 16000);
        var modalities = new List<string> { "text", "audio" };
        var tools = new List<AITool> { AIFunctionFactory.Create(() => 42) };
        var metadata = new AdditionalPropertiesDictionary { ["key"] = "value" };

        message.Items = items;
        message.OutputAudioOptions = audioFormat;
        message.OutputVoice = "alloy";
        message.ExcludeFromConversation = true;
        message.Instructions = "Be brief";
        message.MaxOutputTokens = 100;
        message.Metadata = metadata;
        message.OutputModalities = modalities;
        message.ToolMode = ChatToolMode.Auto;
        message.Tools = tools;

        Assert.Same(items, message.Items);
        Assert.Same(audioFormat, message.OutputAudioOptions);
        Assert.Equal("alloy", message.OutputVoice);
        Assert.True(message.ExcludeFromConversation);
        Assert.Equal("Be brief", message.Instructions);
        Assert.Equal(100, message.MaxOutputTokens);
        Assert.Same(metadata, message.Metadata);
        Assert.Same(modalities, message.OutputModalities);
        Assert.Equal(ChatToolMode.Auto, message.ToolMode);
        Assert.Same(tools, message.Tools);
    }

    [Fact]
    public void ResponseCreateMessage_InheritsClientMessage()
    {
        var message = new RealtimeClientResponseCreateMessage
        {
            EventId = "evt_resp_1",
            RawRepresentation = "raw",
        };

        Assert.Equal("evt_resp_1", message.EventId);
        Assert.Equal("raw", message.RawRepresentation);
        Assert.IsAssignableFrom<RealtimeClientMessage>(message);
    }
}
