// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

[StructLayout(LayoutKind.Sequential)]
internal struct MIB_TCPTABLE
{
    public uint NumberOfEntries;
    public MIB_TCPROW Table;
}
