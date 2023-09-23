// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Network;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

internal sealed class WindowsCounters : IDisposable
{
    private readonly Meter<WindowsCounters> _meter;

    public WindowsCounters(Meter<WindowsCounters> meter, TcpTableInfo tcpTableInfo)
    {
        _meter = meter;

        _ = _meter.CreateObservableGauge(
            "ipv4_tcp_connection_closed_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.ClosedCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv4_tcp_connection_listen_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.ListenCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv4_tcp_connection_syn_sent_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.SynSentCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv4_tcp_connection_syn_received_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.SynRcvdCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv4_tcp_connection_established_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.EstabCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv4_tcp_connection_fin_wait_1_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.FinWait1Count;
            });

        _ = _meter.CreateObservableGauge(
            "ipv4_tcp_connection_fin_wait_2_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.FinWait2Count;
            });

        _ = _meter.CreateObservableGauge(
            "ipv4_tcp_connection_close_wait_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.CloseWaitCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv4_tcp_connection_closing_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.ClosingCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv4_tcp_connection_last_ack_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.LastAckCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv4_tcp_connection_time_wait_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.TimeWaitCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv4_tcp_connection_delete_tcb_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv4CachingSnapshot();
                return snapshot.DeleteTcbCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv6_tcp_connection_closed_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.ClosedCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv6_tcp_connection_listen_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.ListenCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv6_tcp_connection_syn_sent_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.SynSentCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv6_tcp_connection_syn_received_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.SynRcvdCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv6_tcp_connection_established_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.EstabCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv6_tcp_connection_fin_wait_1_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.FinWait1Count;
            });

        _ = _meter.CreateObservableGauge(
            "ipv6_tcp_connection_fin_wait_2_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.FinWait2Count;
            });

        _ = _meter.CreateObservableGauge(
            "ipv6_tcp_connection_close_wait_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.CloseWaitCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv6_tcp_connection_closing_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.ClosingCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv6_tcp_connection_last_ack_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.LastAckCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv6_tcp_connection_time_wait_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.TimeWaitCount;
            });

        _ = _meter.CreateObservableGauge(
            "ipv6_tcp_connection_delete_tcb_count",
            () =>
            {
                var snapshot = tcpTableInfo.GetIPv6CachingSnapshot();
                return snapshot.DeleteTcbCount;
            });
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
