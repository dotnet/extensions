// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

[EventSource(Name = "Microsoft-Extensions-HybridCache")]
internal sealed class HybridCacheEventSource : EventSource
{
    public static readonly HybridCacheEventSource Log = new();

    internal const int EventIdLocalCacheHit = 1;
    internal const int EventIdLocalCacheMiss = 2;
    internal const int EventIdDistributedCacheGet = 3;
    internal const int EventIdDistributedCacheHit = 4;
    internal const int EventIdDistributedCacheMiss = 5;
    internal const int EventIdDistributedCacheFailed = 6;
    internal const int EventIdUnderlyingDataQueryStart = 7;
    internal const int EventIdUnderlyingDataQueryComplete = 8;
    internal const int EventIdUnderlyingDataQueryFailed = 9;
    internal const int EventIdLocalCacheWrite = 10;
    internal const int EventIdDistributedCacheWrite = 11;
    internal const int EventIdStampedeJoin = 12;
    internal const int EventIdUnderlyingDataQueryCanceled = 13;
    internal const int EventIdDistributedCacheCanceled = 14;
    internal const int EventIdTagInvalidated = 15;

    // fast local counters
    private long _totalLocalCacheHit;
    private long _totalLocalCacheMiss;
    private long _totalDistributedCacheHit;
    private long _totalDistributedCacheMiss;
    private long _totalUnderlyingDataQuery;
    private long _currentUnderlyingDataQuery;
    private long _currentDistributedFetch;
    private long _totalLocalCacheWrite;
    private long _totalDistributedCacheWrite;
    private long _totalStampedeJoin;
    private long _totalTagInvalidations;

#if !(NETSTANDARD2_0 || NET462)
    // full Counter infrastructure
    private DiagnosticCounter[]? _counters;
#endif

    [NonEvent]
    public void ResetCounters()
    {
        Debug.WriteLine($"{nameof(HybridCacheEventSource)} counters reset!");

        Volatile.Write(ref _totalLocalCacheHit, 0);
        Volatile.Write(ref _totalLocalCacheMiss, 0);
        Volatile.Write(ref _totalDistributedCacheHit, 0);
        Volatile.Write(ref _totalDistributedCacheMiss, 0);
        Volatile.Write(ref _totalUnderlyingDataQuery, 0);
        Volatile.Write(ref _currentUnderlyingDataQuery, 0);
        Volatile.Write(ref _currentDistributedFetch, 0);
        Volatile.Write(ref _totalLocalCacheWrite, 0);
        Volatile.Write(ref _totalDistributedCacheWrite, 0);
        Volatile.Write(ref _totalStampedeJoin, 0);
        Volatile.Write(ref _totalTagInvalidations, 0);
    }

    [Event(EventIdLocalCacheHit, Level = EventLevel.Verbose)]
    public void LocalCacheHit()
    {
        DebugAssertEnabled();
        _ = Interlocked.Increment(ref _totalLocalCacheHit);
        WriteEvent(EventIdLocalCacheHit);
    }

    [Event(EventIdLocalCacheMiss, Level = EventLevel.Verbose)]
    public void LocalCacheMiss()
    {
        DebugAssertEnabled();
        _ = Interlocked.Increment(ref _totalLocalCacheMiss);
        WriteEvent(EventIdLocalCacheMiss);
    }

    [Event(EventIdDistributedCacheGet, Level = EventLevel.Verbose)]
    public void DistributedCacheGet()
    {
        // should be followed by DistributedCacheHit, DistributedCacheMiss or DistributedCacheFailed
        DebugAssertEnabled();
        _ = Interlocked.Increment(ref _currentDistributedFetch);
        WriteEvent(EventIdDistributedCacheGet);
    }

    [Event(EventIdDistributedCacheHit, Level = EventLevel.Verbose)]
    public void DistributedCacheHit()
    {
        DebugAssertEnabled();

        // note: not concerned about off-by-one here, i.e. don't panic
        // about these two being atomic ref each-other - just the overall shape
        _ = Interlocked.Increment(ref _totalDistributedCacheHit);
        _ = Interlocked.Decrement(ref _currentDistributedFetch);
        WriteEvent(EventIdDistributedCacheHit);
    }

    [Event(EventIdDistributedCacheMiss, Level = EventLevel.Verbose)]
    public void DistributedCacheMiss()
    {
        DebugAssertEnabled();

        // note: not concerned about off-by-one here, i.e. don't panic
        // about these two being atomic ref each-other - just the overall shape
        _ = Interlocked.Increment(ref _totalDistributedCacheMiss);
        _ = Interlocked.Decrement(ref _currentDistributedFetch);
        WriteEvent(EventIdDistributedCacheMiss);
    }

    [Event(EventIdDistributedCacheFailed, Level = EventLevel.Error)]
    public void DistributedCacheFailed()
    {
        DebugAssertEnabled();
        _ = Interlocked.Decrement(ref _currentDistributedFetch);
        WriteEvent(EventIdDistributedCacheFailed);
    }

    [Event(EventIdDistributedCacheCanceled, Level = EventLevel.Verbose)]
    public void DistributedCacheCanceled()
    {
        DebugAssertEnabled();
        _ = Interlocked.Decrement(ref _currentDistributedFetch);
        WriteEvent(EventIdDistributedCacheCanceled);
    }

    [Event(EventIdUnderlyingDataQueryStart, Level = EventLevel.Verbose)]
    public void UnderlyingDataQueryStart()
    {
        // should be followed by UnderlyingDataQueryComplete or UnderlyingDataQueryFailed
        DebugAssertEnabled();
        _ = Interlocked.Increment(ref _totalUnderlyingDataQuery);
        _ = Interlocked.Increment(ref _currentUnderlyingDataQuery);
        WriteEvent(EventIdUnderlyingDataQueryStart);
    }

    [Event(EventIdUnderlyingDataQueryComplete, Level = EventLevel.Verbose)]
    public void UnderlyingDataQueryComplete()
    {
        DebugAssertEnabled();
        _ = Interlocked.Decrement(ref _currentUnderlyingDataQuery);
        WriteEvent(EventIdUnderlyingDataQueryComplete);
    }

    [Event(EventIdUnderlyingDataQueryFailed, Level = EventLevel.Error)]
    public void UnderlyingDataQueryFailed()
    {
        DebugAssertEnabled();
        _ = Interlocked.Decrement(ref _currentUnderlyingDataQuery);
        WriteEvent(EventIdUnderlyingDataQueryFailed);
    }

    [Event(EventIdUnderlyingDataQueryCanceled, Level = EventLevel.Verbose)]
    public void UnderlyingDataQueryCanceled()
    {
        DebugAssertEnabled();
        _ = Interlocked.Decrement(ref _currentUnderlyingDataQuery);
        WriteEvent(EventIdUnderlyingDataQueryCanceled);
    }

    [Event(EventIdLocalCacheWrite, Level = EventLevel.Verbose)]
    public void LocalCacheWrite()
    {
        DebugAssertEnabled();
        _ = Interlocked.Increment(ref _totalLocalCacheWrite);
        WriteEvent(EventIdLocalCacheWrite);
    }

    [Event(EventIdDistributedCacheWrite, Level = EventLevel.Verbose)]
    public void DistributedCacheWrite()
    {
        DebugAssertEnabled();
        _ = Interlocked.Increment(ref _totalDistributedCacheWrite);
        WriteEvent(EventIdDistributedCacheWrite);
    }

    [Event(EventIdStampedeJoin, Level = EventLevel.Verbose)]
    internal void StampedeJoin()
    {
        DebugAssertEnabled();
        _ = Interlocked.Increment(ref _totalStampedeJoin);
        WriteEvent(EventIdStampedeJoin);
    }

    [Event(EventIdTagInvalidated, Level = EventLevel.Verbose)]
    internal void TagInvalidated()
    {
        DebugAssertEnabled();
        _ = Interlocked.Increment(ref _totalTagInvalidations);
        WriteEvent(EventIdTagInvalidated);
    }

#if !(NETSTANDARD2_0 || NET462)
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Lifetime exceeds obvious scope; handed to event source")]
    [NonEvent]
    protected override void OnEventCommand(EventCommandEventArgs command)
    {
        if (command.Command == EventCommand.Enable)
        {
            // lazily create counters on first Enable
            _counters ??= [
                new PollingCounter("total-local-cache-hits", this, () => Volatile.Read(ref _totalLocalCacheHit)) { DisplayName = "Total Local Cache Hits" },
                new PollingCounter("total-local-cache-misses", this, () => Volatile.Read(ref _totalLocalCacheMiss)) { DisplayName = "Total Local Cache Misses" },
                new PollingCounter("total-distributed-cache-hits", this, () => Volatile.Read(ref _totalDistributedCacheHit)) { DisplayName = "Total Distributed Cache Hits" },
                new PollingCounter("total-distributed-cache-misses", this, () => Volatile.Read(ref _totalDistributedCacheMiss)) { DisplayName = "Total Distributed Cache Misses" },
                new PollingCounter("total-data-query", this, () => Volatile.Read(ref _totalUnderlyingDataQuery)) { DisplayName = "Total Data Queries" },
                new PollingCounter("current-data-query", this, () => Volatile.Read(ref _currentUnderlyingDataQuery)) { DisplayName = "Current Data Queries" },
                new PollingCounter("current-distributed-cache-fetches", this, () => Volatile.Read(ref _currentDistributedFetch)) { DisplayName = "Current Distributed Cache Fetches" },
                new PollingCounter("total-local-cache-writes", this, () => Volatile.Read(ref _totalLocalCacheWrite)) { DisplayName = "Total Local Cache Writes" },
                new PollingCounter("total-distributed-cache-writes", this, () => Volatile.Read(ref _totalDistributedCacheWrite)) { DisplayName = "Total Distributed Cache Writes" },
                new PollingCounter("total-stampede-joins", this, () => Volatile.Read(ref _totalStampedeJoin)) { DisplayName = "Total Stampede Joins" },
                new PollingCounter("total-tag-invalidations", this, () => Volatile.Read(ref _totalTagInvalidations)) { DisplayName = "Total Tag Invalidations" },
            ];
        }

        base.OnEventCommand(command);
    }
#endif

    [NonEvent]
    [Conditional("DEBUG")]
    private void DebugAssertEnabled([CallerMemberName] string caller = "")
    {
        Debug.Assert(IsEnabled(), $"Missing check to {nameof(HybridCacheEventSource)}.{nameof(Log)}.{nameof(IsEnabled)} from {caller}");
        Debug.WriteLine($"{nameof(HybridCacheEventSource)}: {caller}"); // also log all event calls, for visibility
    }
}
