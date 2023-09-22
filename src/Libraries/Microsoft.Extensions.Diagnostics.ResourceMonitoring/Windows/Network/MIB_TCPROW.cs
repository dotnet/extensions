// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Network;

/// <summary>The MIB_TCPROW structure contains information for an IPv4 TCP connection.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MIB_TCPROW
{
    /// <summary>The state of the TCP connection.</summary>
    internal MIB_TCP_STATE State;

    /// <summary>The local IPv4 address for the TCP connection on the local computer.</summary>
    internal uint LocalAddr;

    /// <summary>The local port number in network byte order for the TCP connection on the local computer.</summary>
    internal uint LocalPort;

    /// <summary>The IPv4 address for the TCP connection on the remote computer.</summary>
    internal uint RemoteAddr;

    /// <summary>The remote port number in network byte order for the TCP connection on the remote computer.</summary>
    internal uint RemotePort;
}
