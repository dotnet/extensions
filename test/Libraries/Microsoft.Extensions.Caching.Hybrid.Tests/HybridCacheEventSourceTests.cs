// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
#if !NETFRAMEWORK
using Microsoft.Extensions.Caching.Hybrid.Internal;
#endif

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public class HybridCacheEventSourceTests(TestEventListener listener) : IClassFixture<TestEventListener>
{
    // see notes in TestEventListener for context on fixture usage

    [Fact]
    public void MatchesNameAndGuid()
    {
        // Assert
        Assert.Equal("Microsoft-Extensions-HybridCache", listener.Source.Name);
        Assert.Equal(Guid.Parse("b3aca39e-5dc9-5e21-f669-b72225b66cfc"), listener.Source.Guid); // from name
    }

#if !NETFRAMEWORK // testability on netfx is ... ungreat
    [Fact]
    public async Task LocalCacheHit()
    {
        listener.Reset().Source.LocalCacheHit();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdLocalCacheHit, "LocalCacheHit", EventLevel.Verbose);

        await listener.TimeForCounters();
        listener.AssertCounter("total-local-cache-hits", "Total Local Cache Hits", 1);
        listener.AssertRemainingCountersZero();
    }

    [Fact]
    public async Task LocalCacheMiss()
    {
        listener.Reset().Source.LocalCacheMiss();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdLocalCacheMiss, "LocalCacheMiss", EventLevel.Verbose);

        await listener.TimeForCounters();
        listener.AssertCounter("total-local-cache-misses", "Total Local Cache Misses", 1);
        listener.AssertRemainingCountersZero();
    }

    [Fact]
    public async Task DistributedCacheGet()
    {
        listener.Reset().Source.DistributedCacheGet();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdDistributedCacheGet, "DistributedCacheGet", EventLevel.Verbose);

        await listener.TimeForCounters();
        listener.AssertCounter("current-distributed-cache-fetches", "Current Distributed Cache Fetches", 1);
        listener.AssertRemainingCountersZero();
    }

    [Fact]
    public async Task DistributedCacheHit()
    {
        listener.Reset().Source.DistributedCacheGet();
        listener.Reset(resetCounters: false).Source.DistributedCacheHit();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdDistributedCacheHit, "DistributedCacheHit", EventLevel.Verbose);

        await listener.TimeForCounters();
        listener.AssertCounter("total-distributed-cache-hits", "Total Distributed Cache Hits", 1);
        listener.AssertRemainingCountersZero();
    }

    [Fact]
    public async Task DistributedCacheMiss()
    {
        listener.Reset().Source.DistributedCacheGet();
        listener.Reset(resetCounters: false).Source.DistributedCacheMiss();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdDistributedCacheMiss, "DistributedCacheMiss", EventLevel.Verbose);

        await listener.TimeForCounters();
        listener.AssertCounter("total-distributed-cache-misses", "Total Distributed Cache Misses", 1);
        listener.AssertRemainingCountersZero();
    }

    [Fact]
    public async Task DistributedCacheFailed()
    {
        listener.Reset().Source.DistributedCacheGet();
        listener.Reset(resetCounters: false).Source.DistributedCacheFailed();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdDistributedCacheFailed, "DistributedCacheFailed", EventLevel.Error);

        await listener.TimeForCounters();
        listener.AssertRemainingCountersZero();
    }

    [Fact]
    public async Task UnderlyingDataQueryStart()
    {
        listener.Reset().Source.UnderlyingDataQueryStart();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdUnderlyingDataQueryStart, "UnderlyingDataQueryStart", EventLevel.Verbose);

        await listener.TimeForCounters();
        listener.AssertCounter("current-data-query", "Current Data Queries", 1);
        listener.AssertCounter("total-data-query", "Total Data Queries", 1);
        listener.AssertRemainingCountersZero();
    }

    [Fact]
    public async Task UnderlyingDataQueryComplete()
    {
        listener.Reset().Source.UnderlyingDataQueryStart();
        listener.Reset(resetCounters: false).Source.UnderlyingDataQueryComplete();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdUnderlyingDataQueryComplete, "UnderlyingDataQueryComplete", EventLevel.Verbose);

        await listener.TimeForCounters();
        listener.AssertCounter("total-data-query", "Total Data Queries", 1);
        listener.AssertRemainingCountersZero();
    }

    [Fact]
    public async Task UnderlyingDataQueryFailed()
    {
        listener.Reset().Source.UnderlyingDataQueryStart();
        listener.Reset(resetCounters: false).Source.UnderlyingDataQueryFailed();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdUnderlyingDataQueryFailed, "UnderlyingDataQueryFailed", EventLevel.Error);

        await listener.TimeForCounters();
        listener.AssertCounter("total-data-query", "Total Data Queries", 1);
        listener.AssertRemainingCountersZero();
    }

    [Fact]
    public async Task LocalCacheWrite()
    {
        listener.Reset().Source.LocalCacheWrite();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdLocalCacheWrite, "LocalCacheWrite", EventLevel.Verbose);

        await listener.TimeForCounters();
        listener.AssertCounter("total-local-cache-writes", "Total Local Cache Writes", 1);
        listener.AssertRemainingCountersZero();
    }

    [Fact]
    public async Task DistributedCacheWrite()
    {
        listener.Reset().Source.DistributedCacheWrite();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdDistributedCacheWrite, "DistributedCacheWrite", EventLevel.Verbose);

        await listener.TimeForCounters();
        listener.AssertCounter("total-distributed-cache-writes", "Total Distributed Cache Writes", 1);
        listener.AssertRemainingCountersZero();
    }

    [Fact]
    public async Task StampedeJoin()
    {
        listener.Reset().Source.StampedeJoin();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdStampedeJoin, "StampedeJoin", EventLevel.Verbose);

        await listener.TimeForCounters();
        listener.AssertCounter("total-stampede-joins", "Total Stampede Joins", 1);
        listener.AssertRemainingCountersZero();
    }
#endif
}
