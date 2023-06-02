// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Metering.Test;

public class EventCountersExtensionsTest
{
    [Fact]
    public void AddEventCounterCollector_Throws_WhenServiceCollectionNull()
    {
        Assert.Throws<ArgumentNullException>(() => EventCountersExtensions.AddEventCounterCollector(null!, Mock.Of<IConfigurationSection>()));
        Assert.Throws<ArgumentNullException>(() => EventCountersExtensions.AddEventCounterCollector(null!, options => { }));
    }

    [Fact]
    public void AddEventCounterCollector_Throws_WhenActionNull()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddEventCounterCollector((Action<EventCountersCollectorOptions>)null!));
    }

    [Fact]
    public void AddEventCounterCollector_Throws_WhenConfigSectionNull()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddEventCounterCollector((IConfigurationSection)null!));
    }

    [Fact]
    public async Task AddEventCounterCollector_ValidatesOptionsInSection()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureAppConfiguration(static x => x.AddJsonFile("appsettings.json"))
            .ConfigureServices(static (context, services) => services
                .AddEventCounterCollector(context.Configuration.GetSection("InvalidConfig")))
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.RunAsync());
    }

    [Fact]
    public async Task AddEventCounterCollector_ValidatesNullCountersInOptions()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(static services => services
                .AddEventCounterCollector(static x => x.Counters = null!))
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.RunAsync());
    }

    [Fact]
    public async Task AddEventCounterCollector_ValidatesCountersNullValueInOptions()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(static services => services
                .AddEventCounterCollector(static x => x.Counters.Add("key", null!)))
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.RunAsync());
    }

    [Fact]
    public async Task AddEventCounterCollector_ValidatesCountersEmptyValueInOptions()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(static services => services
                .AddEventCounterCollector(static x => x.Counters.Add("key", new HashSet<string>())))
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.RunAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(601)]
    public async Task AddEventCounterCollector_ValidatesSamplingIntervalInOptions(int interval)
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddEventCounterCollector(x => x.SamplingInterval = TimeSpan.FromSeconds(interval)))
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.RunAsync());
    }

    [Fact]
    public void AddEventCounterCollector_AddsOptions()
    {
        var services = new ServiceCollection();
        services.AddEventCounterCollector(static x => x.Counters.Add("key", new SortedSet<string> { "foo" }));

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<EventCountersCollectorOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.Value);
        Assert.NotNull(options.Value.Counters);
        Assert.NotEmpty(options.Value.Counters);
        Assert.Equal(1D, options.Value.SamplingInterval.TotalSeconds);
    }

    [Fact]
    public void AddEventCounterCollectorWithAction_AddsOptions()
    {
        var services = new ServiceCollection();
        services.AddEventCounterCollector(static o =>
            o.Counters.Add("eventSource", new HashSet<string> { "eventCounter" }));

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<EventCountersCollectorOptions>>();
        Assert.Contains("eventSource", options.Value.Counters);
    }

    [Fact]
    public void AddEventCounterCollectorWithSection_AddsOptions()
    {
        EventCountersCollectorOptions? opts = null;
        var configRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    { ConfigurationPath.Combine("SectionName", nameof(opts.SamplingInterval)), "00:01:00" },
                    { ConfigurationPath.Combine("SectionName", nameof(opts.Counters), "Key1", "0"), "foo" },
                    { ConfigurationPath.Combine("SectionName", nameof(opts.Counters), "Key1", "1"), "bar" },
                    { ConfigurationPath.Combine("SectionName", nameof(opts.Counters), "Key1", "2"), "bar" }, // Duplicate value
                    { ConfigurationPath.Combine("SectionName", nameof(opts.Counters), "Key2", "0"), "baz" },
                })
            .Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEventCounterCollector(configRoot.GetSection("SectionName"));

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<EventCountersCollectorOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.Value);

        opts = options.Value;
        Assert.NotNull(opts.Counters);
        Assert.Equal(2, opts.Counters.Count);
        Assert.Equal(2, opts.Counters["Key1"].Count);
        Assert.Equal(1, opts.Counters["Key2"].Count);

        Assert.Contains("foo", opts.Counters["Key1"]);
        Assert.Contains("bar", opts.Counters["Key1"]);
        Assert.Contains("baz", opts.Counters["Key2"]);
        Assert.Equal(1D, opts.SamplingInterval.TotalMinutes);
    }

    [Fact]
    public void AddEventCounterCollectorWithSectionFromFile_AddsOptions()
    {
        var configRoot = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEventCounterCollector(configRoot.GetSection("ValidConfig"));

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<EventCountersCollectorOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.Value);

        var opts = options.Value;
        Assert.NotNull(opts.Counters);
        Assert.Equal(2, opts.Counters.Count);
        Assert.Equal(4, opts.Counters["Key1"].Count);
        Assert.Equal(1, opts.Counters["Key2"].Count);

        Assert.Contains("one", opts.Counters["Key1"]);
        Assert.Contains("two", opts.Counters["Key1"]);
        Assert.Contains("three", opts.Counters["Key1"]);
        Assert.Contains("four", opts.Counters["Key1"]);
        Assert.Contains("ABC", opts.Counters["Key2"]);
        Assert.Equal(2D, opts.SamplingInterval.TotalMinutes);
    }
}
