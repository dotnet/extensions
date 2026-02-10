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
        Assert.Null(options.ToolChoiceMode);
        Assert.Null(options.AIFunction);
        Assert.Null(options.HostedMcpServerTool);
        Assert.Null(options.Tools);
        Assert.False(options.EnableAutoTracing);
        Assert.Null(options.TracingGroupId);
        Assert.Null(options.TracingWorkflowName);
        Assert.Null(options.TracingMetadata);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        RealtimeSessionOptions options = new();

        var inputFormat = new RealtimeAudioFormat("audio/pcm", 16000);
        var outputFormat = new RealtimeAudioFormat("audio/pcm", 24000);
        List<string> modalities = ["text", "audio"];
        List<AITool> tools = [AIFunctionFactory.Create(() => 42)];
        var tracingMetadata = new { key = "value" };
        var transcriptionOptions = new TranscriptionOptions("en", "whisper-1", "greeting");
        var vad = new VoiceActivityDetection { CreateResponse = true, InterruptResponse = true };

        options.SessionKind = RealtimeSessionKind.Transcription;
        options.Model = "gpt-4-realtime";
        options.InputAudioFormat = inputFormat;
        options.OutputAudioFormat = outputFormat;
        options.NoiseReductionOptions = NoiseReductionOptions.NearField;
        options.TranscriptionOptions = transcriptionOptions;
        options.VoiceActivityDetection = vad;
        options.VoiceSpeed = 1.5;
        options.Voice = "alloy";
        options.Instructions = "Be helpful";
        options.MaxOutputTokens = 500;
        options.OutputModalities = modalities;
        options.ToolChoiceMode = ToolChoiceMode.Auto;
        options.Tools = tools;
        options.EnableAutoTracing = true;
        options.TracingGroupId = "group-1";
        options.TracingWorkflowName = "workflow-1";
        options.TracingMetadata = tracingMetadata;

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
        Assert.Equal(ToolChoiceMode.Auto, options.ToolChoiceMode);
        Assert.Same(tools, options.Tools);
        Assert.True(options.EnableAutoTracing);
        Assert.Equal("group-1", options.TracingGroupId);
        Assert.Equal("workflow-1", options.TracingWorkflowName);
        Assert.Same(tracingMetadata, options.TracingMetadata);
    }

    [Fact]
    public void TranscriptionOptions_Properties_Roundtrip()
    {
        var options = new TranscriptionOptions("en", "whisper-1", "greeting");

        Assert.Equal("en", options.Language);
        Assert.Equal("whisper-1", options.Model);
        Assert.Equal("greeting", options.Prompt);

        options.Language = "fr";
        options.Model = "whisper-2";
        options.Prompt = null;

        Assert.Equal("fr", options.Language);
        Assert.Equal("whisper-2", options.Model);
        Assert.Null(options.Prompt);
    }

    [Fact]
    public void TranscriptionOptions_PromptDefaultsToNull()
    {
        var options = new TranscriptionOptions("en", "whisper-1");
        Assert.Null(options.Prompt);
    }

    [Fact]
    public void VoiceActivityDetection_Properties_Roundtrip()
    {
        var vad = new VoiceActivityDetection();

        Assert.False(vad.CreateResponse);
        Assert.False(vad.InterruptResponse);

        vad.CreateResponse = true;
        vad.InterruptResponse = true;

        Assert.True(vad.CreateResponse);
        Assert.True(vad.InterruptResponse);
    }
}
