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
    /// Gets or sets the output audio options for the response. If null, the default conversation audio options will be used.
    /// </summary>
    public RealtimeAudioFormat? OutputAudioOptions { get; set; }

    /// <summary>
    /// Gets or sets the voice of the output audio.
    /// </summary>
    public string? OutputVoice { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the response should be excluded from the conversation history.
    /// </summary>
    public bool ExcludeFromConversation { get; set; }

    /// <summary>
    /// Gets or sets the instructions allows the client to guide the model on desired responses.
    /// If null, the default conversation instructions will be used.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of output tokens for the response.
    /// </summary>
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
    /// Gets or sets the output modalities for the response. like "text", "audio".
    /// If null, then default conversation modalities will be used.
    /// </summary>
    public IList<string>? OutputModalities { get; set; }

    /// <summary>
    /// Gets or sets the tool choice mode for the response.
    /// </summary>
    public ChatToolMode? ToolMode { get; set; }

    /// <summary>
    /// Gets or sets the AI tools available for generating the response.
    /// </summary>
    public IList<AITool>? Tools { get; set; }
}
