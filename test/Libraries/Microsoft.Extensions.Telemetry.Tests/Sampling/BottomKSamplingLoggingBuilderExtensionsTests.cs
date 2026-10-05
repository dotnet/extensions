// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Sampling;

public class BottomKSamplingLoggingBuilderExtensionsTests
{
    [Fact]
    public void AddBottomKLogSampling_RegistersSharedBufferAndSampler()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddBottomKLogSampling());

        using ServiceProvider provider = services.BuildServiceProvider();

        BottomKLogBuffer bottomKBuffer = provider.GetRequiredService<BottomKLogBuffer>();
        Assert.Same(bottomKBuffer, provider.GetRequiredService<LogBuffer>());
        Assert.IsType<BottomKLoggingSampler>(provider.GetRequiredService<LoggingSampler>());
    }

    [Fact]
    public void AddBottomKLogSampling_WithDelegate_ConfiguresOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddBottomKLogSampling(options =>
        {
            options.Capacity = 17;
            options.UnseenWeightMode = BottomKUnseenWeightMode.RarestSeen;
        }));

        using ServiceProvider provider = services.BuildServiceProvider();
        BottomKLogSamplingOptions options =
            provider.GetRequiredService<IOptionsMonitor<BottomKLogSamplingOptions>>().CurrentValue;

        Assert.Equal(17, options.Capacity);
        Assert.Equal(BottomKUnseenWeightMode.RarestSeen, options.UnseenWeightMode);
    }

    [Fact]
    public void AddBottomKLogSampling_WithConfigurationSection_BindsOptions()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:BottomK:Capacity"] = "23",
                ["Logging:BottomK:Enabled"] = "false",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging(builder =>
            builder.AddBottomKLogSampling(configuration.GetSection("Logging:BottomK")));

        using ServiceProvider provider = services.BuildServiceProvider();
        BottomKLogSamplingOptions options =
            provider.GetRequiredService<IOptionsMonitor<BottomKLogSamplingOptions>>().CurrentValue;

        Assert.Equal(23, options.Capacity);
        Assert.False(options.Enabled);
    }

    [Fact]
    public void AddBottomKLogSampling_WhenOptionsInvalid_ValidationFails()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
            builder.AddBottomKLogSampling(options => options.FlushInterval = TimeSpan.Zero));

        using ServiceProvider provider = services.BuildServiceProvider();
        IOptionsMonitor<BottomKLogSamplingOptions> options =
            provider.GetRequiredService<IOptionsMonitor<BottomKLogSamplingOptions>>();

        Assert.Throws<OptionsValidationException>(() => options.CurrentValue);
    }

    [Fact]
    public void AddBottomKLogSampling_WhenAnotherBufferIsRegistered_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddLogging(builder =>
            {
                builder.Services.AddSingleton<LogBuffer, StubLogBuffer>();
                builder.AddBottomKLogSampling();
            }));

        Assert.Contains("another log buffer", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddGlobalBuffer_WhenBottomKIsRegistered_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddLogging(builder =>
            {
                builder.AddBottomKLogSampling();
                builder.AddGlobalBuffer();
            }));

        Assert.Contains("another log buffer", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StubLogBuffer : LogBuffer
    {
        public override void Flush()
        {
        }

        public override bool TryEnqueue<TState>(
            IBufferedLogger bufferedLogger,
            in LogEntry<TState> logEntry) => false;
    }
}
#endif
