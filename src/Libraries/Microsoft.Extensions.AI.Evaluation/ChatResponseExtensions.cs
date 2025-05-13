// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// Extension methods for <see cref="ChatResponse"/>.
/// </summary>
public static class ChatResponseExtensions
{
    /// <summary>
    /// Renders the supplied <paramref name="response"/> to a <see langword="string"/>. The returned
    /// <see langword="string"/> can used as part of constructing an evaluation prompt to evaluate a conversation
    /// that includes the supplied <paramref name="response"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This function only considers the <see cref="ChatResponse.Text"/> and ignores any <see cref="AIContent"/>s
    /// (present within the <see cref="ChatMessage.Contents"/> of the <see cref="ChatResponse.Messages"/>) that are not
    /// <see cref="TextContent"/>s. Any <see cref="ChatResponse.Messages"/> that contain no <see cref="TextContent"/>s
    /// will be skipped and will not be rendered. If none of the <see cref="ChatResponse.Messages"/> include any
    /// <see cref="TextContent"/>s then this function will return an empty string.
    /// </para>
    /// <para>
    /// The rendered <see cref="ChatResponse.Messages"/> are each prefixed with the <see cref="ChatMessage.Role"/> and
    /// <see cref="ChatMessage.AuthorName"/> (if available) in the returned string. The rendered
    /// <see cref="ChatResponse.Messages"/>s are also always separated by new line characters in the returned string.
    /// </para>
    /// </remarks>
    /// <param name="response">The <see cref="ChatResponse"/> that is to be rendered.</param>
    /// <returns>A <see langword="string"/> containing the rendered <paramref name="response"/>.</returns>
    public static string RenderText(this ChatResponse response)
    {
        _ = Throw.IfNull(response);

        return response.Messages.RenderText();
    }
}
