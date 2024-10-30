// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Sampling;
public class SamplingLoggerBuilderExtensionsTests
{
    [Fact]
    public void AddTraceBasedSampling_RegistersInDI()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddTraceBasedSampler();
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var sampler = serviceProvider.GetService<LoggerSampler>();

        Assert.NotNull(sampler);
        Assert.IsType<TraceBasedSampler>(sampler);
    }

    [Fact]
    public void AddRatioBasedSampler_RegistersInDI()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddRatioBasedSampler(1.0);
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var sampler = serviceProvider.GetService<LoggerSampler>();

        Assert.NotNull(sampler);
        Assert.IsType<RatioBasedSampler>(sampler);
    }

    [Fact]
    public void AddSampler_RegistersInDI()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddSampler<MySampler>();
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var sampler = serviceProvider.GetService<LoggerSampler>();

        Assert.NotNull(sampler);
        Assert.IsType<MySampler>(sampler);
    }

    [Fact]
    public void AddSamplerInstance_RegistersInDI()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddSampler(new MySampler());
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var sampler = serviceProvider.GetService<LoggerSampler>();

        Assert.NotNull(sampler);
        Assert.IsType<MySampler>(sampler);
    }

    [Fact]
    public void AddFuncSampler_RegistersInDI()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddSampler(_ => true);
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var sampler = serviceProvider.GetService<LoggerSampler>();

        Assert.NotNull(sampler);
        Assert.IsType<FuncBasedSampler>(sampler);
    }

    [Fact]
    public void WhenArgumentIsNull_Throws()
    {
        var builder = null as ILoggingBuilder;

        var action = () => SamplingLoggerBuilderExtensions.AddTraceBasedSampler(builder!);
        Assert.Throws<ArgumentNullException>(action);

        var action2 = () => SamplingLoggerBuilderExtensions.AddRatioBasedSampler(builder!, 1.0);
        Assert.Throws<ArgumentNullException>(action);

        var action3 = () => SamplingLoggerBuilderExtensions.AddSampler(builder!, (_) => true);
        Assert.Throws<ArgumentNullException>(action);

        var action4 = () => SamplingLoggerBuilderExtensions.AddSampler<MySampler>(builder!);
        Assert.Throws<ArgumentNullException>(action);

        var action5 = () => SamplingLoggerBuilderExtensions.AddSampler(builder!, new MySampler());
        Assert.Throws<ArgumentNullException>(action);
    }

    private class MySampler : LoggerSampler
    {
        public override bool ShouldSample(SamplingParameters _) => true;
    }
}
