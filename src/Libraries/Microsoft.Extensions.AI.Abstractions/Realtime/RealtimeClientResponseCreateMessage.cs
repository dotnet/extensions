// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time message for creating a response item.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public class RealtimeClientResponseCreateMessage : RealtimeClientMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RealtimeClientResponseCreateMessage"/> class.
    /// </summary>
    public RealtimeClientResponseCreateMessage()
    {
    }

    /// <summary>
    /// Gets or sets the list of the conversation items to create a response for.
    /// </summary>
    public IList<RealtimeContentItem>? Items { get; set; }

    /// <summary>
    /// Gets or sets the output audio options for the response.
    /// </summary>
    /// <remarks>
    /// If set, overrides the session-level audio output configuration for this response only.
    /// If <see langword="null"/>, the session's default audio options are used.
    /// </remarks>
    public RealtimeAudioFormat? OutputAudioOptions { get; set; }

    /// <summary>
    /// Gets or sets the voice of the output audio.
    /// </summary>
    /// <remarks>
    /// If set, overrides the session-level voice for this response only.
    /// If <see langword="null"/>, the session's default voice is used.
    /// </remarks>
    public string? OutputVoice { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the response output should be excluded from the conversation context.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, the response is generated out-of-band: the model produces output
    /// but the resulting items are not added to the conversation history, so they will not appear
    /// as context for subsequent responses. Defaults to <see langword="false"/>, meaning response
    /// output is added to the default conversation.
    /// </remarks>
    public bool ExcludeFromConversation { get; set; }

    /// <summary>
    /// Gets or sets the instructions that guide the model on desired responses.
    /// </summary>
    /// <remarks>
    /// If set, overrides the session-level instructions for this response only.
    /// If <see langword="null"/>, the session's default instructions are used.
    /// </remarks>
    public string? Instructions { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of output tokens for the response, inclusive of all modalities and tool calls.
    /// </summary>
    /// <remarks>
    /// This limit applies to the total output tokens regardless of modality (text, audio, etc.).
    /// If <see langword="null"/>, the provider's default limit is used.
    /// </remarks>
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// Gets or sets any additional properties associated with the response request.
    /// </summary>
    /// <remarks>
    /// This can be used to attach arbitrary key-value metadata to a response request
    /// for tracking or disambiguation purposes (e.g., correlating multiple simultaneous responses).
    /// Providers may map this to their own metadata fields.
    /// </remarks>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>
    /// Gets or sets the output modalities for the response (e.g., "text", "audio").
    /// </summary>
    /// <remarks>
    /// If set, overrides the session-level output modalities for this response only.
    /// If <see langword="null"/>, the session's default modalities are used.
    /// </remarks>
    public IList<string>? OutputModalities { get; set; }

    /// <summary>
    /// Gets or sets the tool choice mode for the response.
    /// </summary>
    /// <remarks>
    /// If set, overrides the session-level tool choice for this response only.
    /// If <see langword="null"/>, the session's default tool choice is used.
    /// </remarks>
    public ChatToolMode? ToolMode { get; set; }

    /// <summary>
    /// Gets or sets the AI tools available for generating the response.
    /// </summary>
    /// <remarks>
    /// If set, overrides the session-level tools for this response only.
    /// If <see langword="null"/>, the session's default tools are used.
    /// </remarks>
    public IList<AITool>? Tools { get; set; }
}
