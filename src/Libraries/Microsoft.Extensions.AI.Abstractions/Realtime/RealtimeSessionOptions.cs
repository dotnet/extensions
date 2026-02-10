// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>Represents options for configuring a real-time session.</summary>
[Experimental("MEAI001")]
public class RealtimeSessionOptions
{
    /// <summary>
    /// Gets or sets the session kind.
    /// </summary>
    /// <remarks>
    /// If set to <see cref="RealtimeSessionKind.Transcription"/>, most of the sessions properties will not apply to the session. Only InputAudioFormat, NoiseReductionOptions, TranscriptionOptions, and VoiceActivityDetection will be used.
    /// </remarks>
    public RealtimeSessionKind SessionKind { get; set; } = RealtimeSessionKind.Realtime;

    /// <summary>
    /// Gets or sets the model name to use for the session.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the input audio format for the session.
    /// </summary>
    public RealtimeAudioFormat? InputAudioFormat { get; set; }

    /// <summary>
    /// Gets or sets the noise reduction options for the session.
    /// </summary>
    public NoiseReductionOptions? NoiseReductionOptions { get; set; }

    /// <summary>
    /// Gets or sets the transcription options for the session.
    /// </summary>
    public TranscriptionOptions? TranscriptionOptions { get; set; }

    /// <summary>
    /// Gets or sets the voice activity detection options for the session.
    /// </summary>
    public VoiceActivityDetection? VoiceActivityDetection { get; set; }

    /// <summary>
    /// Gets or sets the output audio format for the session.
    /// </summary>
    public RealtimeAudioFormat? OutputAudioFormat { get; set; }

    /// <summary>
    /// Gets or sets the output voice speed for the session.
    /// </summary>
    /// <remarks>
    /// The default value is 1.0, which represents normal speed.
    /// </remarks>
    public double VoiceSpeed { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the output voice for the session.
    /// </summary>
    public string? Voice { get; set; }

    /// <summary>
    /// Gets or sets the default system instructions for the session.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of response tokens for the session.
    /// </summary>
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the output modalities for the response. like "text", "audio".
    /// If null, then default conversation modalities will be used.
    /// </summary>
    public IList<string>? OutputModalities { get; set; }

    /// <summary>
    /// Gets or sets the tool choice mode for the response.
    /// </summary>
    /// <remarks>
    /// If FunctionToolName or McpToolName is specified, this value will be ignored.
    /// </remarks>
    public ToolChoiceMode? ToolChoiceMode { get; set; }

    /// <summary>
    /// Gets or sets the AI function to use for the response.
    /// </summary>
    /// <remarks>
    /// If specified, the ToolChoiceMode will be ignored.
    /// </remarks>
    public AIFunction? AIFunction { get; set; }

    /// <summary>
    /// Gets or sets the name of the MCP tool to use for the response.
    /// </summary>
    /// <remarks>
    /// If specified, the ToolChoiceMode will be ignored.
    /// </remarks>
    public HostedMcpServerTool? HostedMcpServerTool { get; set; }

    /// <summary>
    /// Gets or sets the AI tools available for generating the response.
    /// </summary>
    public IList<AITool>? Tools { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable automatic tracing for the session.
    /// if enabled, will create a trace for the session with default values for the workflow name, group id, and metadata.
    /// </summary>
    public bool EnableAutoTracing { get; set; }

    /// <summary>
    /// Gets or sets the group ID for tracing.
    /// </summary>
    /// <remarks>
    /// This property is only used if <see cref="EnableAutoTracing"/> is not set to true.
    /// </remarks>
    public string? TracingGroupId { get; set; }

    /// <summary>
    /// Gets or sets the workflow name for tracing.
    /// </summary>
    /// <remarks>
    /// This property is only used if <see cref="EnableAutoTracing"/> is not set to true.
    /// </remarks>
    public string? TracingWorkflowName { get; set; }

    /// <summary>
    /// Gets or sets arbitrary metadata to attach to this trace to enable filtering.
    /// </summary>
    /// <remarks>
    /// This property is only used if <see cref="EnableAutoTracing"/> is not set to true.
    /// </remarks>
    public object? TracingMetadata { get; set; }
}
