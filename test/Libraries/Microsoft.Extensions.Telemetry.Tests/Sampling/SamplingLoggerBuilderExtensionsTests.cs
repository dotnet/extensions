// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    public void AddRatioBasedSamplerConfiguration_RegistersInDI()
    {
        List<RatioBasedSamplerFilterRule> expectedData =
        [
            new RatioBasedSamplerFilterRule(1.0, "Program.MyLogger", LogLevel.Information, 1, null),
            new RatioBasedSamplerFilterRule(0.01, null, LogLevel.Information, null, null),
            new RatioBasedSamplerFilterRule(0.1, null, LogLevel.Warning, null, null)
        ];
        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile("appsettings.json");
        IConfigurationRoot configuration = configBuilder.Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddRatioBasedSamplerConfiguration(configuration);
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<RatioBasedSamplerOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
        Assert.Equivalent(expectedData, options.CurrentValue.Rules);
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
