// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
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
    public void AddRandomProbabilisticSampler_RegistersInDI()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddRandomProbabilisticSampler(1.0);
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var sampler = serviceProvider.GetService<LoggingSampler>();

        Assert.NotNull(sampler);
        Assert.IsType<RandomProbabilisticSampler>(sampler);
    }

    [Fact]
    public void AddRandomProbabilisticSamplerFromConfiguration_RegistersInDI()
    {
        List<RandomProbabilisticSamplerFilterRule> expectedData =
        [
            new RandomProbabilisticSamplerFilterRule (probability: 1.0, categoryName: "Program.MyLogger", logLevel: LogLevel.Information, eventId: 1, eventName: "number one"),
            new RandomProbabilisticSamplerFilterRule (probability : 0.01, logLevel : LogLevel.Information),
            new RandomProbabilisticSamplerFilterRule (probability : 0.1, logLevel : LogLevel.Warning)
        ];
        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile("appsettings.json");
        IConfigurationRoot configuration = configBuilder.Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddRandomProbabilisticSampler(configuration);
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<RandomProbabilisticSamplerOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
        Assert.Equivalent(expectedData, options.CurrentValue.Rules);
    }

    [Fact]
    public void AddRandomProbabilisticSampler_WhenRulesIsNull_ValidationFails()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddRandomProbabilisticSampler(o => o.Rules = null!);
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<RandomProbabilisticSamplerOptions>>();
        Assert.Throws<OptionsValidationException>(() => options?.CurrentValue.Rules);
    }

    [Theory]
    [InlineData(1.1)]
    [InlineData(-0.1)]
    [InlineData(2)]
    [InlineData(100)]
    public void AddRandomProbabilisticSampler_WhenDelegateIsInvalid_ValidationFails(double invalidProbabilityValue)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddRandomProbabilisticSampler(o => o.Rules.Add(new RandomProbabilisticSamplerFilterRule(invalidProbabilityValue)));
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<RandomProbabilisticSamplerOptions>>();
        Assert.Throws<OptionsValidationException>(() => options?.CurrentValue.Rules);
    }

    [Theory]
    [InlineData(1.1)]
    [InlineData(-0.1)]
    [InlineData(2)]
    [InlineData(100)]
    public void AddRandomProbabilisticSampler_WhenConfigInvalid_ValidationFails(double invalidProbabilityValue)
    {
        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new List<KeyValuePair<string, string?>>
        {
            new KeyValuePair<string, string?>("RandomProbabilisticSampler:Rules:0:Probability", invalidProbabilityValue.ToString(CultureInfo.InvariantCulture))
        });
        IConfigurationRoot configuration = configBuilder.Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddRandomProbabilisticSampler(configuration);
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<RandomProbabilisticSamplerOptions>>();
        Assert.Throws<OptionsValidationException>(() => options?.CurrentValue.Rules);
    }

    [Fact]
    public void AddRandomProbabilisticSamplerFromDelegate_RegistersInDI()
    {
        List<RandomProbabilisticSamplerFilterRule> expectedData =
        [
            new RandomProbabilisticSamplerFilterRule (probability: 1.0, categoryName: "Program.MyLogger", logLevel: LogLevel.Information, eventId: 1, eventName: "number one"),
            new RandomProbabilisticSamplerFilterRule (probability : 0.01, logLevel : LogLevel.Information),
            new RandomProbabilisticSamplerFilterRule (probability : 0.1, logLevel : LogLevel.Warning)
        ];
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddRandomProbabilisticSampler(opts =>
            {
                opts.Rules = expectedData;
            });
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<RandomProbabilisticSamplerOptions>>();
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

        var action2 = () => SamplingLoggerBuilderExtensions.AddRandomProbabilisticSampler(builder!, 1.0);
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
