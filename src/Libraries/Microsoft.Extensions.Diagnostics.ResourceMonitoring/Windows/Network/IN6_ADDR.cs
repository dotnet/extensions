// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Network;

/// <summary>The IN6_ADDR structure specifies an IPv6 transport address.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct IN6_ADDR
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    internal byte[] Byte;
}
