// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents options for configuring a real-time session.</summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public class RealtimeSessionOptions
{
    /// <summary>
    /// Gets the session kind.
    /// </summary>
    /// <remarks>
    /// If set to <see cref="RealtimeSessionKind.Transcription"/>, most of the sessions properties will not apply to the session. Only InputAudioFormat, NoiseReductionOptions, TranscriptionOptions, and VoiceActivityDetection will be used.
    /// </remarks>
    public RealtimeSessionKind SessionKind { get; init; } = RealtimeSessionKind.Realtime;

    /// <summary>
    /// Gets the model name to use for the session.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Gets the input audio format for the session.
    /// </summary>
    public RealtimeAudioFormat? InputAudioFormat { get; init; }

    /// <summary>
    /// Gets the noise reduction options for the session.
    /// </summary>
    public NoiseReductionOptions? NoiseReductionOptions { get; init; }

    /// <summary>
    /// Gets the transcription options for the session.
    /// </summary>
    public TranscriptionOptions? TranscriptionOptions { get; init; }

    /// <summary>
    /// Gets the voice activity detection options for the session.
    /// </summary>
    public VoiceActivityDetection? VoiceActivityDetection { get; init; }

    /// <summary>
    /// Gets the output audio format for the session.
    /// </summary>
    public RealtimeAudioFormat? OutputAudioFormat { get; init; }

    /// <summary>
    /// Gets the output voice speed for the session.
    /// </summary>
    /// <remarks>
    /// The default value is 1.0, which represents normal speed.
    /// </remarks>
    public double VoiceSpeed { get; init; } = 1.0;

    /// <summary>
    /// Gets the output voice for the session.
    /// </summary>
    public string? Voice { get; init; }

    /// <summary>
    /// Gets the default system instructions for the session.
    /// </summary>
    public string? Instructions { get; init; }

    /// <summary>
    /// Gets the maximum number of response tokens for the session.
    /// </summary>
    public int? MaxOutputTokens { get; init; }

    /// <summary>
    /// Gets the output modalities for the response. like "text", "audio".
    /// If null, then default conversation modalities will be used.
    /// </summary>
    public IReadOnlyList<string>? OutputModalities { get; init; }

    /// <summary>
    /// Gets the tool choice mode for the session.
    /// </summary>
    public ChatToolMode? ToolMode { get; init; }

    /// <summary>
    /// Gets the AI tools available for generating the response.
    /// </summary>
    public IReadOnlyList<AITool>? Tools { get; init; }

    /// <summary>
    /// Gets a callback responsible for creating the raw representation of the session options from an underlying implementation.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="IRealtimeSession" /> implementation might have its own representation of options.
    /// When <see cref="IRealtimeSession.UpdateAsync" /> is invoked with a <see cref="RealtimeSessionOptions" />,
    /// that implementation might convert the provided options into its own representation in order to use it while
    /// performing the operation. For situations where a consumer knows which concrete <see cref="IRealtimeSession" />
    /// is being used and how it represents options, a new instance of that implementation-specific options type can be
    /// returned by this callback for the <see cref="IRealtimeSession" /> implementation to use, instead of creating a
    /// new instance. Such implementations might mutate the supplied options instance further based on other settings
    /// supplied on this <see cref="RealtimeSessionOptions" /> instance or from other inputs.
    /// Therefore, it is <b>strongly recommended</b> to not return shared instances and instead make the callback return
    /// a new instance on each call.
    /// This is typically used to set an implementation-specific setting that isn't otherwise exposed from the strongly typed
    /// properties on <see cref="RealtimeSessionOptions" />.
    /// </remarks>
    [JsonIgnore]
    public Func<IRealtimeSession, object?>? RawRepresentationFactory { get; init; }
}
