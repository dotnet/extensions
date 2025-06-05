// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// An interface for getting TCP/IP state information.
/// </summary>
internal interface ITcpStateInfoProvider
{
    /// <summary>
    /// Gets the last known TCP/IP v4 state of the system.
    /// </summary>
    /// <returns>An instance of <see cref="TcpStateInfo"/>.</returns>
    TcpStateInfo GetIpV4TcpStateInfo();

    /// <summary>
    /// Gets the last known TCP/IP v6 state of the system.
    /// </summary>
    /// <returns>An instance of <see cref="TcpStateInfo"/>.</returns>
    TcpStateInfo GetIpV6TcpStateInfo();
}
