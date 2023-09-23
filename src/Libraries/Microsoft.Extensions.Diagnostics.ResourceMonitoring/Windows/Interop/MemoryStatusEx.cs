// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;

/// <summary>
/// The Win32 MEMORYSTATUSEX structure.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MEMORYSTATUSEX
{
    public uint Length;                // DWORD
    public uint MemoryLoad;            // DWORD
    public ulong TotalPhys;             // DWORDLONG
    public ulong AvailPhys;             // DWORDLONG
    public ulong TotalPageFile;         // DWORDLONG
    public ulong AvailPageFile;         // DWORDLONG
    public ulong TotalVirtual;          // DWORDLONG
    public ulong AvailVirtual;          // DWORDLONG
    public ulong AvailExtendedVirtual;  // DWORDLONG
}
