// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Network;

internal class LinuxTcpTableInfo : ITcpTableInfo
{
    private readonly object _lock = new();
    private readonly TimeSpan _samplingInterval;
    private readonly LinuxNetworkUtilizationParser _parser;
    private readonly TimeProvider _timeProvider;

    private TcpStateInfo _iPv4Snapshot = new();
    private TcpStateInfo _iPv6Snapshot = new();
    private DateTimeOffset _refreshAfter;

    public LinuxTcpTableInfo(IOptions<ResourceMonitoringOptions> options, LinuxNetworkUtilizationParser parser, TimeProvider timeProvider)
    {
        _samplingInterval = options.Value.SamplingInterval;
        _parser = parser;
        _timeProvider = timeProvider;
    }

    public TcpStateInfo GetIpV4CachingSnapshot()
    {
        RefreshSnapshotIfNeeded();
        return _iPv4Snapshot;
    }

    public TcpStateInfo GetIpV6CachingSnapshot()
    {
        RefreshSnapshotIfNeeded();
        return _iPv6Snapshot;
    }

    private void RefreshSnapshotIfNeeded()
    {
        lock (_lock)
        {
            if (_refreshAfter < _timeProvider.GetUtcNow())
            {
                _iPv4Snapshot = _parser.GetTcpIPv4StateInfo();
                _iPv6Snapshot = _parser.GetTcpIPv6StateInfo();
                _refreshAfter = _timeProvider.GetUtcNow().Add(_samplingInterval);
            }
        }
    }
}
