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
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

public sealed class AcceptanceTestResourceUtilizationLinux
{
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux specific package.")]
    public void Adding_Linux_Resource_Utilization_Allows_To_Query_Snapshot_Provider()
    {
        using var services = new ServiceCollection()
            .AddResourceUtilization(x => x.AddLinuxProvider())
            .BuildServiceProvider();

        var provider = services.GetRequiredService<ISnapshotProvider>();

        Assert.NotEqual(default, provider.Resources);
        Assert.NotEqual(default, provider.GetSnapshot());
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Want to see if it throws on windows.")]
    public async Task Adding_Linux_Resource_Utilization_On_Windows_Throws()
    {
        var e = await Record.ExceptionAsync(async () =>
        {
            var h = FakeHost.CreateBuilder().ConfigureServices((_, s) => s.AddResourceUtilization(x => x.AddLinuxProvider())
                .AddSingleton<IUserHz>(new FakeUserHz(100)))
            .Build();

            await h.StartAsync();
            await h.StopAsync();
        });

        Assert.IsAssignableFrom<NotSupportedException>(e);
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
                new($"{nameof(LinuxResourceUtilizationProviderOptions)}:{nameof(LinuxResourceUtilizationProviderOptions.CpuConsumptionRefreshInterval)}", cpuRefresh.ToString()),
                new($"{nameof(LinuxResourceUtilizationProviderOptions)}:{nameof(LinuxResourceUtilizationProviderOptions.MemoryConsumptionRefreshInterval)}", memoryRefresh.ToString()),
            };

        var section = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build()
            .GetSection(nameof(LinuxResourceUtilizationProviderOptions));

        using var services = new ServiceCollection()
            .AddSingleton<IOperatingSystem>(new FakeOperatingSystem(isLinux: true))
            .AddResourceUtilization(x => x.AddLinuxProvider(section))
            .BuildServiceProvider();

        var options = services.GetRequiredService<IOptions<LinuxResourceUtilizationProviderOptions>>();

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
            .AddSingleton<IOperatingSystem>(new FakeOperatingSystem(isLinux: true))
            .AddResourceUtilization(x => x.AddLinuxProvider(options =>
            {
                options.CpuConsumptionRefreshInterval = cpuRefresh;
                options.MemoryConsumptionRefreshInterval = memoryRefresh;
            }))
            .BuildServiceProvider();

        var options = services.GetRequiredService<IOptions<LinuxResourceUtilizationProviderOptions>>();

        Assert.NotNull(options.Value);
        Assert.Equal(cpuRefresh, options.Value.CpuConsumptionRefreshInterval);
        Assert.Equal(memoryRefresh, options.Value.MemoryConsumptionRefreshInterval);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux specific package.")]
    [SuppressMessage("Minor Code Smell", "S3257:Declarations and initializations should be as concise as possible", Justification = "Broken analyzer.")]
    public void Adding_Linux_Resource_Utilization_With_Section_Registers_SnapshotProvider()
    {
        var cpuRefresh = TimeSpan.FromMinutes(13);
        var memoryRefresh = TimeSpan.FromMinutes(14);

        var config = new KeyValuePair<string, string?>[]
            {
                new($"{nameof(LinuxResourceUtilizationProviderOptions)}:{nameof(LinuxResourceUtilizationProviderOptions.CpuConsumptionRefreshInterval)}", cpuRefresh.ToString()),
                new($"{nameof(LinuxResourceUtilizationProviderOptions)}:{nameof(LinuxResourceUtilizationProviderOptions.MemoryConsumptionRefreshInterval)}", memoryRefresh.ToString()),
            };

        var section = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build()
            .GetSection(nameof(LinuxResourceUtilizationProviderOptions));

        using var services = new ServiceCollection()
            .AddSingleton<IOperatingSystem>(new FakeOperatingSystem(isLinux: true))
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
            }))
            .AddResourceUtilization(x => x.AddLinuxProvider(section))
            .BuildServiceProvider();

        var provider = services.GetService<ISnapshotProvider>();

        Assert.NotNull(provider);
        Assert.Equal(1, provider.Resources.GuaranteedCpuUnits); // hack to make hardcoded calculation in resource utilization main package work.
        Assert.Equal(20.0d, provider.Resources.MaximumCpuUnits); // read from cpuset.cpus
        Assert.Equal(100_000UL, provider.Resources.GuaranteedMemoryInBytes); // read from memory.limit_in_bytes
        Assert.Equal(104_767_488UL, provider.Resources.MaximumMemoryInBytes); // meminfo * 1024
    }

    [ConditionalFact(Skip = "Flaky test, see https://github.com/dotnet/r9/issues/406")]
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

        listener.InstrumentPublished = (i, m) =>
        {
            if (i.Name == LinuxResourceUtilizationCounters.CpuConsumptionPercentage
            || i.Name == LinuxResourceUtilizationCounters.MemoryConsumptionPercentage)
            {
                m.EnableMeasurementEvents(i);
            }
        };

        listener.SetMeasurementEventCallback<double>((m, f, _, _) =>
        {
            if (m.Name == LinuxResourceUtilizationCounters.CpuConsumptionPercentage)
            {
                cpuFromGauge = f;
            }
            else if (m.Name == LinuxResourceUtilizationCounters.MemoryConsumptionPercentage)
            {
                memoryFromGauge = f;
            }
        });

        listener.Start();

        using var host = FakeHost.CreateBuilder().ConfigureServices(x =>
            x.AddLogging()
            .AddSingleton<TimeProvider>(clock)
            .AddSingleton<IOperatingSystem>(new FakeOperatingSystem(isLinux: true))
            .AddSingleton<IUserHz>(new FakeUserHz(100))
            .AddSingleton<IFileSystem>(fileSystem)
            .AddSingleton<IResourceUtilizationPublisher>(new GenericPublisher(_ => e.Set()))
            .AddResourceUtilization(x => x.AddLinuxProvider()))
            .Build();

        var tracker = host.Services.GetService<IResourceUtilizationTracker>();
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
