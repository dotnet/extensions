// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.Testing;
using TestClasses;
using Xunit;

using static TestClasses.LogPropertiesRedactionExtensions;

namespace Microsoft.Gen.Logging.Test;

public class PrimaryConstructorTests
{
    [Fact]
    public void FindsLoggerInPrimaryConstructorParameter()
    {
        using var logger = Utils.GetLogger();

        var collector = logger.FakeLogCollector;

        new ClassWithPrimaryConstructor(logger).Test();

        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("Test.", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void FindsLoggerInPrimaryConstructorParameterInDifferentPartialDeclaration()
    {
        using var logger = Utils.GetLogger();

        var collector = logger.FakeLogCollector;

        new ClassWithPrimaryConstructorInDifferentPartialDeclaration(logger).Test();

        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("Test.", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void FindsLoggerInFieldInitializedFromPrimaryConstructorParameter()
    {
        using var logger = Utils.GetLogger();

        var collector = logger.FakeLogCollector;

        new ClassWithPrimaryConstructorAndField(logger).Test();

        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("Test.", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void FindsLoggerInRecordPrimaryConstructorParameter()
    {
        using var logger = Utils.GetLogger();

        var collector = logger.FakeLogCollector;

        new RecordWithPrimaryConstructor(logger).Test();

        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("Test.", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);
    }
}
