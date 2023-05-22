// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

/// <summary>The MIB_TCP_STATE enumeration enumerates different possible TCP states.</summary>
internal enum MIB_TCP_STATE : uint
{
    /// <summary>The TCP connection is closed.</summary>
    CLOSED = 1,

    /// <summary> The TCP connection is in the listen state.</summary>
    LISTEN = 2,

    /// <summary>A SYN packet has been sent.</summary>
    SYN_SENT = 3,

    /// <summary>A SYN packet has been received.</summary>
    SYN_RCVD = 4,

    /// <summary>The TCP connection has been established.</summary>
    ESTAB = 5,

    /// <summary>The TCP connection is waiting for a FIN packet.</summary>
    FIN_WAIT1 = 6,

    /// <summary>The TCP connection is waiting for a FIN packet.</summary>
    FIN_WAIT2 = 7,

    /// <summary>The TCP connection is in the close wait state.</summary>
    CLOSE_WAIT = 8,

    /// <summary>The TCP connection is closing.</summary>
    CLOSING = 9,

    /// <summary>The TCP connection is in the last ACK state.</summary>
    LAST_ACK = 10,

    /// <summary>The TCP connection is in the time wait state.</summary>
    TIME_WAIT = 11,

    /// <summary>The TCP connection is in the delete TCB state.</summary>
    DELETE_TCB = 12
}
