// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents an OpenAI chat completion request deserialized as Microsoft.Extension.AI models.
/// </summary>
public sealed class OpenAIChatCompletionRequest
{
    /// <summary>
    /// Gets the chat messages specified in the completion request.
    /// </summary>
    public required IList<ChatMessage> Messages { get; init; }

    /// <summary>
    /// Gets the chat options governing the completion request.
    /// </summary>
    public required ChatOptions Options { get; init; }

    /// <summary>
    /// Gets a value indicating whether the completion response should be streamed.
    /// </summary>
    public bool Stream { get; init; }

    /// <summary>
    /// Gets the model id requested by the chat completion.
    /// </summary>
    public string? ModelId { get; init; }
}
