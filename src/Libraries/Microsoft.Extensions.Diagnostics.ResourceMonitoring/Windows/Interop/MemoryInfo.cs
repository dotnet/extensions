// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;

/// <summary>
/// Native memory interop methods.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed partial class MemoryInfo : IMemoryInfo
{
    /// <summary>
    /// Get the memory status of the host.
    /// </summary>
    /// <returns>Memory status information.</returns>
    public unsafe MEMORYSTATUSEX GetMemoryStatus()
    {
        MEMORYSTATUSEX info = default;
        info.Length = (uint)sizeof(MEMORYSTATUSEX);
        if (SafeNativeMethods.GlobalMemoryStatusEx(&info) != BOOL.TRUE)
        {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        return info;
    }

    private static partial class SafeNativeMethods
    {
    }
}
