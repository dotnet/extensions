// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

[EventSource(Name = "HybridCache", Guid = "447667be-e2b5-4962-b3b8-f2c591ec517c")]
internal sealed class HybridCacheEventSource : EventSource
{
    public static readonly HybridCacheEventSource Log = new();

    private const int EventIdLocalCacheHit = 1;
    private const int EventIdLocalCacheMiss = 2;
    private const int EventIdDistributedCacheGet = 3;
    private const int EventIdDistributedCacheHit = 4;
    private const int EventIdDistributedCacheMiss = 5;
    private const int EventIdDistributedCacheFailed = 6;
    private const int EventIdBackendExecuteStart = 7;
    private const int EventIdBackendExecuteComplete = 8;
    private const int EventIdBackendExecuteFailed = 9;
    private const int EventIdLocalCacheWrite = 10;
    private const int EventIdDistributedCacheWrite = 11;
    private const int EventIdStampedeJoin = 12;

    // fast local counters
    private long _totalLocalCacheHit;
    private long _totalLocalCacheMiss;
    private long _totalDistributedCacheHit;
    private long _totalDistributedCacheMiss;
    private long _totalBackendExecute;
    private long _currentBackendExecute;
    private long _currentDistributedFetch;
    private long _totalLocalCacheWrite;
    private long _totalDistributedCacheWrite;
    private long _totalStampedeJoin;

#if !(NETSTANDARD2_0 || NET462)
    // full Counter infrastructure
    private PollingCounter[]? _counters;
#endif

    [NonEvent]
    public void ResetCounters()
    {
        Volatile.Write(ref _totalLocalCacheHit, 0);
        Volatile.Write(ref _totalLocalCacheMiss, 0);
        Volatile.Write(ref _totalDistributedCacheHit, 0);
        Volatile.Write(ref _totalDistributedCacheMiss, 0);
        Volatile.Write(ref _totalBackendExecute, 0);
        Volatile.Write(ref _currentBackendExecute, 0);
        Volatile.Write(ref _currentDistributedFetch, 0);
        Volatile.Write(ref _totalLocalCacheWrite, 0);
        Volatile.Write(ref _totalDistributedCacheWrite, 0);
        Volatile.Write(ref _totalStampedeJoin, 0);
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
        _ = Interlocked.Increment(ref _totalDistributedCacheHit);
        _ = Interlocked.Decrement(ref _currentDistributedFetch);
        WriteEvent(EventIdDistributedCacheHit);
    }

    [Event(EventIdDistributedCacheMiss, Level = EventLevel.Verbose)]
    public void DistributedCacheMiss()
    {
        DebugAssertEnabled();
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

    [Event(EventIdBackendExecuteStart, Level = EventLevel.Verbose)]
    public void BackendExecuteStart()
    {
        // should be followed by BackendExecuteComplete or BackendExecuteFailed
        DebugAssertEnabled();
        _ = Interlocked.Increment(ref _totalBackendExecute);
        _ = Interlocked.Increment(ref _currentBackendExecute);
        WriteEvent(EventIdBackendExecuteStart);
    }

    [Event(EventIdBackendExecuteComplete, Level = EventLevel.Verbose)]
    public void BackendExecuteComplete()
    {
        DebugAssertEnabled();
        _ = Interlocked.Decrement(ref _currentBackendExecute);
        WriteEvent(EventIdBackendExecuteComplete);
    }

    [Event(EventIdBackendExecuteFailed, Level = EventLevel.Error)]
    public void BackendExecuteFailed()
    {
        DebugAssertEnabled();
        _ = Interlocked.Decrement(ref _currentBackendExecute);
        WriteEvent(EventIdBackendExecuteFailed);
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

#if !(NETSTANDARD2_0 || NET462)
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Lifetime exceeds obvious scope; handed to event source")]
    [NonEvent]
    protected override void OnEventCommand(EventCommandEventArgs command)
    {
        if (command.Command == EventCommand.Enable)
        {
            // lazily create counters on first Enable
            _counters ??= [
                new("total-local-cache-hits", this, () => Volatile.Read(ref _totalLocalCacheHit)) { DisplayName = "Total Local Cache Hits" },
                new("total-local-cache-misses", this, () => Volatile.Read(ref _totalLocalCacheMiss)) { DisplayName = "Total Local Cache Misses" },
                new("total-distributed-cache-hits", this, () => Volatile.Read(ref _totalDistributedCacheHit)) { DisplayName = "Total Distributed Cache Hits" },
                new("total-distributed-cache-misses", this, () => Volatile.Read(ref _totalDistributedCacheMiss)) { DisplayName = "Total Distributed Cache Misses" },
                new("total-data-execute", this, () => Volatile.Read(ref _totalBackendExecute)) { DisplayName = "Total Data Executions" },
                new("current-data-execute", this, () => Volatile.Read(ref _currentBackendExecute)) { DisplayName = "Current Data Executions" },
                new("current-distributed-cache-fetches", this, () => Volatile.Read(ref _currentDistributedFetch)) { DisplayName = "Current Distributed Cache Fetches" },
                new("total-local-cache-writes", this, () => Volatile.Read(ref _totalLocalCacheWrite)) { DisplayName = "Total Local Cache Writes" },
                new("total-distributed-cache-writes", this, () => Volatile.Read(ref _totalDistributedCacheWrite)) { DisplayName = "Total Distributed Cache Writes" },
                new("total-stampede-joins", this, () => Volatile.Read(ref _totalStampedeJoin)) { DisplayName = "Total Stampede Joins" },
            ];
        }
    }
#endif

    [NonEvent]
    [Conditional("DEBUG")]
    private void DebugAssertEnabled([CallerMemberName] string caller = "")
    {
        Debug.Assert(IsEnabled(), $"Missing check to {nameof(HybridCacheEventSource)}.{nameof(Log)}.{nameof(IsEnabled)} from {caller}");
    }
}
