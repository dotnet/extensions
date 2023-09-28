﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Network;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

internal sealed class WindowsCounters
{
    public WindowsCounters(IMeterFactory meterFactory, TcpTableInfo tcpTableInfo)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        // We don't dispose the meter because IMeterFactory handles that
        // An issue on analyzer side: https://github.com/dotnet/roslyn-analyzers/issues/6912
        // Related documentation: https://github.com/dotnet/docs/pull/37170
        var meter = meterFactory.Create("Microsoft.Extensions.Diagnostics.ResourceMonitoring");
#pragma warning restore CA2000 // Dispose objects before losing scope

        _ = meter.CreateObservableGauge(
            "ipv4_tcp_connection_closed_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.ClosedCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv4_tcp_connection_listen_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.ListenCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv4_tcp_connection_syn_sent_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.SynSentCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv4_tcp_connection_syn_received_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.SynRcvdCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv4_tcp_connection_established_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.EstabCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv4_tcp_connection_fin_wait_1_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.FinWait1Count;
            });

        _ = meter.CreateObservableGauge(
            "ipv4_tcp_connection_fin_wait_2_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.FinWait2Count;
            });

        _ = meter.CreateObservableGauge(
            "ipv4_tcp_connection_close_wait_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.CloseWaitCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv4_tcp_connection_closing_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.ClosingCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv4_tcp_connection_last_ack_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.LastAckCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv4_tcp_connection_time_wait_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.TimeWaitCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv4_tcp_connection_delete_tcb_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.DeleteTcbCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv6_tcp_connection_closed_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.ClosedCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv6_tcp_connection_listen_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.ListenCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv6_tcp_connection_syn_sent_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.SynSentCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv6_tcp_connection_syn_received_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.SynRcvdCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv6_tcp_connection_established_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.EstabCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv6_tcp_connection_fin_wait_1_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.FinWait1Count;
            });

        _ = meter.CreateObservableGauge(
            "ipv6_tcp_connection_fin_wait_2_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.FinWait2Count;
            });

        _ = meter.CreateObservableGauge(
            "ipv6_tcp_connection_close_wait_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.CloseWaitCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv6_tcp_connection_closing_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.ClosingCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv6_tcp_connection_last_ack_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.LastAckCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv6_tcp_connection_time_wait_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.TimeWaitCount;
            });

        _ = meter.CreateObservableGauge(
            "ipv6_tcp_connection_delete_tcb_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.DeleteTcbCount;
            });
    }
}
