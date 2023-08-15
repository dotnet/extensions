// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Testing.Logging.Test;

public class FakeLoggerExtensionsTests
{
    [Fact]
    public void Basic()
    {
        using var serviceProvider = new ServiceCollection()
            .AddFakeLogging()
            .BuildServiceProvider();

        var factory = serviceProvider.GetService<ILoggerFactory>();
        var collector = serviceProvider.GetFakeLogCollector();

        var logger = factory!.CreateLogger("DOT-NET");
        Assert.Equal(0, collector.Count);
        logger.LogError("M1");
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void WithDelegate()
    {
        using var serviceProvider = new ServiceCollection()
            .AddFakeLogging(options => options.FilteredCategories.Add("Storage"))
            .BuildServiceProvider();

        var factory = serviceProvider.GetService<ILoggerFactory>();
        var collector = serviceProvider.GetFakeLogCollector();

        var logger = factory!.CreateLogger("Storage");
        Assert.Equal(0, collector.Count);
        logger.LogError("M1");
        Assert.Equal(1, collector.Count);

        logger = factory.CreateLogger("Network");
        logger.LogError("M2");
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void WithConfig()
    {
        var configRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { $"Logging:{nameof(FakeLogCollectorOptions.FilteredCategories)}:0", "Storage" },
            })
            .Build();

        var section = configRoot.GetSection("Logging");
        using var serviceProvider = new ServiceCollection()
            .AddFakeLogging(section)
            .BuildServiceProvider();

        var factory = serviceProvider.GetService<ILoggerFactory>()!;
        var collector = serviceProvider.GetFakeLogCollector();

        var logger = factory.CreateLogger("Storage");
        Assert.Equal(0, collector.Count);
        logger.LogError("M1");
        Assert.Equal(1, collector.Count);

        logger = factory.CreateLogger("Network");
        logger.LogError("M2");
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void Exception()
    {
        using var serviceProvider = new ServiceCollection()
            .BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => serviceProvider.GetFakeLogCollector());
    }
}
