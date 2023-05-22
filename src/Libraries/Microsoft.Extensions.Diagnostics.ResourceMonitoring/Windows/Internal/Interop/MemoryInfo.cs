// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

/// <summary>
/// Native memory interop methods.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class MemoryInfo : IMemoryInfo
{
    internal MemoryInfo()
    {
    }

    /// <summary>
    /// Get the memory status of the host.
    /// </summary>
    /// <returns>Memory status information.</returns>
    public unsafe MEMORYSTATUSEX GetMemoryStatus()
    {
        MEMORYSTATUSEX info = default;
        info.Length = (uint)sizeof(MEMORYSTATUSEX);
        if (!SafeNativeMethods.GlobalMemoryStatusEx(ref info))
        {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        return info;
    }

    private static class SafeNativeMethods
    {
        /// <summary>
        /// GlobalMemoryStatusEx.
        /// </summary>
        /// <param name="memoryStatus">Memory Status structure.</param>
        /// <returns>Success or failure.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX memoryStatus);
    }
}
