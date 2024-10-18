// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents text content in a chat.
/// </summary>
public sealed class TextContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextContent"/> class.
    /// </summary>
    /// <param name="text">The text content.</param>
    public TextContent(string? text)
    {
        Text = text;
    }

    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    public string? Text { get; set; }

    /// <inheritdoc/>
    public override string ToString() => Text ?? string.Empty;
}
