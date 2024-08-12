// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;

/// <summary>
/// An interface to enable the mocking of memory usage information retrieval.
/// </summary>
internal interface IProcessInfo
{
    /// <summary>
    /// Retrieves the amount of memory, in bytes, used by the current process.
    /// </summary>
    /// <returns>The number of bytes allocated by the current process.</returns>
    ulong GetCurrentProcessMemoryUsage();

    /// <summary>
    /// Retrieves the amount of memory, in bytes, used by the system.
    /// </summary>
    /// <returns>The number of bytes allocated by the system.</returns>
    ulong GetMemoryUsage();
}
