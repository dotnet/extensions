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

    /// <summary>Gets or sets an optional opaque blob of data associated with this reasoning content.</summary>
    /// <remarks>
    /// <para>
    /// This property is used to store data from a provider that should be roundtripped back to the provider but that is not
    /// intended for human consumption. It is often encrypted or otherwise redacted information that is only intended to be
    /// sent back to the provider and not displayed to the user. It's possible for a <see cref="TextReasoningContent"/> to contain
    /// only <see cref="ProtectedData"/> and have an empty <see cref="Text"/> property. This data also may be associated with
    /// the corresponding <see cref="Text"/>, acting as a validation signature for it.
    /// </para>
    /// <para>
    /// Note that whereas <see cref="Text"/> can be provider agnostic, <see cref="ProtectedData"/>
    /// is provider-specific, and is likely to only be understood by the provider that created it.
    /// </para>
    /// </remarks>
    public string? ProtectedData { get; set; }

    /// <inheritdoc/>
    public override string ToString() => Text;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"Reasoning = \"{Text}\"";
}
