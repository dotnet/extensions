// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;

/// <summary>
/// SYSTEM_INFO struct needed for GetSystemInfo.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct SYSTEM_INFO
{
    public ushort ProcessorArchitecture;
    public ushort Reserved;
    public uint PageSize;
    public UIntPtr MinimumApplicationAddress;
    public UIntPtr MaximumApplicationAddress;
    public UIntPtr ActiveProcessorMask;
    public uint NumberOfProcessors;
    public uint ProcessorType;
    public uint AllocationGranularity;
    public ushort ProcessorLevel;
    public ushort ProcessorRevision;
}
