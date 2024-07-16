// Copyright (c) Microsoft Corporation. All Rights Reserved.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;

#if NET8_0_OR_GREATER
using DllImportAttr = System.Runtime.InteropServices.LibraryImportAttribute; // We trigger source-gen on .NET 7 and above
#else
using DllImportAttr = System.Runtime.InteropServices.DllImportAttribute;
#endif

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;

internal sealed partial class MemoryInfo
{
    private static partial class SafeNativeMethods
    {
        /// <summary>
        /// GlobalMemoryStatusEx.
        /// </summary>
        /// <param name="memoryStatus">Memory Status structure.</param>
        /// <returns>Success or failure.</returns>
        [DllImportAttr("kernel32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static unsafe
#if NET8_0_OR_GREATER
            partial
#else
            extern
#endif
            BOOL GlobalMemoryStatusEx(MEMORYSTATUSEX* memoryStatus);
    }
}
