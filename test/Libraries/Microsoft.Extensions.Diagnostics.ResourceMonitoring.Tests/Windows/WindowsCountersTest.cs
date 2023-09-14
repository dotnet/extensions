// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Test;

[Collection("Tcp Connection Tests")]
public sealed class WindowsCountersTest
{
    [Fact]
    public void WindowsCounters_Registers_Instruments()
    {
        TcpTableInfoTest.StartTimestamp = DateTimeOffset.UtcNow;
        TcpTableInfoTest.NextTimestamp = TcpTableInfoTest.StartTimestamp.Add(TcpTableInfoTest.DefaultTimeSpan);
        TcpTableInfo.SetGetTcpTableDelegate(TcpTableInfoTest.FakeGetTcpTableWithFakeInformation);
        var options = new ResourceMonitoringOptions
        {
            SourceIpAddresses = new HashSet<string> { "127.0.0.1" },
            SamplingInterval = TimeSpan.FromSeconds(5)
        };
        using var meter = new Meter<WindowsCounters>();
        using var windowsCounters = new WindowsCounters(Microsoft.Extensions.Options.Options.Create(options), meter);
        using var listener = new System.Diagnostics.Metrics.MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        var samples = new List<(System.Diagnostics.Metrics.Instrument instrument, long value)>();
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            samples.Add((instrument, value));
        });

        listener.Start();
        listener.RecordObservableInstruments();
        samples.Count.Should().Be(12);
        samples.First().instrument.Name.Should().Be("ipv4_tcp_connection_closed_count");
        samples.First().value.Should().Be(1);
        samples.Skip(1).First().instrument.Name.Should().Be("ipv4_tcp_connection_listen_count");
        samples.Skip(1).First().value.Should().Be(1);
        samples.Skip(2).First().instrument.Name.Should().Be("ipv4_tcp_connection_syn_sent_count");
        samples.Skip(2).First().value.Should().Be(1);
        samples.Skip(3).First().instrument.Name.Should().Be("ipv4_tcp_connection_syn_received_count");
        samples.Skip(3).First().value.Should().Be(1);
        samples.Skip(4).First().instrument.Name.Should().Be("ipv4_tcp_connection_established_count");
        samples.Skip(4).First().value.Should().Be(1);
        samples.Skip(5).First().instrument.Name.Should().Be("ipv4_tcp_connection_fin_wait_1_count");
        samples.Skip(5).First().value.Should().Be(1);
        samples.Skip(6).First().instrument.Name.Should().Be("ipv4_tcp_connection_fin_wait_2_count");
        samples.Skip(6).First().value.Should().Be(1);
        samples.Skip(7).First().instrument.Name.Should().Be("ipv4_tcp_connection_close_wait_count");
        samples.Skip(7).First().value.Should().Be(1);
        samples.Skip(8).First().instrument.Name.Should().Be("ipv4_tcp_connection_closing_count");
        samples.Skip(8).First().value.Should().Be(1);
        samples.Skip(9).First().instrument.Name.Should().Be("ipv4_tcp_connection_last_ack_count");
        samples.Skip(9).First().value.Should().Be(1);
        samples.Skip(10).First().instrument.Name.Should().Be("ipv4_tcp_connection_time_wait_count");
        samples.Skip(10).First().value.Should().Be(1);
        samples.Skip(11).First().instrument.Name.Should().Be("ipv4_tcp_connection_delete_tcb_count");
        samples.Skip(11).First().value.Should().Be(1);
    }

    [Fact]
    public void WindowsCounters_Got_Unsuccessful()
    {
        TcpTableInfo.SetGetTcpTableDelegate(TcpTableInfoTest.FakeGetTcpTableWithUnsuccessfulStatusAllTheTime);
        var options = new ResourceMonitoringOptions
        {
            SourceIpAddresses = new HashSet<string> { "127.0.0.1" },
            SamplingInterval = TimeSpan.FromSeconds(5)
        };
        using var meter = new Meter<WindowsCounters>();
        using var windowsCounters = new WindowsCounters(Microsoft.Extensions.Options.Options.Create(options), meter);
        using var listener = new System.Diagnostics.Metrics.MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        var samples = new List<(System.Diagnostics.Metrics.Instrument instrument, long value)>();
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            samples.Add((instrument, value));
        });

        listener.Start();
        Assert.Throws<AggregateException>(() =>
        {
            listener.RecordObservableInstruments();
        });
    }
}
