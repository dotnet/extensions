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
using Microsoft.Extensions.Logging.Test;
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

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        LoggingSampler? sampler = serviceProvider.GetService<LoggingSampler>();

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

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        LoggingSampler? sampler = serviceProvider.GetService<LoggingSampler>();

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
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        IOptionsMonitor<RandomProbabilisticSamplerOptions>? options = serviceProvider.GetService<IOptionsMonitor<RandomProbabilisticSamplerOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
        Assert.Equivalent(expectedData, options.CurrentValue.Rules);
    }

    [Fact]
    public void AddRandomProbabilisticSamplerFromConfig_PicksUpConfigChanges()
    {
        List<RandomProbabilisticSamplerFilterRule> initialData =
        [
            new(probability: 1.0, categoryName: "Program.MyLogger", logLevel: LogLevel.Information, eventId: 1, eventName: "number one"),
            new(probability : 0.01, logLevel : LogLevel.Information),
            new(probability : 0.1, logLevel : LogLevel.Warning)
        ];
        List<RandomProbabilisticSamplerFilterRule> updatedData =
        [
            new(probability: 0, logLevel: LogLevel.Information),
            new(probability : 0, logLevel : LogLevel.Information),
            new(probability : 0, logLevel : LogLevel.Warning)
        ];
        string jsonConfig =
            @"
{
  ""RandomProbabilisticSampler"": {
    ""Rules"": [
      {
        ""CategoryName"": ""Program.MyLogger"",
        ""LogLevel"": ""Information"",
        ""EventId"": 1,
        ""EventName"": ""number one"",
        ""Probability"": 1.0
      },
      {
        ""LogLevel"": ""Information"",
        ""Probability"": 0.01
      },
      {
        ""LogLevel"": ""Warning"",
        ""Probability"": 0.1
      }
    ]
  }
}
";

        using var config = TestConfiguration.Create(() => jsonConfig);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddRandomProbabilisticSampler(config);
        });
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        IOptionsMonitor<RandomProbabilisticSamplerOptions>? options = serviceProvider.GetService<IOptionsMonitor<RandomProbabilisticSamplerOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
        Assert.Equivalent(initialData, options.CurrentValue.Rules);

        jsonConfig =
@"
{
  ""RandomProbabilisticSampler"": {
    ""Rules"": [
      {
        ""LogLevel"": ""Information"",
        ""Probability"": 0
      },
      {
        ""LogLevel"": ""Information"",
        ""Probability"": 0
      },
      {
        ""LogLevel"": ""Warning"",
        ""Probability"": 0
      }
    ]
  }
}
";
        config.Reload();

        var sampler = serviceProvider.GetRequiredService<LoggingSampler>() as RandomProbabilisticSampler;
        Assert.NotNull(sampler);
        Assert.Equivalent(updatedData, sampler.LastKnownGoodSamplerRules);
    }

    [Fact]
    public void AddRandomProbabilisticSampler_WhenRulesIsNull_ValidationFails()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddRandomProbabilisticSampler(o => o.Rules = null!);
        });
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        IOptionsMonitor<RandomProbabilisticSamplerOptions>? options = serviceProvider.GetService<IOptionsMonitor<RandomProbabilisticSamplerOptions>>();
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
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        IOptionsMonitor<RandomProbabilisticSamplerOptions>? options = serviceProvider.GetService<IOptionsMonitor<RandomProbabilisticSamplerOptions>>();
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
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        IOptionsMonitor<RandomProbabilisticSamplerOptions>? options = serviceProvider.GetService<IOptionsMonitor<RandomProbabilisticSamplerOptions>>();
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
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        IOptionsMonitor<RandomProbabilisticSamplerOptions>? options = serviceProvider.GetService<IOptionsMonitor<RandomProbabilisticSamplerOptions>>();
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
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        LoggingSampler? sampler = serviceProvider.GetService<LoggingSampler>();

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

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        LoggingSampler? sampler = serviceProvider.GetService<LoggingSampler>();

        Assert.NotNull(sampler);
        Assert.IsType<MySampler>(sampler);
    }

    [Fact]
    public void WhenArgumentIsNull_Throws()
    {
        var builder = null as ILoggingBuilder;

        Func<ILoggingBuilder> action = () => SamplingLoggerBuilderExtensions.AddTraceBasedSampler(builder!);
        Assert.Throws<ArgumentNullException>(action);

        Func<ILoggingBuilder> action2 = () => SamplingLoggerBuilderExtensions.AddRandomProbabilisticSampler(builder!, 1.0);
        Assert.Throws<ArgumentNullException>(action);

        Func<ILoggingBuilder> action3 = () => SamplingLoggerBuilderExtensions.AddSampler<MySampler>(builder!);
        Assert.Throws<ArgumentNullException>(action);

        Func<ILoggingBuilder> action4 = () => SamplingLoggerBuilderExtensions.AddSampler(builder!, new MySampler());
        Assert.Throws<ArgumentNullException>(action);
    }

    private class MySampler : LoggingSampler
    {
        public override bool ShouldSample<TState>(in LogEntry<TState> logEntry) => true;
    }
}
