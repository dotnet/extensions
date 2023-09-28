// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.Testing;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public static class DataClassificationTests
{
    [Fact]
    public static void EnsureRightClassification()
    {
        var c1 = new DataClassificationTestExtensions.C1 { P1 = "p1", P2 = "p2" };

        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        collector.Clear();
        DataClassificationTestExtensions.M2(logger, c1, c1);
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M2", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);
    }
}
