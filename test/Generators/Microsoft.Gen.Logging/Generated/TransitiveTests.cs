// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Logging.Testing;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class TransitiveTests
{
    [Fact]
    public void Basic()
    {
        var logger = new FakeLogger();
        var c = new TransitiveTestExtensions.C0();

        TransitiveTestExtensions.M0(logger, c);

        var expectedState = new Dictionary<string, string?>
        {
            ["p0.P1"] = "V1",
            ["p0.P0.P2"] = "V2",
        };

        logger.Collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);

        TransitiveTestExtensions.M1(logger, c);

        expectedState = new Dictionary<string, string?>
        {
            ["p0.P1"] = "V1",
            ["p0.P0"] = "TS1",
        };

        logger.Collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }
}
