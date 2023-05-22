// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Moq;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Tracing.Test;

public sealed class SamplingExtensionsTests : IDisposable
{
    private readonly ActivitySource _activitySource = new("testTraceSource");

    public void Dispose()
    {
        _activitySource.Dispose();
    }

    [Fact]
    public void AddSampling_NullArguments_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddSampling(Mock.Of<IConfigurationSection>()));
        Assert.Throws<ArgumentNullException>(() =>
            Sdk.CreateTracerProviderBuilder().AddSampling((IConfigurationSection)null!));

        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddSampling(Mock.Of<Action<SamplingOptions>>()));
        Assert.Throws<ArgumentNullException>(() =>
            Sdk.CreateTracerProviderBuilder().AddSampling((IConfigurationSection)null!));
    }

    [Fact]
    public async Task AddSampling_AlwaysOn_Records()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(_activitySource.Name)
                    .AddSampling(o => o.SamplerType = SamplerType.AlwaysOn)))
            .Build();

        using var activity = await RunHostAndActivityAsync(host);

        Assert.True(activity.Recorded);
    }

    [Fact]
    public async Task AddSampling_AlwaysOff_DoesNotRecord()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(_activitySource.Name)
                    .AddSampling(o => o.SamplerType = SamplerType.AlwaysOff)))
            .Build();

        using var activity = await RunHostAndActivityAsync(host);

        Assert.False(activity.Recorded);
    }

    [Fact]
    public async Task AddSampling_TraceIdRatioBased_Probability1_Records()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(_activitySource.Name)
                    .AddSampling(o =>
                    {
                        o.SamplerType = SamplerType.TraceIdRatioBased;
                        o.TraceIdRatioBasedSamplerOptions = new TraceIdRatioBasedSamplerOptions
                        {
                            Probability = 1
                        };
                    })))
            .Build();

        using var activity = await RunHostAndActivityAsync(host);

        Assert.True(activity.Recorded);
    }

    [Fact]
    public async Task AddSampling_TraceIdRatioBased_Probability0_DoesNotRecord()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(_activitySource.Name)
                    .AddSampling(o =>
                    {
                        o.SamplerType = SamplerType.TraceIdRatioBased;
                        o.TraceIdRatioBasedSamplerOptions = new TraceIdRatioBasedSamplerOptions
                        {
                            Probability = 0
                        };
                    })))
            .Build();

        using var activity = await RunHostAndActivityAsync(host);

        Assert.False(activity.Recorded);
    }

    [Fact]
    public async Task AddSampling_TraceIdRatioBased_InvalidProbability_Throws()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(_activitySource.Name)
                    .AddSampling(o =>
                    {
                        o.SamplerType = SamplerType.TraceIdRatioBased;
                        o.TraceIdRatioBasedSamplerOptions = new TraceIdRatioBasedSamplerOptions
                        {
                            Probability = 1.46
                        };
                    })))
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(
            () => RunHostAndActivityAsync(host));
    }

    [Fact]
    public async Task AddSampling_ParentBased_AlwaysOnRootSampler_Records()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(_activitySource.Name)
                    .AddSampling(o =>
                    {
                        o.SamplerType = SamplerType.ParentBased;
                        o.ParentBasedSamplerOptions = new ParentBasedSamplerOptions
                        {
                            RootSamplerType = SamplerType.AlwaysOn
                        };
                    })))
            .Build();

        using var activity = await RunHostAndActivityAsync(host);

        Assert.True(activity.Recorded);
    }

    [Fact]
    public async Task AddSampling_ParentBased_AlwaysOffRootSampler_DoesNotRecord()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(_activitySource.Name)
                    .AddSampling(o =>
                    {
                        o.SamplerType = SamplerType.ParentBased;
                        o.ParentBasedSamplerOptions = new ParentBasedSamplerOptions
                        {
                            RootSamplerType = SamplerType.AlwaysOff
                        };
                    })))
            .Build();

        using var activity = await RunHostAndActivityAsync(host);

        Assert.False(activity.Recorded);
    }

    [Fact]
    public async Task AddSampling_ParentBased_TraceIdRatioBasedRootSampler_Probability1_Records()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(_activitySource.Name)
                    .AddSampling(o =>
                    {
                        o.SamplerType = SamplerType.ParentBased;
                        o.ParentBasedSamplerOptions = new ParentBasedSamplerOptions
                        {
                            RootSamplerType = SamplerType.TraceIdRatioBased
                        };
                        o.TraceIdRatioBasedSamplerOptions = new TraceIdRatioBasedSamplerOptions
                        {
                            Probability = 1
                        };
                    })))
            .Build();

        using var activity = await RunHostAndActivityAsync(host);

        Assert.True(activity.Recorded);
    }

    [Fact]
    public async Task AddSampling_ParentBased_TraceIdRatioBasedRootSampler_Probability0_DoesNotRecord()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(_activitySource.Name)
                    .AddSampling(o =>
                    {
                        o.SamplerType = SamplerType.ParentBased;
                        o.ParentBasedSamplerOptions = new ParentBasedSamplerOptions
                        {
                            RootSamplerType = SamplerType.TraceIdRatioBased
                        };
                        o.TraceIdRatioBasedSamplerOptions = new TraceIdRatioBasedSamplerOptions
                        {
                            Probability = 0
                        };
                    })))
            .Build();

        using var activity = await RunHostAndActivityAsync(host);

        Assert.False(activity.Recorded);
    }

    private async Task<Activity> RunHostAndActivityAsync(IHost host)
    {
        await host.StartAsync();
        var activity = _activitySource.StartActivity("Test");
        await host.StopAsync();
        return activity!;
    }
}
