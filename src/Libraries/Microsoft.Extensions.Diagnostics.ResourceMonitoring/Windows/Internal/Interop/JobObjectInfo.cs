// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
#if NETFRAMEWORK
using System.Runtime.ConstrainedExecution;
#endif
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

/// <summary>
/// JobObject class.
/// </summary>
/// <remarks>
/// This will not be covered by UTs, as those classes have insufficient and inconsistent privileges,
/// depending on runtime environment.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static class JobObjectInfo
{
    /// <summary>
    /// Job object info <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/ms686216(v=vs.85).aspx" >class</see>.
    /// </summary>
    [SuppressMessage("Design", "CA1027:Mark enums with FlagsAttribute", Justification = "Analyzer is confused")]
    public enum JOBOBJECTINFOCLASS
    {
        JobObjectBasicAccountingInformation = 1,
        JobObjectExtendedLimitInformation = 9,
        JobObjectCpuRateControlInformation = 15,
    }

    /// <summary>
    /// Job object CPU rate control limit <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/hh448384(v=vs.85).aspx">flags</see>.
    /// </summary>
    [Flags]
    public enum JobCpuRateControlLimit : uint
    {
        CpuRateControlEnable = 1,
        CpuRateControlHardCap = 4,
    }

    /// <summary>
    /// I/O counters which are part of the JOBOBJECT_EXTENDED_LIMIT_INFORMATION structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct IO_COUNTERS
    {
        /// <summary>ReadOperationCount field.</summary>
        public ulong ReadOperationCount;

        /// <summary>WriteOperationCount field.</summary>
        public ulong WriteOperationCount;

        /// <summary>OtherOperationCount field.</summary>
        public ulong OtherOperationCount;

        /// <summary>ReadTransferCount field.</summary>
        public ulong ReadTransferCount;

        /// <summary>WriteTransferCount field.</summary>
        public ulong WriteTransferCount;

        /// <summary>OtherTransferCount field.</summary>
        public ulong OtherTransferCount;
    }

    /// <summary>
    /// The job object basic limit information <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/ms684147(v=vs.85).aspx">structure</see>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        /// <summary>PerProcessUserTimeLimit field.</summary>
        public long PerProcessUserTimeLimit;

        /// <summary>PerJobUserTimeLimit field.</summary>
        public long PerJobUserTimeLimit;

        /// <summary>LimitFlags field.</summary>
        public uint LimitFlags;

        /// <summary>MinimumWorkingSetSize field.</summary>
        public UIntPtr MinimumWorkingSetSize;

        /// <summary>MaximumWorkingSetSize field.</summary>
        public UIntPtr MaximumWorkingSetSize;

        /// <summary>ActiveProcessLimit field.</summary>
        public uint ActiveProcessLimit;

        /// <summary>Affinity field.</summary>
        public UIntPtr Affinity;

        /// <summary>PriorityClass field.</summary>
        public uint PriorityClass;

        /// <summary>SchedulingClass field.</summary>
        public uint SchedulingClass;
    }

    /// <summary>
    /// The job object extended limit information <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/ms684156(v=vs.85).aspx">structure</see>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        /// <summary>BasicLimitInformation field.</summary>
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;

        /// <summary>IoInfo field.</summary>
        public IO_COUNTERS IoInfo;

        /// <summary>ProcessMemoryLimit field.</summary>
        public UIntPtr ProcessMemoryLimit;

        /// <summary>JobMemoryLimit field.</summary>
        public UIntPtr JobMemoryLimit;

        /// <summary>PeakProcessMemoryUsed field.</summary>
        public UIntPtr PeakProcessMemoryUsed;

        /// <summary>PeakJobMemoryUsed field.</summary>
        public UIntPtr PeakJobMemoryUsed;
    }

    /// <summary>
    /// The job object CPU rate control information <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/hh448384(v=vs.85).aspx">structure</see>.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct JOBOBJECT_CPU_RATE_CONTROL_INFORMATION
    {
        /// <summary>ControlFlags field.</summary>
        [FieldOffset(0)]
        public uint ControlFlags;

        /// <summary>CpuRate field.</summary>
        [FieldOffset(4)]
        public uint CpuRate;

        /// <summary>Weight field.</summary>
        [FieldOffset(4)]
        public uint Weight;
    }

    /// <summary>
    /// Contains basic accounting information for a job object.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct JOBOBJECT_BASIC_ACCOUNTING_INFORMATION
    {
        /// <summary>The total user time.</summary>
        public long TotalUserTime;

        /// <summary>The total kernel time.</summary>
        public long TotalKernelTime;

        /// <summary>The this period total user time.</summary>
        public long ThisPeriodTotalUserTime;

        /// <summary>The this period total kernel time.</summary>
        public long ThisPeriodTotalKernelTime;

        /// <summary>The total page fault count.</summary>
        public int TotalPageFaultCount;

        /// <summary>The total processes.</summary>
        public int TotalProcesses;

        /// <summary>The active processes.</summary>
        public int ActiveProcesses;

        /// <summary>The total terminated processes.</summary>
        public int TotalTerminatedProcesses;
    }

    /// <summary>
    /// Wrapper class for job handle.
    /// </summary>
    public sealed class SafeJobHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Validate that the process is inside a JobObject.
        /// </summary>
        /// <exception cref="InvalidOperationException">The process is not running in a job.</exception>
        /// <returns><see langword="true" /> if the process is inside a JobObject; otherwise, <see langword="false" />.</returns>
        public static bool IsProcessInJob()
        {
            const uint DUPLICATE_SAME_ACCESS = 0x00000002;

            // Get a pseudo handle of the current process.
            var processHandle = UnsafeNativeMethods.GetCurrentProcess();

            // Using the pseudo handle get a copy of the current process handle.
            _ = UnsafeNativeMethods.DuplicateHandle(processHandle,
                    processHandle,
                    processHandle,
                    out var realProcessHandle,
                    0,
                    false,
                    DUPLICATE_SAME_ACCESS);

            using SafeJobHandle jobHandle = new SafeJobHandle();

            // Check if the process is running inside a job.
            _ = UnsafeNativeMethods.IsProcessInJob(realProcessHandle, jobHandle, out var processInJob);

            // Close the duplicated handle.
            _ = UnsafeNativeMethods.CloseHandle(realProcessHandle);

            return processInJob;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeJobHandle" /> class.
        /// </summary>
        public SafeJobHandle()
            : base(true)
        {
        }

        /// <summary>
        /// Get the current CPU rate information from the Job object.
        /// </summary>
        /// <returns>CPU rate information.</returns>
        public JOBOBJECT_CPU_RATE_CONTROL_INFORMATION GetJobCpuLimitInfo()
        {
            unsafe
            {
                JOBOBJECT_CPU_RATE_CONTROL_INFORMATION limit = default;
                void* buffer = &limit;
                GetJobObjectInformation(
                    JOBOBJECTINFOCLASS.JobObjectCpuRateControlInformation,
                    buffer,
                    sizeof(JOBOBJECT_CPU_RATE_CONTROL_INFORMATION));

                return limit;
            }
        }

        /// <summary>
        /// Get the current CPU rate information from the Job object.
        /// </summary>
        /// <returns>CPU rate information.</returns>
        public JOBOBJECT_BASIC_ACCOUNTING_INFORMATION GetBasicAccountingInfo()
        {
            unsafe
            {
                JOBOBJECT_BASIC_ACCOUNTING_INFORMATION limit = default;
                void* buffer = &limit;
                GetJobObjectInformation(
                    JOBOBJECTINFOCLASS.JobObjectBasicAccountingInformation,
                    buffer,
                    sizeof(JOBOBJECT_BASIC_ACCOUNTING_INFORMATION));

                return limit;
            }
        }

        /// <summary>
        /// Get the extended limit information from the Job object.
        /// </summary>
        /// <returns>Extended limit information.</returns>
        public JOBOBJECT_EXTENDED_LIMIT_INFORMATION GetExtendedLimitInfo()
        {
            unsafe
            {
                JOBOBJECT_EXTENDED_LIMIT_INFORMATION limit = default;
                void* buffer = &limit;
                GetJobObjectInformation(
                    JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation,
                    buffer,
                    sizeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));

                return limit;
            }
        }

        /// <summary>
        /// Release the encapsulated handle.
        /// </summary>
        /// <returns>True: released successfully, otherwise false.</returns>
#if NETFRAMEWORK
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        protected override bool ReleaseHandle()
        {
            return UnsafeNativeMethods.CloseHandle(handle);
        }

        protected override void Dispose(bool disposing)
        {
            _ = ReleaseHandle();
            base.Dispose(disposing);
        }

        /// <summary>
        /// Get the job object limit.
        /// </summary>
        /// <param name="infoClass">Job object info class.</param>
        /// <param name="buffer">Buffer containing the limit.</param>
        /// <param name="size">Buffer size.</param>
        /// <remarks>
        /// An application cannot obtain a handle to the job object in which it is running unless it has the name of the job object.
        /// However, an application can call the QueryInformationJobObject function with NULL to obtain information about the job object.
        /// </remarks>
        private unsafe void GetJobObjectInformation(JOBOBJECTINFOCLASS infoClass, void* buffer, int size)
        {
            if (!UnsafeNativeMethods.QueryInformationJobObject(
                this,
                infoClass,
                buffer,
                size,
                out _))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }
    }

    private static class UnsafeNativeMethods
    {
        /// <summary>
        /// Retrieves a pseudo handle for the current process.
        /// </summary>
        /// <returns>Pseudo handle to the current process.</returns>
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern IntPtr GetCurrentProcess();

        /// <summary>
        /// Duplicates an object handle.
        /// </summary>
        /// <param name="hSourceProcessHandle">A handle to the process with the handle to be duplicated.</param>
        /// <param name="hSourceHandle">The handle to be duplicated.</param>
        /// <param name="hTargetProcessHandle">A handle to the process that is to receive the duplicated handle.</param>
        /// <param name="lpTargetHandle">A pointer to a variable that receives the duplicate handle.</param>
        /// <param name="dwDesiredAccess">The access requested for the new handle.</param>
        /// <param name="bInheritHandle">A variable that indicates whether the handle is inheritable.</param>
        /// <param name="dwOptions">Optional actions.</param>
        /// <returns>Returns true if the function succeeds.</returns>
        /// <remarks>Used with <see cref="GetCurrentProcess"/> to get real handle of the current process instead of the pseudo handle.</remarks>
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern bool DuplicateHandle(
          IntPtr hSourceProcessHandle,
          IntPtr hSourceHandle,
          IntPtr hTargetProcessHandle,
          out IntPtr lpTargetHandle,
          uint dwDesiredAccess,
          bool bInheritHandle,
          uint dwOptions);

        /// <summary>
        /// Determines whether the process is running in the specified job.
        /// </summary>
        /// <param name="processHandle">A handle to the process to be tested.</param>
        /// <param name="jobHandle">A handle to the job.</param>
        /// <param name="result">A pointer to a value that receives true if the process is running in the job, and false otherwise.</param>
        /// <returns>True f the function succeeds, and false otherwise.</returns>
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsProcessInJob(
            IntPtr processHandle,
            SafeJobHandle jobHandle,
            out bool result);

        /// <summary>
        /// OS import for CloseHandle.
        /// </summary>
        /// <param name="handle">the object to close.</param>
        /// <returns>true if the handle was valid and closed.</returns>
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr handle);

        /// <summary>
        /// OS import for QueryInformationJobObject.
        /// </summary>
        /// <param name="job">The job handle.</param>
        /// <param name="jobObjectInfoClass">The job object information class.</param>
        /// <param name="jobObjectInfo">The information buffer.</param>
        /// <param name="jobObjectInfoLength">The information length.</param>
        /// <param name="returnLength">The data written.</param>
        /// <returns>True if the call succeeded; otherwise false.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern unsafe bool QueryInformationJobObject(
            SafeJobHandle job,
            JOBOBJECTINFOCLASS jobObjectInfoClass,
            void* jobObjectInfo,
            int jobObjectInfoLength,
            out int returnLength);
    }
}
