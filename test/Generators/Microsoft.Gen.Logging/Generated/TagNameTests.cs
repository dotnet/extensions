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

        var expectedState = new Dictionary<string, string?>
        {
            ["TN1"] = "0",
        };

        logger.Collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }
}
