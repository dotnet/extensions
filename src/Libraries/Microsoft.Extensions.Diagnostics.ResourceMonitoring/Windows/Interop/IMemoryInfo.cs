// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;

/// <summary>
/// An interface to enable the mocking of memory information retrieval.
/// </summary>
internal interface IMemoryInfo
{
    /// <summary>
    /// Get the memory status of the host.
    /// </summary>
    /// <returns>Memory status information.</returns>
    MEMORYSTATUSEX GetMemoryStatus();
}
