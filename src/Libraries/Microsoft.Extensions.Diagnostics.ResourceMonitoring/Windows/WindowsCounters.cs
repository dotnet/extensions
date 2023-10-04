// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Network;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

internal sealed class WindowsCounters
{
    public WindowsCounters(IMeterFactory meterFactory, TcpTableInfo tcpTableInfo)
    {
        const string NetworkStateKey = "system.network.state";
        const string GaugeName = "process.network.connections";
#pragma warning disable CA2000 // Dispose objects before losing scope
        // We don't dispose the meter because IMeterFactory handles that
        // An issue on analyzer side: https://github.com/dotnet/roslyn-analyzers/issues/6912
        // Related documentation: https://github.com/dotnet/docs/pull/37170
        var meter = meterFactory.Create("Microsoft.Extensions.Diagnostics.ResourceMonitoring");
#pragma warning restore CA2000 // Dispose objects before losing scope

        var tcpTag = KeyValuePair.Create<string, object?>("network.transport", "tcp");
        var tcpVersionFourTag = KeyValuePair.Create<string, object?>("network.transport.version", "ipv4");
        var tcpVersionSixTag = KeyValuePair.Create<string, object?>("network.transport.version", "ipv6");

        // TODO: these are heavily rewritten to align with
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/system/system-metrics.md#metric-systemnetworkconnections
        // All these should be of UpDownCounter type
        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv4_connection_closed_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.ClosedCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionFourTag, new(NetworkStateKey, "closed") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv4_connection_listen_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.ListenCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionFourTag, new(NetworkStateKey, "listen") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv4_connection_syn_sent_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.SynSentCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionFourTag, new(NetworkStateKey, "syn_sent") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv4_connection_syn_received_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.SynRcvdCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionFourTag, new(NetworkStateKey, "syn_recv") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv4_connection_established_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.EstabCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionFourTag, new(NetworkStateKey, "established") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv4_connection_fin_wait_1_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.FinWait1Count;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionFourTag, new(NetworkStateKey, "fin_wait_1") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv4_connection_fin_wait_2_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.FinWait2Count;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionFourTag, new(NetworkStateKey, "fin_wait_2") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv4_connection_close_wait_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.CloseWaitCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionFourTag, new(NetworkStateKey, "close_wait") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv4_connection_closing_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.ClosingCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionFourTag, new(NetworkStateKey, "closing") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv4_connection_last_ack_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.LastAckCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionFourTag, new(NetworkStateKey, "last_ack") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv4_connection_time_wait_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.TimeWaitCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionFourTag, new(NetworkStateKey, "time_wait") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv4_connection_delete_tcb_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.DeleteTcbCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionFourTag, new(NetworkStateKey, "delete") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv6_connection_closed_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.ClosedCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionSixTag, new(NetworkStateKey, "close") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv6_connection_listen_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.ListenCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionSixTag, new(NetworkStateKey, "listen") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv6_connection_syn_sent_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.SynSentCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionSixTag, new(NetworkStateKey, "syn_sent") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv6_connection_syn_received_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.SynRcvdCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionSixTag, new(NetworkStateKey, "syn_recv") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv6_connection_established_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.EstabCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionSixTag, new(NetworkStateKey, "established") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv6_connection_fin_wait_1_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.FinWait1Count;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionSixTag, new(NetworkStateKey, "fin_wait_1") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv6_connection_fin_wait_2_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.FinWait2Count;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionSixTag, new(NetworkStateKey, "fin_wait_2") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv6_connection_close_wait_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.CloseWaitCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionSixTag, new(NetworkStateKey, "close_wait") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv6_connection_closing_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.ClosingCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionSixTag, new(NetworkStateKey, "closing") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv6_connection_last_ack_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.LastAckCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionSixTag, new(NetworkStateKey, "last_ack") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv6_connection_time_wait_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.TimeWaitCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionSixTag, new(NetworkStateKey, "time_wait") });

        _ = meter.CreateObservableGauge(
            //"process.network.tcp.ipv6_connection_delete_tcb_count",
            GaugeName,
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.DeleteTcbCount;
            },
            unit: "{connection}",
            description: null,
            tags: new[] { tcpTag, tcpVersionSixTag, new(NetworkStateKey, "delete") });
    }
}
