// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents text content in a chat.
/// </summary>
public sealed class TextContent : AIContent
{
    private string? _text;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextContent"/> class.
    /// </summary>
    /// <param name="text">The text content.</param>
    public TextContent(string? text)
    {
        _text = text;
    }

    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    [AllowNull]
    public string Text
    {
        get => _text ?? string.Empty;
        set => _text = value;
    }

    /// <inheritdoc/>
    public override string ToString() => Text;
}
