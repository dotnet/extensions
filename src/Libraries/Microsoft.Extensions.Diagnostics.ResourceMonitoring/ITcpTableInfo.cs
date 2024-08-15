// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// An interface for getting TCP/IP table information.
/// </summary>
internal interface ITcpTableInfo
{
    /// <summary>
    /// Gets the last known snapshot of TCP/IP v4 state info on the system.
    /// </summary>
    /// <returns>An instance of <see cref="TcpStateInfo"/>.</returns>
    TcpStateInfo GetIpV4CachingSnapshot();

    /// <summary>
    /// Gets the last known snapshot of TCP/IP v6 state info on the system.
    /// </summary>
    /// <returns>An instance of <see cref="TcpStateInfo"/>.</returns>
    TcpStateInfo GetIpV6CachingSnapshot();
}
