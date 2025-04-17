// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.ML.Tokenizers;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// Extension methods for <see cref="ChatMessage"/>.
/// </summary>
public static class ChatMessageExtensions
{
    /// <summary>
    /// Uses the supplied <paramref name="tokenizer"/> to count the number of tokens present in the supplied
    /// <paramref name="conversation"/>, and discards the oldest messages (from the beginning of the collection), if
    /// necessary, to ensure that the remaining messages fit within the specified <paramref name="tokenBudget"/>.
    /// </summary>
    /// <remarks>
    /// Note that this API only considers the text (i.e., <see cref="TextContent"/>) present in the supplied
    /// <paramref name="conversation"/>. It does not consider other modalities such as images or audio.
    /// </remarks>
    /// <param name="conversation">
    /// A collection of <see cref="ChatMessage"/>s representing an LLM conversation history, with the oldest messages
    /// appearing towards the beginning of the collection, and the newest ones appearing towards the end.
    /// </param>
    /// <param name="tokenizer">
    /// The <see cref="Tokenizer"/> to be used to count the number of tokens present in each message in the supplied
    /// <paramref name="conversation"/>.
    /// </param>
    /// <param name="tokenBudget">The overall budget for the number of tokens available.</param>
    /// <returns>
    /// A <see cref="Tuple{T1, T2}"/> that contains the collection of messages that were retained after trimming down
    /// the supplied <paramref name="conversation"/> to fit within the specified <paramref name="tokenBudget"/>, as
    /// well as an <see langword="int"/> count identifying the remaining number of tokens available after this trimming
    /// operation.
    /// </returns>
    [Experimental("EVAL001")]
    public static (IEnumerable<ChatMessage> trimmedConversation, int remainingTokenBudget) Trim(
        this IEnumerable<ChatMessage> conversation,
        Tokenizer tokenizer,
        int tokenBudget)
    {
        _ = Throw.IfNull(conversation);
        _ = Throw.IfNull(tokenizer);
        _ = Throw.IfLessThan(tokenBudget, min: 0);

        var trimmedConversation = new List<ChatMessage>();
        int remainingTokenBudget = tokenBudget;

        foreach (ChatMessage message in conversation.Reverse())
        {
            int tokenCount = tokenizer.CountTokens(message.Text);
            int newTokenBudget = remainingTokenBudget - tokenCount;

            if (newTokenBudget < 0)
            {
                break;
            }
            else
            {
                remainingTokenBudget = newTokenBudget;
                trimmedConversation.Add(message);
            }
        }

        return (trimmedConversation, remainingTokenBudget);
    }
}
