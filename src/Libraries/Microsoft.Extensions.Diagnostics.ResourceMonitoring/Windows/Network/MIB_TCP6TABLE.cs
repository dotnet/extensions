// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Network;

/// <summary>The MIB_TCP6TABLE structure contains a table of TCP connections for IPv6 on the local computer.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MIB_TCP6TABLE
{
    internal uint NumberOfEntries;
    internal MIB_TCP6ROW Table;
}
