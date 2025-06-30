// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents text reasoning content in a chat.
/// </summary>
/// <remarks>
/// <see cref="TextReasoningContent"/> is distinct from <see cref="TextContent"/>. <see cref="TextReasoningContent"/>
/// represents "thinking" or "reasoning" performed by the model and is distinct from the actual output text from
/// the model, which is represented by <see cref="TextContent"/>. Neither types derives from the other.
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class TextReasoningContent : AIContent
{
    private string? _text;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextReasoningContent"/> class.
    /// </summary>
    /// <param name="text">The text reasoning content.</param>
    public TextReasoningContent(string? text)
    {
        _text = text;
    }

    /// <summary>
    /// Gets or sets the text reasoning content.
    /// </summary>
    [AllowNull]
    public string Text
    {
        get => _text ?? string.Empty;
        set => _text = value;
    }

    /// <inheritdoc/>
    public override string ToString() => Text;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"Reasoning = \"{Text}\"";
}
