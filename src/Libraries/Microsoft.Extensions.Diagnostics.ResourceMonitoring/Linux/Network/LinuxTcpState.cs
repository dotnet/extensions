// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Network;

/// <summary>
/// Enumerates all possible TCP states on Linux.
/// </summary>
internal enum LinuxTcpState
{
    /// <summary>The TCP connection was established.</summary>
    ESTABLISHED = 1,

    /// <summary>The TCP connection has sent a SYN packet.</summary>
    SYN_SENT = 2,

    /// <summary>The TCP connection has received a SYN packet.</summary>
    SYN_RECV = 3,

    /// <summary>The TCP connection is waiting for a FIN packet.</summary>
    FIN_WAIT1 = 4,

    /// <summary>The TCP connection is waiting for a FIN packet.</summary>
    FIN_WAIT2 = 5,

    /// <summary>The TCP connection is in the time wait state.</summary>
    TIME_WAIT = 6,

    /// <summary>The TCP connection is closed.</summary>
    CLOSE = 7,

    /// <summary>The TCP connection is in the close wait state.</summary>
    CLOSE_WAIT = 8,

    /// <summary>The TCP connection is in the last ACK state.</summary>
    LAST_ACK = 9,

    /// <summary>The TCP connection is in the listen state.</summary>
    LISTEN = 10,

    /// <summary>The TCP connection is closing.</summary>
    CLOSING = 11
}
