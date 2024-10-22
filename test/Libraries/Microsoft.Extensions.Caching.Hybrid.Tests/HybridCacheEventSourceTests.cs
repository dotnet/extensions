// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Hybrid.Internal;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public class HybridCacheEventSourceTests
{
    [Fact]
    public void MatchesNameAndGuid()
    {
        // Arrange & Act
        using var eventSource = new HybridCacheEventSource();

        // Assert
        Assert.Equal("HybridCache", eventSource.Name);
        Assert.Equal(Guid.Parse("447667be-e2b5-4962-b3b8-f2c591ec517c"), eventSource.Guid);
    }
}
