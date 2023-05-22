// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

/// <summary>
/// Process native methods class.
/// </summary>
/// <remarks>This will not be covered by UTs, as those
/// classes have insufficient and inconsistent privileges,
/// depending on runtime environment.</remarks>
[ExcludeFromCodeCoverage]
internal static class ProcessInfo
{
    private enum PROCESS_INFORMATION_CLASS
    {
        ProcessAppMemoryInfo = 2
    }

    /// <summary>
    /// The APP_MEMORY_INFORMATION structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct APP_MEMORY_INFORMATION
    {
        public ulong AvailableCommit;
        public ulong PrivateCommitUsage;
        public ulong PeakPrivateCommitUsage;
        public ulong TotalCommitUsage;
    }

    /// <summary>
    /// Retrieve the current application memory information.
    /// </summary>
    /// <returns>An appropriate memory data structure.</returns>
    public static APP_MEMORY_INFORMATION GetCurrentAppMemoryInfo()
    {
        unsafe
        {
            APP_MEMORY_INFORMATION info = default;
            void* buffer = &info;
            using var currentProcess = Process.GetCurrentProcess();
            NtGetProcessInformation(
                currentProcess.Handle,
                PROCESS_INFORMATION_CLASS.ProcessAppMemoryInfo,
                buffer,
                sizeof(APP_MEMORY_INFORMATION));

            return info;
        }
    }

    /// <summary>
    /// Get process information.
    /// </summary>
    /// <param name="handle">The handle of the object to query.</param>
    /// <param name="infoClass">Process info class.</param>
    /// <param name="buffer">Buffer containing the limit.</param>
    /// <param name="size">Buffer size.</param>
    private static unsafe void NtGetProcessInformation(IntPtr handle, PROCESS_INFORMATION_CLASS infoClass, void* buffer, int size)
    {
        if (!UnsafeNativeMethods.GetProcessInformation(
            handle,
            infoClass,
            buffer,
            size))
        {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
    }

    private static class UnsafeNativeMethods
    {
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static unsafe extern bool GetProcessInformation(
            IntPtr processHandle,
            PROCESS_INFORMATION_CLASS processInformationClass,
            void* processInformation,
            int processInformationSize);
    }
}
