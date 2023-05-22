// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

#pragma warning disable CA1716
namespace Microsoft.Shared.Diagnostics;
#pragma warning restore CA1716

/// <summary>
/// Debugger with environment dependent state.
/// </summary>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal sealed class SystemDebugger : IDebuggerState
{
    private SystemDebugger()
    {
        // Intentionally left empty.
    }

    /// <summary>
    /// Gets cached instance of <see cref="SystemDebugger"/>.
    /// </summary>
    public static SystemDebugger Instance { get; } = new();

    /// <inheritdoc/>
    public bool IsAttached => Debugger.IsAttached;
}
