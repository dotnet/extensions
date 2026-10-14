// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Logging.Test;

public class RateLimitAttributeTests
{
    [Fact]
    public void HasDefaults()
    {
        var attribute = new RateLimitAttribute(5);

        Assert.Equal(5, attribute.PermitLimit);
        Assert.Equal(1000, attribute.WindowMilliseconds);
        Assert.Equal(1, attribute.SegmentsPerWindow);
    }

    [Fact]
    public void SetsProperties()
    {
        var attribute = new RateLimitAttribute(100)
        {
            WindowMilliseconds = 60_000,
            SegmentsPerWindow = 6,
        };

        Assert.Equal(100, attribute.PermitLimit);
        Assert.Equal(60_000, attribute.WindowMilliseconds);
        Assert.Equal(6, attribute.SegmentsPerWindow);
    }
}
