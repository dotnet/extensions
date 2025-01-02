// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a response format with no constraints around the format.</summary>
/// <remarks>
/// Use <see cref="ChatResponseFormat.Text"/> to get an instance of <see cref="ChatResponseFormatText"/>.
/// </remarks>
[DebuggerDisplay("Text")]
public sealed class ChatResponseFormatText : ChatResponseFormat
{
    /// <summary>Initializes a new instance of the <see cref="ChatResponseFormatText"/> class.</summary>
    /// <remarks> Use <see cref="ChatResponseFormat.Text"/> to get an instance of <see cref="ChatResponseFormatText"/>.</remarks>
    public ChatResponseFormatText()
    {
        // must exist in support of polymorphic deserialization of a ChatResponseFormat
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ChatResponseFormatText;

    /// <inheritdoc/>
    public override int GetHashCode() => typeof(ChatResponseFormatText).GetHashCode();
}
