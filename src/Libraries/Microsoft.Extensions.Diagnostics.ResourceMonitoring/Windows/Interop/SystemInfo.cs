// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;

/// <summary>
/// Native system interop methods.
/// </summary>
internal sealed class SystemInfo : ISystemInfo
{
    /// <summary>
    /// Get the system info.
    /// </summary>
    /// <returns>System information structure.</returns>
    public SYSTEM_INFO GetSystemInfo()
    {
        SYSTEM_INFO info = default;
        SafeNativeMethods.GetSystemInfo(ref info);
        return info;
    }

    private static class SafeNativeMethods
    {
        /// <summary>
        /// Import of GetSystemInfo win32 function.
        /// </summary>
        /// <param name="s">SYSTEM_INFO struct to fill.</param>
        [DllImport("kernel32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern void GetSystemInfo(ref SYSTEM_INFO s);
    }
}
