// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        var sampler = serviceProvider.GetService<LoggingSampler>();

        Assert.NotNull(sampler);
        Assert.IsType<TraceBasedSampler>(sampler);
    }

    [Fact]
    public void AddProbabilisticSampler_RegistersInDI()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddProbabilisticSampler(1.0);
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var sampler = serviceProvider.GetService<LoggingSampler>();

        Assert.NotNull(sampler);
        Assert.IsType<ProbabilisticSampler>(sampler);
    }

    [Fact]
    public void AddProbabilisticSamplerConfiguration_RegistersInDI()
    {
        List<ProbabilisticSamplerFilterRule> expectedData =
        [
            new ProbabilisticSamplerFilterRule { Probability = 1.0, Category = "Program.MyLogger", LogLevel = LogLevel.Information, EventId = 1, EventName = "number one" },
            new ProbabilisticSamplerFilterRule { Probability = 0.01, LogLevel = LogLevel.Information },
            new ProbabilisticSamplerFilterRule { Probability = 0.1, LogLevel = LogLevel.Warning }
        ];
        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile("appsettings.json");
        IConfigurationRoot configuration = configBuilder.Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddProbabilisticSamplerConfiguration(configuration);
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<ProbabilisticSamplerOptions>>();
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
        var sampler = serviceProvider.GetService<LoggingSampler>();

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
        var sampler = serviceProvider.GetService<LoggingSampler>();

        Assert.NotNull(sampler);
        Assert.IsType<MySampler>(sampler);
    }

    [Fact]
    public void WhenArgumentIsNull_Throws()
    {
        var builder = null as ILoggingBuilder;

        var action = () => SamplingLoggerBuilderExtensions.AddTraceBasedSampler(builder!);
        Assert.Throws<ArgumentNullException>(action);

        var action2 = () => SamplingLoggerBuilderExtensions.AddProbabilisticSampler(builder!, 1.0);
        Assert.Throws<ArgumentNullException>(action);

        var action3 = () => SamplingLoggerBuilderExtensions.AddSampler<MySampler>(builder!);
        Assert.Throws<ArgumentNullException>(action);

        var action4 = () => SamplingLoggerBuilderExtensions.AddSampler(builder!, new MySampler());
        Assert.Throws<ArgumentNullException>(action);
    }

    private class MySampler : LoggingSampler
    {
        public override bool ShouldSample<TState>(in LogEntry<TState> logEntry) => true;
    }
}
