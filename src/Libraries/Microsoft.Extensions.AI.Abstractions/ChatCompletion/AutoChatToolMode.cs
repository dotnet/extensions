// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Indicates that an <see cref="IChatClient"/> is free to select any of the available tools, or none at all.
/// </summary>
/// <remarks>
/// Use <see cref="ChatToolMode.Auto"/> to get an instance of <see cref="AutoChatToolMode"/>.
/// </remarks>
[DebuggerDisplay("Auto")]
public sealed class AutoChatToolMode : ChatToolMode
{
    /// <summary>Initializes a new instance of the <see cref="AutoChatToolMode"/> class.</summary>
    /// <remarks>Use <see cref="ChatToolMode.Auto"/> to get an instance of <see cref="AutoChatToolMode"/>.</remarks>
    public AutoChatToolMode()
    {
    } // must exist in support of polymorphic deserialization of a ChatToolMode

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is AutoChatToolMode;

    /// <inheritdoc/>
    public override int GetHashCode() => typeof(AutoChatToolMode).GetHashCode();
}
