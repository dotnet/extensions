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
        Assert.Equal("Microsoft-Extensions-HybridCache", eventSource.Name);
        Assert.Equal(Guid.Parse("b3aca39e-5dc9-5e21-f669-b72225b66cfc"), eventSource.Guid); // from name
    }
}
