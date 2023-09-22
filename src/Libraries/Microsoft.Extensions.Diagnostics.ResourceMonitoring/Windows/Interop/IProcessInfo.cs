// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop.ProcessInfo;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;

/// <summary>
/// An interface to enable the mocking of process information retrieval.
/// </summary>
internal interface IProcessInfo
{
    /// <summary>
    /// Retrieve the current application memory information.
    /// </summary>
    /// <returns>An appropriate memory data structure.</returns>
    APP_MEMORY_INFORMATION GetCurrentAppMemoryInfo();
}
