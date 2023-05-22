// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

/// <summary>
/// An interface to enable the mocking of system information retrieval.
/// </summary>
internal interface ISystemInfo
{
    /// <summary>
    /// Get the system info.
    /// </summary>
    /// <returns>System information structure.</returns>
    SYSTEM_INFO GetSystemInfo();
}
