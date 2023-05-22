// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1716
namespace Microsoft.Shared.Diagnostics;
#pragma warning restore CA1716

/// <summary>
/// Always attached debugger.
/// </summary>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal sealed class AttachedDebugger : IDebuggerState
{
    private AttachedDebugger()
    {
        // Intentionally left empty.
    }

    /// <summary>
    /// Gets cached instance of <see cref="AttachedDebugger"/>.
    /// </summary>
    public static AttachedDebugger Instance { get; } = new();

    /// <inheritdoc/>
    public bool IsAttached => true;
}
