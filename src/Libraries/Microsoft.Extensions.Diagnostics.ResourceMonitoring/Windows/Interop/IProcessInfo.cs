// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;

/// <summary>
/// An interface to enable the mocking of memory usage information retrieval.
/// </summary>
internal interface IProcessInfo
{
    /// <summary>
    /// Retrieve the memory usage of a system.
    /// </summary>
    /// <returns>Memory usage amount in bytes.</returns>
    ulong GetMemoryUsage();
}
