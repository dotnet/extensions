// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Indicates that an <see cref="IChatClient"/> should not request the invocation of any tools.
/// </summary>
/// <remarks>
/// Use <see cref="ChatToolMode.None"/> to get an instance of <see cref="NoneChatToolMode"/>.
/// </remarks>
[DebuggerDisplay("None")]
public sealed class NoneChatToolMode : ChatToolMode
{
    /// <summary>Initializes a new instance of the <see cref="NoneChatToolMode"/> class.</summary>
    /// <remarks>Use <see cref="ChatToolMode.None"/> to get an instance of <see cref="NoneChatToolMode"/>.</remarks>
    public NoneChatToolMode()
    {
    } // must exist in support of polymorphic deserialization of a ChatToolMode

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is NoneChatToolMode;

    /// <inheritdoc/>
    public override int GetHashCode() => typeof(NoneChatToolMode).GetHashCode();
}
