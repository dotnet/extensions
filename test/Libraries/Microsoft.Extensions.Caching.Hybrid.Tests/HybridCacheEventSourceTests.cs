﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public class HybridCacheEventSourceTests(ITestOutputHelper log, TestEventListener listener) : IClassFixture<TestEventListener>
{
    // see notes in TestEventListener for context on fixture usage

    [SkippableFact]
    public void MatchesNameAndGuid()
    {
        // Assert
        Assert.Equal("Microsoft-Extensions-HybridCache", listener.Source.Name);
        Assert.Equal(Guid.Parse("b3aca39e-5dc9-5e21-f669-b72225b66cfc"), listener.Source.Guid); // from name
    }

    [SkippableFact]
    public async Task LocalCacheHit()
    {
        AssertEnabled();

        listener.Reset().Source.LocalCacheHit();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdLocalCacheHit, "LocalCacheHit", EventLevel.Verbose);

        await AssertCountersAsync();
        listener.AssertCounter("total-local-cache-hits", "Total Local Cache Hits", 1);
        listener.AssertRemainingCountersZero();
    }

    [SkippableFact]
    public async Task LocalCacheMiss()
    {
        AssertEnabled();

        listener.Reset().Source.LocalCacheMiss();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdLocalCacheMiss, "LocalCacheMiss", EventLevel.Verbose);

        await AssertCountersAsync();
        listener.AssertCounter("total-local-cache-misses", "Total Local Cache Misses", 1);
        listener.AssertRemainingCountersZero();
    }

    [SkippableFact]
    public async Task DistributedCacheGet()
    {
        AssertEnabled();

        listener.Reset().Source.DistributedCacheGet();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdDistributedCacheGet, "DistributedCacheGet", EventLevel.Verbose);

        await AssertCountersAsync();
        listener.AssertCounter("current-distributed-cache-fetches", "Current Distributed Cache Fetches", 1);
        listener.AssertRemainingCountersZero();
    }

    [SkippableFact]
    public async Task DistributedCacheHit()
    {
        AssertEnabled();

        listener.Reset().Source.DistributedCacheGet();
        listener.Reset(resetCounters: false).Source.DistributedCacheHit();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdDistributedCacheHit, "DistributedCacheHit", EventLevel.Verbose);

        await AssertCountersAsync();
        listener.AssertCounter("total-distributed-cache-hits", "Total Distributed Cache Hits", 1);
        listener.AssertRemainingCountersZero();
    }

    [SkippableFact]
    public async Task DistributedCacheMiss()
    {
        AssertEnabled();

        listener.Reset().Source.DistributedCacheGet();
        listener.Reset(resetCounters: false).Source.DistributedCacheMiss();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdDistributedCacheMiss, "DistributedCacheMiss", EventLevel.Verbose);

        await AssertCountersAsync();
        listener.AssertCounter("total-distributed-cache-misses", "Total Distributed Cache Misses", 1);
        listener.AssertRemainingCountersZero();
    }

    [SkippableFact]
    public async Task DistributedCacheFailed()
    {
        AssertEnabled();

        listener.Reset().Source.DistributedCacheGet();
        listener.Reset(resetCounters: false).Source.DistributedCacheFailed();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdDistributedCacheFailed, "DistributedCacheFailed", EventLevel.Error);

        await AssertCountersAsync();
        listener.AssertRemainingCountersZero();
    }

    [SkippableFact]
    public async Task DistributedCacheCanceled()
    {
        AssertEnabled();

        listener.Reset().Source.DistributedCacheGet();
        listener.Reset(resetCounters: false).Source.DistributedCacheCanceled();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdDistributedCacheCanceled, "DistributedCacheCanceled", EventLevel.Verbose);

        await AssertCountersAsync();
        listener.AssertRemainingCountersZero();
    }

    [SkippableFact]
    public async Task UnderlyingDataQueryStart()
    {
        AssertEnabled();

        listener.Reset().Source.UnderlyingDataQueryStart();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdUnderlyingDataQueryStart, "UnderlyingDataQueryStart", EventLevel.Verbose);

        await AssertCountersAsync();
        listener.AssertCounter("current-data-query", "Current Data Queries", 1);
        listener.AssertCounter("total-data-query", "Total Data Queries", 1);
        listener.AssertRemainingCountersZero();
    }

    [SkippableFact]
    public async Task UnderlyingDataQueryComplete()
    {
        AssertEnabled();

        listener.Reset().Source.UnderlyingDataQueryStart();
        listener.Reset(resetCounters: false).Source.UnderlyingDataQueryComplete();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdUnderlyingDataQueryComplete, "UnderlyingDataQueryComplete", EventLevel.Verbose);

        await AssertCountersAsync();
        listener.AssertCounter("total-data-query", "Total Data Queries", 1);
        listener.AssertRemainingCountersZero();
    }

    [SkippableFact]
    public async Task UnderlyingDataQueryFailed()
    {
        AssertEnabled();

        listener.Reset().Source.UnderlyingDataQueryStart();
        listener.Reset(resetCounters: false).Source.UnderlyingDataQueryFailed();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdUnderlyingDataQueryFailed, "UnderlyingDataQueryFailed", EventLevel.Error);

        await AssertCountersAsync();
        listener.AssertCounter("total-data-query", "Total Data Queries", 1);
        listener.AssertRemainingCountersZero();
    }

    [SkippableFact]
    public async Task UnderlyingDataQueryCanceled()
    {
        AssertEnabled();

        listener.Reset().Source.UnderlyingDataQueryStart();
        listener.Reset(resetCounters: false).Source.UnderlyingDataQueryCanceled();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdUnderlyingDataQueryCanceled, "UnderlyingDataQueryCanceled", EventLevel.Verbose);

        await AssertCountersAsync();
        listener.AssertCounter("total-data-query", "Total Data Queries", 1);
        listener.AssertRemainingCountersZero();
    }

    [SkippableFact]
    public async Task LocalCacheWrite()
    {
        AssertEnabled();

        listener.Reset().Source.LocalCacheWrite();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdLocalCacheWrite, "LocalCacheWrite", EventLevel.Verbose);

        await AssertCountersAsync();
        listener.AssertCounter("total-local-cache-writes", "Total Local Cache Writes", 1);
        listener.AssertRemainingCountersZero();
    }

    [SkippableFact]
    public async Task DistributedCacheWrite()
    {
        AssertEnabled();

        listener.Reset().Source.DistributedCacheWrite();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdDistributedCacheWrite, "DistributedCacheWrite", EventLevel.Verbose);

        await AssertCountersAsync();
        listener.AssertCounter("total-distributed-cache-writes", "Total Distributed Cache Writes", 1);
        listener.AssertRemainingCountersZero();
    }

    [SkippableFact]
    public async Task StampedeJoin()
    {
        AssertEnabled();

        listener.Reset().Source.StampedeJoin();
        listener.AssertSingleEvent(HybridCacheEventSource.EventIdStampedeJoin, "StampedeJoin", EventLevel.Verbose);

        await AssertCountersAsync();
        listener.AssertCounter("total-stampede-joins", "Total Stampede Joins", 1);
        listener.AssertRemainingCountersZero();
    }

    private void AssertEnabled()
    {
        // including this data for visibility when tests fail - ETW subsystem can be ... weird
        log.WriteLine($".NET {Environment.Version} on {Environment.OSVersion}, {IntPtr.Size * 8}-bit");

        Skip.IfNot(listener.Source.IsEnabled(), "Event source not enabled");
    }

    private async Task AssertCountersAsync()
    {
        var count = await listener.TryAwaitCountersAsync();

        // ETW counters timing can be painfully unpredictable; generally
        // it'll work fine locally, especially on modern .NET, but:
        // CI servers and netfx in particular - not so much. The tests
        // can still observe and validate the simple events, though, which
        // should be enough to be credible that the eventing system is
        // fundamentally working. We're not meant to be testing that
        // the counters system *itself* works!

        Skip.If(count == 0, "No counters received");
    }
}
