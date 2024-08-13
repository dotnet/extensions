// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Network;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

internal sealed class WindowsNetworkMetrics
{
    private readonly TcpTableInfo _tcpTableInfo;

    public WindowsNetworkMetrics(IMeterFactory meterFactory, TcpTableInfo tcpTableInfo)
    {
        _tcpTableInfo = tcpTableInfo;

#pragma warning disable CA2000 // Dispose objects before losing scope
        // We don't dispose the meter because IMeterFactory handles that
        // An issue on analyzer side: https://github.com/dotnet/roslyn-analyzers/issues/6912
        // Related documentation: https://github.com/dotnet/docs/pull/37170
        var meter = meterFactory.Create(nameof(Microsoft.Extensions.Diagnostics.ResourceMonitoring));
#pragma warning restore CA2000 // Dispose objects before losing scope

        var tcpTag = new KeyValuePair<string, object?>("network.transport", "tcp");
        var commonTags = new TagList
        {
            tcpTag
        };

        // The metric is aligned with
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/system/system-metrics.md#metric-systemnetworkconnections

        _ = meter.CreateObservableUpDownCounter(
            "system.network.connections",
            GetMeasurements,
            unit: "{connection}",
            description: null,
            tags: commonTags);
    }

    private IEnumerable<Measurement<long>> GetMeasurements()
    {
        const string NetworkStateKey = "system.network.state";

        // These are covered in https://github.com/open-telemetry/semantic-conventions/blob/main/docs/rpc/rpc-metrics.md#attributes:
        var tcpVersionFourTag = new KeyValuePair<string, object?>("network.type", "ipv4");
        var tcpVersionSixTag = new KeyValuePair<string, object?>("network.type", "ipv6");

        var measurements = new List<Measurement<long>>(24);

        // IPv4:
        var snapshotV4 = _tcpTableInfo.GetIPv4CachingSnapshot();
        measurements.Add(new Measurement<long>(snapshotV4.ClosedCount, new TagList { tcpVersionFourTag, new(NetworkStateKey, "close") }));
        measurements.Add(new Measurement<long>(snapshotV4.ListenCount, new TagList { tcpVersionFourTag, new(NetworkStateKey, "listen") }));
        measurements.Add(new Measurement<long>(snapshotV4.SynSentCount, new TagList { tcpVersionFourTag, new(NetworkStateKey, "syn_sent") }));
        measurements.Add(new Measurement<long>(snapshotV4.SynRcvdCount, new TagList { tcpVersionFourTag, new(NetworkStateKey, "syn_recv") }));
        measurements.Add(new Measurement<long>(snapshotV4.EstabCount, new TagList { tcpVersionFourTag, new(NetworkStateKey, "established") }));
        measurements.Add(new Measurement<long>(snapshotV4.FinWait1Count, new TagList { tcpVersionFourTag, new(NetworkStateKey, "fin_wait_1") }));
        measurements.Add(new Measurement<long>(snapshotV4.FinWait2Count, new TagList { tcpVersionFourTag, new(NetworkStateKey, "fin_wait_2") }));
        measurements.Add(new Measurement<long>(snapshotV4.CloseWaitCount, new TagList { tcpVersionFourTag, new(NetworkStateKey, "close_wait") }));
        measurements.Add(new Measurement<long>(snapshotV4.ClosingCount, new TagList { tcpVersionFourTag, new(NetworkStateKey, "closing") }));
        measurements.Add(new Measurement<long>(snapshotV4.LastAckCount, new TagList { tcpVersionFourTag, new(NetworkStateKey, "last_ack") }));
        measurements.Add(new Measurement<long>(snapshotV4.TimeWaitCount, new TagList { tcpVersionFourTag, new(NetworkStateKey, "time_wait") }));
        measurements.Add(new Measurement<long>(snapshotV4.DeleteTcbCount, new TagList { tcpVersionFourTag, new(NetworkStateKey, "delete") }));

        // IPv6:
        var snapshotV6 = _tcpTableInfo.GetIPv6CachingSnapshot();
        measurements.Add(new Measurement<long>(snapshotV6.ClosedCount, new TagList { tcpVersionSixTag, new(NetworkStateKey, "close") }));
        measurements.Add(new Measurement<long>(snapshotV6.ListenCount, new TagList { tcpVersionSixTag, new(NetworkStateKey, "listen") }));
        measurements.Add(new Measurement<long>(snapshotV6.SynSentCount, new TagList { tcpVersionSixTag, new(NetworkStateKey, "syn_sent") }));
        measurements.Add(new Measurement<long>(snapshotV6.SynRcvdCount, new TagList { tcpVersionSixTag, new(NetworkStateKey, "syn_recv") }));
        measurements.Add(new Measurement<long>(snapshotV6.EstabCount, new TagList { tcpVersionSixTag, new(NetworkStateKey, "established") }));
        measurements.Add(new Measurement<long>(snapshotV6.FinWait1Count, new TagList { tcpVersionSixTag, new(NetworkStateKey, "fin_wait_1") }));
        measurements.Add(new Measurement<long>(snapshotV6.FinWait2Count, new TagList { tcpVersionSixTag, new(NetworkStateKey, "fin_wait_2") }));
        measurements.Add(new Measurement<long>(snapshotV6.CloseWaitCount, new TagList { tcpVersionSixTag, new(NetworkStateKey, "close_wait") }));
        measurements.Add(new Measurement<long>(snapshotV6.ClosingCount, new TagList { tcpVersionSixTag, new(NetworkStateKey, "closing") }));
        measurements.Add(new Measurement<long>(snapshotV6.LastAckCount, new TagList { tcpVersionSixTag, new(NetworkStateKey, "last_ack") }));
        measurements.Add(new Measurement<long>(snapshotV6.TimeWaitCount, new TagList { tcpVersionSixTag, new(NetworkStateKey, "time_wait") }));
        measurements.Add(new Measurement<long>(snapshotV6.DeleteTcbCount, new TagList { tcpVersionSixTag, new(NetworkStateKey, "delete") }));

        return measurements;
    }
}
