// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Network;

/// <summary>
/// TcpStateInfo contains different possible TCP state counts.
/// </summary>
internal sealed class TcpStateInfo
{
    /// <summary>Gets the count about the TCP connections which are closed.</summary>
    public long ClosedCount;

    /// <summary>Gets the count about the TCP connections which are in the listen state.</summary>
    public long ListenCount;

    /// <summary>Gets the count about the SYN packets which have been sent.</summary>
    public long SynSentCount;

    /// <summary>Gets the count about the SYN packets which have been received.</summary>
    public long SynRcvdCount;

    /// <summary>Gets the count about the TCP connections which have been established.</summary>
    public long EstabCount;

    /// <summary>Gets the count about the TCP connections which are waiting for a FIN packet 1.</summary>
    public long FinWait1Count;

    /// <summary>Gets the count about the TCP connections which are waiting for a FIN packet 2.</summary>
    public long FinWait2Count;

    /// <summary>Gets the count about the TCP connections which are in the close wait state.</summary>
    public long CloseWaitCount;

    /// <summary>Gets the count about the TCP connections which are closing.</summary>
    public long ClosingCount;

    /// <summary>Gets the count about the TCP connections which are in the last ACK state.</summary>
    public long LastAckCount;

    /// <summary>Gets the count about the TCP connections which are in the time wait state.</summary>
    public long TimeWaitCount;

    /// <summary>Gets the count about the TCP connections which are in the delete TCB state.</summary>
    public long DeleteTcbCount;
}
