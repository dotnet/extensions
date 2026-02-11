// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the type of a real-time response.
/// This is used to identify the response type being received from the model.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public enum RealtimeServerMessageType
{
    /// <summary>
    /// Indicates that the response contains only raw content.
    /// </summary>
    /// <remarks>
    /// This response type is to support extensibility for supporting custom content types not natively supported by the SDK.
    /// </remarks>
    RawContentOnly,

    /// <summary>
    /// Indicates the output of audio transcription for user audio written to the user audio buffer.
    /// </summary>
    /// <remarks>
    /// The type <ref name="RealtimeServerInputAudioTranscriptionMessage"/> is used with this response type.
    /// </remarks>
    InputAudioTranscriptionCompleted,

    /// <summary>
    /// Indicates the text value of an input audio transcription content part is updated with incremental transcription results.
    /// </summary>
    /// <remarks>
    /// The type <ref name="RealtimeServerInputAudioTranscriptionMessage"/> is used with this response type.
    /// </remarks>
    InputAudioTranscriptionDelta,

    /// <summary>
    /// Indicates that the audio transcription for user audio written to the user audio buffer has failed.
    /// </summary>
    /// <remarks>
    /// The type <ref name="RealtimeServerInputAudioTranscriptionMessage"/> is used with this response type.
    /// </remarks>
    InputAudioTranscriptionFailed,

    /// <summary>
    /// Indicates the output text update with incremental results response.
    /// </summary>
    /// <remarks>
    /// The type <ref name="RealtimeServerOutputTextAudioMessage"/> is used with this response type.
    /// </remarks>
    OutputTextDelta,

    /// <summary>
    /// Indicates the output text is complete.
    /// </summary>
    /// <remarks>
    /// The type <ref name="RealtimeServerOutputTextAudioMessage"/> is used with this response type.
    /// </remarks>
    OutputTextDone,

    /// <summary>
    /// Indicates the model-generated transcription of audio output updated.
    /// </summary>
    /// <remarks>
    /// The type <ref name="RealtimeServerOutputTextAudioMessage"/> is used with this response type.
    /// </remarks>
    OutputAudioTranscriptionDelta,

    /// <summary>
    /// Indicates the model-generated transcription of audio output is done streaming.
    /// </summary>
    /// <remarks>
    /// The type <ref name="RealtimeServerOutputTextAudioMessage"/> is used with this response type.
    /// </remarks>
    OutputAudioTranscriptionDone,

    /// <summary>
    /// Indicates the audio output updated.
    /// </summary>
    /// <remarks>
    /// The type <ref name="RealtimeServerOutputTextAudioMessage"/> is used with this response type.
    /// </remarks>
    OutputAudioDelta,

    /// <summary>
    /// Indicates the audio output is done streaming.
    /// </summary>
    /// <remarks>
    /// The type <ref name="RealtimeServerOutputTextAudioMessage"/> is used with this response type.
    /// </remarks>
    OutputAudioDone,

    /// <summary>
    /// Indicates the response has been created.
    /// </summary>
    /// <remarks>
    /// The type <ref name="RealtimeServerResponseCreatedMessage"/> is used with this response type.
    /// </remarks>
    ResponseDone,

    /// <summary>
    /// Indicates the response has been created.
    /// </summary>
    /// <remarks>
    /// The type <ref name="RealtimeServerResponseCreatedMessage"/> is used with this response type.
    /// </remarks>
    ResponseCreated,

    /// <summary>
    /// Indicates an error occurred while processing the request.
    /// </summary>
    /// <remarks>
    /// The type <ref name="RealtimeServerErrorMessage"/> is used with this response type.
    /// </remarks>
    Error,

    /// <summary>
    /// Indicates that an MCP tool call is in progress.
    /// </summary>
    McpCallInProgress,

    /// <summary>
    /// Indicates that an MCP tool call has completed.
    /// </summary>
    McpCallCompleted,

    /// <summary>
    /// Indicates that an MCP tool call has failed.
    /// </summary>
    McpCallFailed,

    /// <summary>
    /// Indicates that listing MCP tools is in progress.
    /// </summary>
    McpListToolsInProgress,

    /// <summary>
    /// Indicates that listing MCP tools has completed.
    /// </summary>
    McpListToolsCompleted,

    /// <summary>
    /// Indicates that listing MCP tools has failed.
    /// </summary>
    McpListToolsFailed,
}
