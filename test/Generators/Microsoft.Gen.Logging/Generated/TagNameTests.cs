// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Logging.Testing;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class TagNameTests
{
    [Fact]
    public void Basic()
    {
        var logger = new FakeLogger();

        TagNameExtensions.M0(logger, 0);
        logger.Collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(new Dictionary<string, string?>
        {
            ["TN1"] = "0",
        });

        logger.Collector.Clear();
        TagNameExtensions.M1(logger, 0);
        logger.Collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(new Dictionary<string, string?>
        {
            ["foo.bar"] = "0",
            ["{OriginalFormat}"] = "{foo.bar}",
        });

        Assert.Equal("0", logger.Collector.LatestRecord.Message);
    }
}
