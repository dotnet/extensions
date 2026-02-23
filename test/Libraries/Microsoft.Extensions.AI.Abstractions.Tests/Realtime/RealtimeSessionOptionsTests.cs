// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Microsoft.Extensions.AI;

public class RealtimeSessionOptionsTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        RealtimeSessionOptions options = new();

        Assert.Equal(RealtimeSessionKind.Realtime, options.SessionKind);
        Assert.Null(options.Model);
        Assert.Null(options.InputAudioFormat);
        Assert.Null(options.NoiseReductionOptions);
        Assert.Null(options.TranscriptionOptions);
        Assert.Null(options.VoiceActivityDetection);
        Assert.Null(options.OutputAudioFormat);
        Assert.Equal(1.0, options.VoiceSpeed);
        Assert.Null(options.Voice);
        Assert.Null(options.Instructions);
        Assert.Null(options.MaxOutputTokens);
        Assert.Null(options.OutputModalities);
        Assert.Null(options.ToolMode);
        Assert.Null(options.Tools);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        var inputFormat = new RealtimeAudioFormat("audio/pcm", 16000);
        var outputFormat = new RealtimeAudioFormat("audio/pcm", 24000);
        List<string> modalities = ["text", "audio"];
        List<AITool> tools = [AIFunctionFactory.Create(() => 42)];
        var transcriptionOptions = new TranscriptionOptions { SpeechLanguage = "en", ModelId = "whisper-1", Prompt = "greeting" };
        var vad = new VoiceActivityDetection { CreateResponse = true, InterruptResponse = true };

        RealtimeSessionOptions options = new()
        {
            SessionKind = RealtimeSessionKind.Transcription,
            Model = "gpt-4-realtime",
            InputAudioFormat = inputFormat,
            OutputAudioFormat = outputFormat,
            NoiseReductionOptions = NoiseReductionOptions.NearField,
            TranscriptionOptions = transcriptionOptions,
            VoiceActivityDetection = vad,
            VoiceSpeed = 1.5,
            Voice = "alloy",
            Instructions = "Be helpful",
            MaxOutputTokens = 500,
            OutputModalities = modalities,
            ToolMode = ChatToolMode.Auto,
            Tools = tools,
        };

        Assert.Equal(RealtimeSessionKind.Transcription, options.SessionKind);
        Assert.Equal("gpt-4-realtime", options.Model);
        Assert.Same(inputFormat, options.InputAudioFormat);
        Assert.Same(outputFormat, options.OutputAudioFormat);
        Assert.Equal(NoiseReductionOptions.NearField, options.NoiseReductionOptions);
        Assert.Same(transcriptionOptions, options.TranscriptionOptions);
        Assert.Same(vad, options.VoiceActivityDetection);
        Assert.Equal(1.5, options.VoiceSpeed);
        Assert.Equal("alloy", options.Voice);
        Assert.Equal("Be helpful", options.Instructions);
        Assert.Equal(500, options.MaxOutputTokens);
        Assert.Same(modalities, options.OutputModalities);
        Assert.Equal(ChatToolMode.Auto, options.ToolMode);
        Assert.Same(tools, options.Tools);
    }

    [Fact]
    public void TranscriptionOptions_Properties_Roundtrip()
    {
        var options = new TranscriptionOptions { SpeechLanguage = "en", ModelId = "whisper-1", Prompt = "greeting" };

        Assert.Equal("en", options.SpeechLanguage);
        Assert.Equal("whisper-1", options.ModelId);
        Assert.Equal("greeting", options.Prompt);

        options.SpeechLanguage = "fr";
        options.ModelId = "whisper-2";
        options.Prompt = null;

        Assert.Equal("fr", options.SpeechLanguage);
        Assert.Equal("whisper-2", options.ModelId);
        Assert.Null(options.Prompt);
    }

    [Fact]
    public void TranscriptionOptions_PromptDefaultsToNull()
    {
        var options = new TranscriptionOptions { SpeechLanguage = "en", ModelId = "whisper-1" };
        Assert.Null(options.Prompt);
    }

    [Fact]
    public void VoiceActivityDetection_Properties_Roundtrip()
    {
        var vad = new VoiceActivityDetection();

        Assert.False(vad.CreateResponse);
        Assert.False(vad.InterruptResponse);

        var vad2 = new VoiceActivityDetection { CreateResponse = true, InterruptResponse = true };

        Assert.True(vad2.CreateResponse);
        Assert.True(vad2.InterruptResponse);
    }
}
