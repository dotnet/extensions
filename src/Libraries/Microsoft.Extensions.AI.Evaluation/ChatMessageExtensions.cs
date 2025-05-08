// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// Extension methods for <see cref="ChatMessage"/>.
/// </summary>
public static class ChatMessageExtensions
{
    /// <summary>
    /// Given a collection of <paramref name="messages"/> representing an LLM chat conversation, returns a
    /// single <see cref="ChatMessage"/> representing the last <paramref name="userRequest"/> in this conversation.
    /// </summary>
    /// <param name="messages">
    /// A collection of <see cref="ChatMessage"/>s representing an LLM chat conversation history.
    /// </param>
    /// <param name="userRequest">
    /// Returns the last <see cref="ChatMessage"/> in the supplied collection of <paramref name="messages"/> if this
    /// last <see cref="ChatMessage"/> has <see cref="ChatMessage.Role"/> set to <see cref="ChatRole.User"/>;
    /// <see langword="null" /> otherwise.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the last <see cref="ChatMessage"/> in the supplied collection of
    /// <paramref name="messages"/> has <see cref="ChatMessage.Role"/> set to <see cref="ChatRole.User"/>;
    /// <see langword="false"/> otherwise.
    /// </returns>
    public static bool TryGetUserRequest(
        this IEnumerable<ChatMessage> messages,
        [NotNullWhen(true)] out ChatMessage? userRequest)
    {
        userRequest =
            messages.LastOrDefault() is ChatMessage lastMessage && lastMessage.Role == ChatRole.User
                ? lastMessage
                : null;

        return userRequest is not null;
    }

    /// <summary>
    /// Decomposes the supplied collection of <paramref name="messages"/> representing an LLM chat conversation into a
    /// single <see cref="ChatMessage"/> representing the last <paramref name="userRequest"/> in this conversation and
    /// a collection of <paramref name="remainingMessages"/> representing the rest of the conversation history.
    /// </summary>
    /// <param name="messages">
    /// A collection of <see cref="ChatMessage"/>s representing an LLM chat conversation history.
    /// </param>
    /// <param name="userRequest">
    /// Returns the last <see cref="ChatMessage"/> in the supplied collection of <paramref name="messages"/> if this
    /// last <see cref="ChatMessage"/> has <see cref="ChatMessage.Role"/> set to <see cref="ChatRole.User"/>;
    /// <see langword="null" /> otherwise.
    /// </param>
    /// <param name="remainingMessages">
    /// Returns the remaining <see cref="ChatMessage"/>s in the conversation history excluding
    /// <paramref name="userRequest"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the last <see cref="ChatMessage"/> in the supplied collection of
    /// <paramref name="messages"/> has <see cref="ChatMessage.Role"/> set to <see cref="ChatRole.User"/>;
    /// <see langword="false"/> otherwise.
    /// </returns>
    public static bool TryGetUserRequest(
        this IEnumerable<ChatMessage> messages,
        [NotNullWhen(true)] out ChatMessage? userRequest,
        out IReadOnlyList<ChatMessage> remainingMessages)
    {
        List<ChatMessage> conversationHistory = [.. messages];
        int lastMessageIndex = conversationHistory.Count - 1;

        if (lastMessageIndex >= 0 &&
            conversationHistory[lastMessageIndex] is ChatMessage lastMessage &&
            lastMessage.Role == ChatRole.User)
        {
            userRequest = lastMessage;
            conversationHistory.RemoveAt(lastMessageIndex);
        }
        else
        {
            userRequest = null;
        }

        remainingMessages = conversationHistory;
        return userRequest is not null;
    }

    /// <summary>
    /// Renders the supplied <paramref name="message"/> to a <see langword="string"/>. The returned
    /// <see langword="string"/> can used as part of constructing an evaluation prompt to evaluate a conversation
    /// that includes the supplied <paramref name="message"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This function only considers the <see cref="ChatMessage.Text"/> and ignores any <see cref="AIContent"/>s
    /// (present within the <see cref="ChatMessage.Contents"/> of the <paramref name="message"/>) that are not
    /// <see cref="TextContent"/>s. If the <paramref name="message"/> does not contain any <see cref="TextContent"/>s
    /// then this function returns an empty string.
    /// </para>
    /// <para>
    /// The returned string is prefixed with the <see cref="ChatMessage.Role"/> and
    /// <see cref="ChatMessage.AuthorName"/> (if available). The returned string also always has a new line character
    /// at the end.
    /// </para>
    /// </remarks>
    /// <param name="message">The <see cref="ChatMessage"/> that is to be rendered.</param>
    /// <returns>A <see langword="string"/> containing the rendered <paramref name="message"/>.</returns>
    public static string RenderText(this ChatMessage message)
    {
        _ = Throw.IfNull(message);

        if (!message.Contents.OfType<TextContent>().Any())
        {
            // Don't render messages (such as messages with role ChatRole.Tool) that don't contain any textual content.
            return string.Empty;
        }

        string? author = message.AuthorName;
        string role = message.Role.Value;
        string? content = message.Text;

        return string.IsNullOrWhiteSpace(author)
            ? $"[{role}] {content}\n"
            : $"[{author} ({role})] {content}\n";
    }

    /// <summary>
    /// Renders the supplied <paramref name="messages"/> to a <see langword="string"/>.The returned
    /// <see langword="string"/> can used as part of constructing an evaluation prompt to evaluate a conversation
    /// that includes the supplied <paramref name="messages"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This function only considers the <see cref="ChatMessage.Text"/> and ignores any <see cref="AIContent"/>s
    /// (present within the <see cref="ChatMessage.Contents"/> of the <paramref name="messages"/>) that are not
    /// <see cref="TextContent"/>s. Any <paramref name="messages"/> that contain no <see cref="TextContent"/>s will be
    /// skipped and will not be rendered. If none of the <paramref name="messages"/> include any
    /// <see cref="TextContent"/>s then this function will return an empty string.
    /// </para>
    /// <para>
    /// The rendered <paramref name="messages"/> are each prefixed with the <see cref="ChatMessage.Role"/> and
    /// <see cref="ChatMessage.AuthorName"/> (if available) in the returned string. The rendered
    /// <see cref="ChatMessage"/>s are also always separated by new line characters in the returned string.
    /// </para>
    /// </remarks>
    /// <param name="messages">The <see cref="ChatMessage"/>s that are to be rendered.</param>
    /// <returns>A <see langword="string"/> containing the rendered <paramref name="messages"/>.</returns>
    public static string RenderText(this IEnumerable<ChatMessage> messages)
    {
        _ = Throw.IfNull(messages);

        var builder = new StringBuilder();
        foreach (ChatMessage message in messages)
        {
            string renderedMessage = message.RenderText();
            _ = builder.Append(renderedMessage);
        }

        return builder.ToString();
    }
}
