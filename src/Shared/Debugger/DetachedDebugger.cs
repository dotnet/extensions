// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1716
namespace Microsoft.Shared.Diagnostics;
#pragma warning restore CA1716

/// <summary>
/// Always detached debugger.
/// </summary>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal sealed class DetachedDebugger : IDebuggerState
{
    private DetachedDebugger()
    {
        // Intentionally left empty.
    }

    /// <summary>
    /// Gets cached instance of <see cref="DetachedDebugger"/>.
    /// </summary>
    public static DetachedDebugger Instance { get; } = new();

    /// <inheritdoc/>
    public bool IsAttached => false;
}
