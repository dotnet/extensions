// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Logging.Testing.Test.Logging;

public class FakeLoggerProviderTests
{
    [Fact]
    public void Basic()
    {
        using var loggerProvider = new FakeLoggerProvider();

        var logger = loggerProvider.CreateLogger("Storage");
        Assert.Equal(logger.Collector, loggerProvider.Collector);
        logger.LogDebug("M1");
        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal("Storage", logger.LatestRecord.Category);

        logger = loggerProvider.CreateLogger(null);
        Assert.Equal(logger.Collector, loggerProvider.Collector);
        logger.LogDebug("M2");
        Assert.Equal(2, logger.Collector.Count);
        Assert.Null(logger.LatestRecord.Category);

        logger = new FakeLogger<FakeLoggerProviderTests>(loggerProvider.Collector);
        Assert.Equal(logger.Collector, loggerProvider.Collector);
        logger.LogDebug("M3");
        Assert.Equal(3, logger.Collector.Count);
        Assert.Equal("Microsoft.Extensions.Logging.Testing.Test.Logging.FakeLoggerProviderTests", logger.LatestRecord.Category);
    }

    [Fact]
    public void ScopeProvider()
    {
        using var provider = new FakeLoggerProvider();
        var l1 = provider.CreateLogger(null);
        using var factory = new LoggerFactory();
        factory.AddProvider(provider);
        var l2 = factory.CreateLogger("Storage");

        l1.LogDebug("M1");
        l2.LogDebug("M2");

        var records = provider.Collector.GetSnapshot();
        Assert.Equal(2, records.Count);
        Assert.Null(records[0].Category);
        Assert.Equal("Storage", records[1].Category);
    }
}
