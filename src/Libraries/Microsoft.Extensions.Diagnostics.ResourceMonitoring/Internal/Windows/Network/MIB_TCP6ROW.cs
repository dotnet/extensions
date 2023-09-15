// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

/// <summary>The MIB_TCP6ROW structure contains information that describes an IPv6 TCP connection.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MIB_TCP6ROW
{
    /// <summary>The state of the TCP connection.</summary>
    internal MIB_TCP_STATE State;

    /// <summary>The local IPv6 address for the TCP connection on the local computer. A value of zero indicates the listener can accept a connection on any interface.</summary>
    internal IN6_ADDR LocalAddr;

    /// <summary>The local scope ID for the TCP connection on the local computer.</summary>
    internal uint LocalScopeId;

    /// <summary>The local port number in network byte order for the TCP connection on the local computer.</summary>
    internal uint LocalPort;

    /// <summary>The IPv6 address for the TCP connection on the remote computer. When the State member is MIB_TCP_STATE_LISTEN, this value has no meaning.</summary>
    internal IN6_ADDR RemoteAddr;

    /// <summary>The remote scope ID for the TCP connection on the remote computer. When the State member is MIB_TCP_STATE_LISTEN, this value has no meaning.</summary>
    internal uint RemoteScopeId;

    /// <summary>The remote port number in network byte order for the TCP connection on the remote computer. When the State member is MIB_TCP_STATE_LISTEN, this value has no meaning.</summary>
    internal uint RemotePort;

}

