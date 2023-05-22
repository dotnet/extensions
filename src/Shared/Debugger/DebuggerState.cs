// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1716
namespace Microsoft.Shared.Diagnostics;
#pragma warning restore CA1716

/// <summary>
/// Holds all debugger states useful for test writing.
/// </summary>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

internal static class DebuggerState
{
    /// <summary>
    /// Gets a debugger dynamically changing its state depending on environment.
    /// </summary>
    public static IDebuggerState System => SystemDebugger.Instance;

    /// <summary>
    /// Gets always attached debugger.
    /// </summary>
    public static IDebuggerState Attached => AttachedDebugger.Instance;

    /// <summary>
    /// Gets always detached debugger.
    /// </summary>
    public static IDebuggerState Detached => DetachedDebugger.Instance;
}
