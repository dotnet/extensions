// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

public sealed class AcceptanceTest
{
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux specific package.")]
    public void Adding_Linux_Resource_Utilization_Allows_To_Query_Snapshot_Provider()
    {
        using var services = new ServiceCollection()
            .AddResourceMonitoring()
            .BuildServiceProvider();

        var provider = services.GetRequiredService<ISnapshotProvider>();

        Assert.NotEqual(default, provider.Resources);
        Assert.NotEqual(default, provider.GetSnapshot());
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux specific package.")]
    [SuppressMessage("Minor Code Smell", "S3257:Declarations and initializations should be as concise as possible", Justification = "Broken analyzer.")]
    public void Adding_Linux_Resource_Utilization_Can_Be_Configured_With_Section()
    {
        var cpuRefresh = TimeSpan.FromMinutes(13);
        var memoryRefresh = TimeSpan.FromMinutes(14);

        var config = new KeyValuePair<string, string?>[]
            {
                new($"{nameof(ResourceMonitoringOptions)}:{nameof(ResourceMonitoringOptions.CpuConsumptionRefreshInterval)}", cpuRefresh.ToString()),
                new($"{nameof(ResourceMonitoringOptions)}:{nameof(ResourceMonitoringOptions.MemoryConsumptionRefreshInterval)}", memoryRefresh.ToString()),
            };

        var section = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build()
            .GetSection(nameof(ResourceMonitoringOptions));

        using var services = new ServiceCollection()
            .AddResourceMonitoring(x => x.ConfigureMonitor(section))
            .BuildServiceProvider();

        var options = services.GetRequiredService<IOptions<ResourceMonitoringOptions>>();

        Assert.NotNull(options.Value);
        Assert.Equal(cpuRefresh, options.Value.CpuConsumptionRefreshInterval);
        Assert.Equal(memoryRefresh, options.Value.MemoryConsumptionRefreshInterval);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux specific package.")]
    public void Adding_Linux_Resource_Utilization_Can_Be_Configured_With_Action()
    {
        var cpuRefresh = TimeSpan.FromMinutes(13);
        var memoryRefresh = TimeSpan.FromMinutes(14);

        using var services = new ServiceCollection()
            .AddResourceMonitoring(x => x.ConfigureMonitor(options =>
            {
                options.CpuConsumptionRefreshInterval = cpuRefresh;
                options.MemoryConsumptionRefreshInterval = memoryRefresh;
            }))
            .BuildServiceProvider();

        var options = services.GetRequiredService<IOptions<ResourceMonitoringOptions>>();

        Assert.NotNull(options.Value);
        Assert.Equal(cpuRefresh, options.Value.CpuConsumptionRefreshInterval);
        Assert.Equal(memoryRefresh, options.Value.MemoryConsumptionRefreshInterval);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux specific package.")]
    [SuppressMessage("Minor Code Smell", "S3257:Declarations and initializations should be as concise as possible", Justification = "Broken analyzer.")]
    public void Adding_Linux_Resource_Utilization_With_Section_Registers_SnapshotProvider_Cgroupv1()
    {
        var cpuRefresh = TimeSpan.FromMinutes(13);
        var memoryRefresh = TimeSpan.FromMinutes(14);

        var config = new KeyValuePair<string, string?>[]
            {
                new($"{nameof(ResourceMonitoringOptions)}:{nameof(ResourceMonitoringOptions.CpuConsumptionRefreshInterval)}", cpuRefresh.ToString()),
                new($"{nameof(ResourceMonitoringOptions)}:{nameof(ResourceMonitoringOptions.MemoryConsumptionRefreshInterval)}", memoryRefresh.ToString()),
            };

        var section = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build()
            .GetSection(nameof(ResourceMonitoringOptions));

        using var services = new ServiceCollection()
            .AddSingleton<IUserHz>(new FakeUserHz(100))
            .AddSingleton<IFileSystem>(new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
            {
                { new FileInfo("/sys/fs/cgroup/memory/memory.limit_in_bytes"), "100000" },
                { new FileInfo("/proc/stat"), "cpu  10 10 10 10 10 10 10 10 10 10"},
                { new FileInfo("/sys/fs/cgroup/cpuacct/cpuacct.usage"), "102312"},
                { new FileInfo("/proc/meminfo"), "MemTotal: 102312 kB"},
                { new FileInfo("/sys/fs/cgroup/cpuset/cpuset.cpus"), "0-19"},
                { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_quota_us"), "12"},
                { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_period_us"), "6"},
                { new FileInfo("/sys/fs/cgroup/cpu/cpu.shares"), "1024"}
            }))
            .AddResourceMonitoring(x => x.ConfigureMonitor(section))

            // Ingesting LinuxUtilizationParser with cgroup v1 support.
            .Replace(ServiceDescriptor.Singleton<ILinuxUtilizationParser, LinuxUtilizationParserCgroupV1>())
            .BuildServiceProvider();

        var provider = services.GetService<ISnapshotProvider>();
        Assert.NotNull(provider);
        Assert.Equal(1, provider.Resources.GuaranteedCpuUnits); // hack to make hardcoded calculation in resource utilization main package work.
        Assert.Equal(2.0d, provider.Resources.MaximumCpuUnits); // read from cpuset.cpus
        Assert.Equal(100_000UL, provider.Resources.GuaranteedMemoryInBytes); // read from memory.limit_in_bytes

        // The main usage of this library is containers.
        // To not break the contaract with the main package, we need to set the maximum memory to the same value as the guaranteed memory.
        Assert.Equal(100_000UL, provider.Resources.MaximumMemoryInBytes);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux specific package.")]
    [SuppressMessage("Minor Code Smell", "S3257:Declarations and initializations should be as concise as possible", Justification = "Broken analyzer.")]
    public void Adding_Linux_Resource_Utilization_With_Section_Registers_SnapshotProvider_Cgroupv2()
    {
        var cpuRefresh = TimeSpan.FromMinutes(13);
        var memoryRefresh = TimeSpan.FromMinutes(14);

        var config = new KeyValuePair<string, string?>[]
            {
                new($"{nameof(ResourceMonitoringOptions)}:{nameof(ResourceMonitoringOptions.CpuConsumptionRefreshInterval)}", cpuRefresh.ToString()),
                new($"{nameof(ResourceMonitoringOptions)}:{nameof(ResourceMonitoringOptions.MemoryConsumptionRefreshInterval)}", memoryRefresh.ToString()),
            };

        var section = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build()
            .GetSection(nameof(ResourceMonitoringOptions));

        using var services = new ServiceCollection()
            .AddSingleton<IUserHz>(new FakeUserHz(100))
            .AddSingleton<IFileSystem>(new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
            {
                { new FileInfo("/proc/stat"), "cpu  10 10 10 10 10 10 10 10 10 10"},
                { new FileInfo("/sys/fs/cgroup/cpu.stat"), "usage_usec 102312"},
                { new FileInfo("/proc/meminfo"), "MemTotal: 102312 kB"},
                { new FileInfo("/sys/fs/cgroup/cpuset.cpus.effective"), "0-1"},
                { new FileInfo("/sys/fs/cgroup/cpu.max"), "20000 100000"},
                { new FileInfo("/sys/fs/cgroup/cpu.weight"), "4"},
                { new FileInfo("/sys/fs/cgroup/memory.max"), "100000" }
            }))
            .AddResourceMonitoring(x => x.ConfigureMonitor(section))

            // Ingesting LinuxUtilizationParser with cgroup v2 support.
            .Replace(ServiceDescriptor.Singleton<ILinuxUtilizationParser, LinuxUtilizationParserCgroupV2>())
            .BuildServiceProvider();

        var provider = services.GetService<ISnapshotProvider>();
        Assert.NotNull(provider);
        Assert.Equal(0.1, Math.Round(provider.Resources.GuaranteedCpuUnits, 1)); // hack to make hardcoded calculation in resource utilization main package work.
        Assert.Equal(0.2d, Math.Round(provider.Resources.MaximumCpuUnits, 1)); // read from cpuset.cpus
        Assert.Equal(100_000UL, provider.Resources.GuaranteedMemoryInBytes); // read from memory.max

        // The main usage of this library is containers.
        // To not break the contaract with the main package, we need to set the maximum memory value to the guaranteed memory.
        Assert.Equal(100_000UL, provider.Resources.MaximumMemoryInBytes);
    }

    [ConditionalFact(Skip = "Flaky test, see https://github.com/dotnet/extensions/issues/3997")]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux specific package.")]
    public Task ResourceUtilizationTracker_Reports_The_Same_Values_As_One_Can_Observe_From_Gauges()
    {
        var cpuRefresh = TimeSpan.FromMinutes(13);
        var memoryRefresh = TimeSpan.FromMinutes(14);
        var fileSystem = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
            {
                { new FileInfo("/sys/fs/cgroup/memory/memory.limit_in_bytes"), "100000" },
                { new FileInfo("/sys/fs/cgroup/memory/memory.usage_in_bytes"), "450000" },
                { new FileInfo("/proc/stat"), "cpu  10 10 10 10 10 10 10 10 10 10"},
                { new FileInfo("/sys/fs/cgroup/cpuacct/cpuacct.usage"), "102312"},
                { new FileInfo("/proc/meminfo"), "MemTotal: 102312 kB"},
                { new FileInfo("/sys/fs/cgroup/cpuset/cpuset.cpus"), "0-19"},
                { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_quota_us"), "12"},
                { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_period_us"), "6"},
                { new FileInfo("/sys/fs/cgroup/memory/memory.stat"), "total_inactive_file 100"},
            });

        using var listener = new MeterListener();
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var cpuFromGauge = 0.0d;
        var memoryFromGauge = 0.0d;
        using var e = new ManualResetEventSlim();

        object? meterScope = null;
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (!ReferenceEquals(instrument.Meter.Scope, meterScope))
            {
                return;
            }

            if (instrument.Name == ResourceUtilizationInstruments.CpuUtilization ||
                instrument.Name == ResourceUtilizationInstruments.MemoryUtilization)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<double>((m, f, _, _) =>
        {
            if (m.Name == ResourceUtilizationInstruments.CpuUtilization)
            {
                cpuFromGauge = f;
            }
            else if (m.Name == ResourceUtilizationInstruments.MemoryUtilization)
            {
                memoryFromGauge = f;
            }
        });

        listener.Start();

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(x =>
                x.AddLogging()
                .AddSingleton<TimeProvider>(clock)
                .AddSingleton<IUserHz>(new FakeUserHz(100))
                .AddSingleton<IFileSystem>(fileSystem)
                .AddSingleton<IResourceUtilizationPublisher>(new GenericPublisher(_ => e.Set()))
                .AddResourceMonitoring())
            .Build();

        meterScope = host.Services.GetRequiredService<IMeterFactory>();
        var tracker = host.Services.GetService<IResourceMonitor>();
        Assert.NotNull(tracker);

        _ = host.RunAsync();

        listener.RecordObservableInstruments();

        var utilization = tracker.GetUtilization(TimeSpan.FromSeconds(5));

        Assert.Equal(double.NaN, utilization.CpuUsedPercentage);
        Assert.Equal(100, utilization.MemoryUsedPercentage);
        Assert.Equal(utilization.CpuUsedPercentage, cpuFromGauge);
        Assert.Equal(utilization.MemoryUsedPercentage, memoryFromGauge);

        fileSystem.ReplaceFileContent(new FileInfo("/sys/fs/cgroup/memory/memory.usage_in_bytes"), "50100");
        fileSystem.ReplaceFileContent(new FileInfo("/proc/stat"), "cpu  11 10 10 10 10 10 10 10 10 10");
        fileSystem.ReplaceFileContent(new FileInfo("/sys/fs/cgroup/cpuacct/cpuacct.usage"), "112312");

        clock.Advance(TimeSpan.FromSeconds(6));
        listener.RecordObservableInstruments();

        e.Wait();

        utilization = tracker.GetUtilization(TimeSpan.FromSeconds(5));

        Assert.Equal(1, utilization.CpuUsedPercentage);
        Assert.Equal(utilization.CpuUsedPercentage, cpuFromGauge);
        Assert.Equal(50, utilization.MemoryUsedPercentage);
        Assert.Equal(utilization.MemoryUsedPercentage, memoryFromGauge);

        return Task.CompletedTask;
    }
}
