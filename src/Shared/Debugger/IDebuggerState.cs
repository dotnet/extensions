// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1716
namespace Microsoft.Shared.Diagnostics;
#pragma warning restore CA1716

/// <summary>
/// Abstracts debugger presence to increase testability.
/// </summary>
internal interface IDebuggerState
{
    /// <summary>
    /// Gets a value indicating whether a debugger is attached or not.
    /// </summary>
    bool IsAttached { get; }
}
