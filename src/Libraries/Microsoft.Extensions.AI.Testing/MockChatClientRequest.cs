// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents one request invocation captured by <see cref="MockChatClient"/>.
/// </summary>
/// <remarks>
/// Instances are created by <see cref="MockChatClient"/> when
/// <see cref="IChatClient.GetResponseAsync(IEnumerable{ChatMessage}, ChatOptions?, System.Threading.CancellationToken)"/> or
/// <see cref="IChatClient.GetStreamingResponseAsync(IEnumerable{ChatMessage}, ChatOptions?, System.Threading.CancellationToken)"/>
/// is called.
/// </remarks>
public sealed class MockChatClientRequest
{
    /// <summary>Initializes a new instance of the <see cref="MockChatClientRequest"/> class.</summary>
    /// <param name="messages">The request messages in call order.</param>
    /// <param name="options">The request options, if any.</param>
    /// <param name="isStreaming">
    /// <see langword="true"/> when the request came from
    /// <see cref="IChatClient.GetStreamingResponseAsync(IEnumerable{ChatMessage}, ChatOptions?, System.Threading.CancellationToken)"/>.
    /// </param>
    /// <exception cref="System.ArgumentNullException"><paramref name="messages"/> is <see langword="null"/>.</exception>
    public MockChatClientRequest(IReadOnlyList<ChatMessage> messages, ChatOptions? options, bool isStreaming)
    {
        Messages = Throw.IfNull(messages);
        Options = options;
        IsStreaming = isStreaming;
    }

    /// <summary>Gets the request messages.</summary>
    /// <remarks>
    /// For requests recorded by <see cref="MockChatClient"/>, each message is cloned when captured so later changes
    /// to original message instances do not affect stored request history.
    /// </remarks>
    public IReadOnlyList<ChatMessage> Messages { get; }

    /// <summary>Gets the request options.</summary>
    public ChatOptions? Options { get; }

    /// <summary>
    /// Gets a value indicating whether this request originated from the streaming chat API.
    /// </summary>
    public bool IsStreaming { get; }

    /// <summary>
    /// Gets the text of the last user message in <see cref="Messages"/>, if available.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> when there is no user message, or when the last user message has no text.
    /// </remarks>
    public string? LastUserText =>
        Messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text;
}
