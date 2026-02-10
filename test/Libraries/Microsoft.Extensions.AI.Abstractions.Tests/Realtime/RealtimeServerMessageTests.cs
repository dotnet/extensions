// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Microsoft.Extensions.AI;

public class RealtimeServerMessageTests
{
    [Fact]
    public void RealtimeServerMessage_DefaultProperties()
    {
        var message = new RealtimeServerMessage();

        Assert.Equal(default, message.Type);
        Assert.Null(message.EventId);
        Assert.Null(message.RawRepresentation);
    }

    [Fact]
    public void RealtimeServerMessage_Properties_Roundtrip()
    {
        var rawObj = new object();
        var message = new RealtimeServerMessage
        {
            Type = RealtimeServerMessageType.ResponseDone,
            EventId = "evt_001",
            RawRepresentation = rawObj,
        };

        Assert.Equal(RealtimeServerMessageType.ResponseDone, message.Type);
        Assert.Equal("evt_001", message.EventId);
        Assert.Same(rawObj, message.RawRepresentation);
    }

    [Fact]
    public void ErrorMessage_Constructor_SetsType()
    {
        var message = new RealtimeServerErrorMessage();

        Assert.Equal(RealtimeServerMessageType.Error, message.Type);
    }

    [Fact]
    public void ErrorMessage_DefaultProperties()
    {
        var message = new RealtimeServerErrorMessage();

        Assert.Null(message.Error);
        Assert.Null(message.ErrorEventId);
        Assert.Null(message.Parameter);
    }

    [Fact]
    public void ErrorMessage_Properties_Roundtrip()
    {
        var error = new ErrorContent("Test error");
        var message = new RealtimeServerErrorMessage
        {
            Error = error,
            ErrorEventId = "evt_bad",
            Parameter = "temperature",
            EventId = "evt_err_1",
        };

        Assert.Same(error, message.Error);
        Assert.Equal("evt_bad", message.ErrorEventId);
        Assert.Equal("temperature", message.Parameter);
        Assert.Equal("evt_err_1", message.EventId);
        Assert.IsAssignableFrom<RealtimeServerMessage>(message);
    }

    [Fact]
    public void InputAudioTranscriptionMessage_Constructor_SetsType()
    {
        var message = new RealtimeServerInputAudioTranscriptionMessage(
            RealtimeServerMessageType.InputAudioTranscriptionCompleted);

        Assert.Equal(RealtimeServerMessageType.InputAudioTranscriptionCompleted, message.Type);
    }

    [Fact]
    public void InputAudioTranscriptionMessage_DefaultProperties()
    {
        var message = new RealtimeServerInputAudioTranscriptionMessage(
            RealtimeServerMessageType.InputAudioTranscriptionDelta);

        Assert.Null(message.ContentIndex);
        Assert.Null(message.ItemId);
        Assert.Null(message.Transcription);
        Assert.Null(message.Usage);
        Assert.Null(message.Error);
    }

    [Fact]
    public void InputAudioTranscriptionMessage_Properties_Roundtrip()
    {
        var usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 20 };
        var error = new ErrorContent("transcription error");

        var message = new RealtimeServerInputAudioTranscriptionMessage(
            RealtimeServerMessageType.InputAudioTranscriptionCompleted)
        {
            ContentIndex = 0,
            ItemId = "item_audio_1",
            Transcription = "Hello world",
            Usage = usage,
            Error = error,
        };

        Assert.Equal(0, message.ContentIndex);
        Assert.Equal("item_audio_1", message.ItemId);
        Assert.Equal("Hello world", message.Transcription);
        Assert.Same(usage, message.Usage);
        Assert.Same(error, message.Error);
        Assert.IsAssignableFrom<RealtimeServerMessage>(message);
    }

    [Fact]
    public void OutputTextAudioMessage_Constructor_SetsType()
    {
        var message = new RealtimeServerOutputTextAudioMessage(RealtimeServerMessageType.OutputTextDelta);

        Assert.Equal(RealtimeServerMessageType.OutputTextDelta, message.Type);
    }

    [Fact]
    public void OutputTextAudioMessage_DefaultProperties()
    {
        var message = new RealtimeServerOutputTextAudioMessage(RealtimeServerMessageType.OutputTextDelta);

        Assert.Null(message.ContentIndex);
        Assert.Null(message.Text);
        Assert.Null(message.ItemId);
        Assert.Null(message.OutputIndex);
        Assert.Null(message.ResponseId);
    }

    [Fact]
    public void OutputTextAudioMessage_Properties_Roundtrip()
    {
        var message = new RealtimeServerOutputTextAudioMessage(RealtimeServerMessageType.OutputTextDone)
        {
            ContentIndex = 0,
            Text = "Hello there!",
            ItemId = "item_text_1",
            OutputIndex = 0,
            ResponseId = "resp_1",
        };

        Assert.Equal(RealtimeServerMessageType.OutputTextDone, message.Type);
        Assert.Equal(0, message.ContentIndex);
        Assert.Equal("Hello there!", message.Text);
        Assert.Equal("item_text_1", message.ItemId);
        Assert.Equal(0, message.OutputIndex);
        Assert.Equal("resp_1", message.ResponseId);
        Assert.IsAssignableFrom<RealtimeServerMessage>(message);
    }

    [Fact]
    public void ResponseCreatedMessage_Constructor_SetsType()
    {
        var message = new RealtimeServerResponseCreatedMessage(RealtimeServerMessageType.ResponseCreated);

        Assert.Equal(RealtimeServerMessageType.ResponseCreated, message.Type);
    }

    [Fact]
    public void ResponseCreatedMessage_DefaultProperties()
    {
        var message = new RealtimeServerResponseCreatedMessage(RealtimeServerMessageType.ResponseDone);

        Assert.Null(message.OutputAudioOptions);
        Assert.Null(message.OutputVoice);
        Assert.Null(message.ConversationId);
        Assert.Null(message.ResponseId);
        Assert.Null(message.MaxOutputTokens);
        Assert.Null(message.Metadata);
        Assert.Null(message.Items);
        Assert.Null(message.OutputModalities);
        Assert.Null(message.Status);
        Assert.Null(message.Error);
        Assert.Null(message.Usage);
    }

    [Fact]
    public void ResponseCreatedMessage_Properties_Roundtrip()
    {
        var audioFormat = new RealtimeAudioFormat("audio/pcm", 24000);
        var metadata = new AdditionalPropertiesDictionary { ["key"] = "value" };
        var items = new List<RealtimeContentItem>
        {
            new RealtimeContentItem([new TextContent("response")], "item_1"),
        };
        var modalities = new List<string> { "text" };
        var error = new ErrorContent("response error");
        var usage = new UsageDetails { InputTokenCount = 15, OutputTokenCount = 25, TotalTokenCount = 40 };

        var message = new RealtimeServerResponseCreatedMessage(RealtimeServerMessageType.ResponseDone)
        {
            OutputAudioOptions = audioFormat,
            OutputVoice = "alloy",
            ConversationId = "conv_1",
            ResponseId = "resp_1",
            MaxOutputTokens = 1000,
            Metadata = metadata,
            Items = items,
            OutputModalities = modalities,
            Status = "completed",
            Error = error,
            Usage = usage,
        };

        Assert.Same(audioFormat, message.OutputAudioOptions);
        Assert.Equal("alloy", message.OutputVoice);
        Assert.Equal("conv_1", message.ConversationId);
        Assert.Equal("resp_1", message.ResponseId);
        Assert.Equal(1000, message.MaxOutputTokens);
        Assert.Same(metadata, message.Metadata);
        Assert.Same(items, message.Items);
        Assert.Same(modalities, message.OutputModalities);
        Assert.Equal("completed", message.Status);
        Assert.Same(error, message.Error);
        Assert.Same(usage, message.Usage);
        Assert.IsAssignableFrom<RealtimeServerMessage>(message);
    }

    [Fact]
    public void ResponseOutputItemMessage_Constructor_SetsType()
    {
        var message = new RealtimeServerResponseOutputItemMessage(RealtimeServerMessageType.ResponseDone);

        Assert.Equal(RealtimeServerMessageType.ResponseDone, message.Type);
    }

    [Fact]
    public void ResponseOutputItemMessage_DefaultProperties()
    {
        var message = new RealtimeServerResponseOutputItemMessage(RealtimeServerMessageType.ResponseCreated);

        Assert.Null(message.ResponseId);
        Assert.Null(message.OutputIndex);
        Assert.Null(message.Item);
    }

    [Fact]
    public void ResponseOutputItemMessage_Properties_Roundtrip()
    {
        var item = new RealtimeContentItem([new TextContent("output")], "item_out_1", ChatRole.Assistant);

        var message = new RealtimeServerResponseOutputItemMessage(RealtimeServerMessageType.ResponseDone)
        {
            ResponseId = "resp_1",
            OutputIndex = 0,
            Item = item,
        };

        Assert.Equal("resp_1", message.ResponseId);
        Assert.Equal(0, message.OutputIndex);
        Assert.Same(item, message.Item);
        Assert.IsAssignableFrom<RealtimeServerMessage>(message);
    }
}
