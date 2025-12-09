// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides a chat reducer that limits the number of non-system messages in a conversation to a specified maximum
/// count, preserving the most recent messages and the first system message if present.
/// </summary>
/// <remarks>
/// This reducer is useful for scenarios where it is necessary to constrain the size of a chat history,
/// such as when preparing input for models with context length limits. The reducer always includes the first
/// encountered system message, if any, and then retains up to the specified number of the most recent non-system
/// messages. Messages containing function call or function result content are excluded from the reduced
/// output.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.ChatReduction, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class MessageCountingChatReducer : IChatReducer
{
    private readonly int _targetCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageCountingChatReducer"/> class.
    /// </summary>
    /// <param name="targetCount">The maximum number of non-system messages to retain in the reduced output.</param>
    public MessageCountingChatReducer(int targetCount)
    {
        _targetCount = Throw.IfLessThanOrEqual(targetCount, min: 0);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(messages);
        return Task.FromResult(GetReducedMessages(messages));
    }

    private IEnumerable<ChatMessage> GetReducedMessages(IEnumerable<ChatMessage> messages)
    {
        ChatMessage? systemMessage = null;
        Queue<ChatMessage> reducedMessages = new(capacity: _targetCount);

        foreach (var message in messages)
        {
            if (message.Role == ChatRole.System)
            {
                systemMessage ??= message;
            }
            else if (!message.Contents.Any(m => m is FunctionCallContent or FunctionResultContent))
            {
                if (reducedMessages.Count >= _targetCount)
                {
                    _ = reducedMessages.Dequeue();
                }

                reducedMessages.Enqueue(message);
            }
        }

        if (systemMessage is not null)
        {
            yield return systemMessage;
        }

        while (reducedMessages.Count > 0)
        {
            yield return reducedMessages.Dequeue();
        }
    }
}
