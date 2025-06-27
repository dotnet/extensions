// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Network;

internal sealed class LinuxTcpStateInfo : ITcpStateInfoProvider
{
    private readonly object _lock = new();
    private readonly TimeSpan _samplingInterval;
    private readonly LinuxNetworkUtilizationParser _parser;
    private static TimeProvider TimeProvider => TimeProvider.System;

    private TcpStateInfo _iPv4Snapshot = new();
    private TcpStateInfo _iPv6Snapshot = new();
    private DateTimeOffset _refreshAfter;

    public LinuxTcpStateInfo(IOptions<ResourceMonitoringOptions> options, LinuxNetworkUtilizationParser parser)
    {
        _samplingInterval = options.Value.SamplingInterval;
        _parser = parser;
    }

    public TcpStateInfo GetIpV4TcpStateInfo()
    {
        RefreshSnapshotIfNeeded();
        return _iPv4Snapshot;
    }

    public TcpStateInfo GetIpV6TcpStateInfo()
    {
        RefreshSnapshotIfNeeded();
        return _iPv6Snapshot;
    }

    private void RefreshSnapshotIfNeeded()
    {
        lock (_lock)
        {
            if (_refreshAfter < TimeProvider.GetUtcNow())
            {
                _iPv4Snapshot = _parser.GetTcpIPv4StateInfo();
                _iPv6Snapshot = _parser.GetTcpIPv6StateInfo();
                _refreshAfter = TimeProvider.GetUtcNow().Add(_samplingInterval);
            }
        }
    }
}
