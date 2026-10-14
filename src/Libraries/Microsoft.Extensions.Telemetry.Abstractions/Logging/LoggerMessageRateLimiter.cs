// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Limits how many log records a logging method emits.
/// </summary>
/// <remarks>
/// This type is used by logging methods generated for the <see cref="RateLimitAttribute"/> and isn't intended to be used directly.
/// It behaves like a sliding window rate limiter without a queue.
/// </remarks>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class LoggerMessageRateLimiter
{
    internal const string DroppedCounterName = "logging.rate_limiting.dropped";
    internal const string DroppedCounterUnit = "{log_record}";
    internal const string DroppedCounterDescription = "Number of log records dropped by a logging method's rate limit.";
    internal const string EventNameTagName = "logging.rate_limiting.event_name";
    internal const int MaxPermitLimit = (1 << SumBits) - 1;

    // The state packs the current segment number (high bits) and the number of calls logged in the window (low bits)
    // into one value, so that a rejected call can be decided with a single read.
    private const int SumBits = 24;
    private const long SumMask = (1L << SumBits) - 1;
    private const long SegmentMask = (1L << (64 - SumBits)) - 1;
    private const long HalfSegmentRange = 1L << (64 - SumBits - 1);
    private const long MillisecondsPerSecond = 1000;

    private static readonly ConditionalWeakTable<Meter, Counter<long>> _droppedCounters = new();

    private readonly int _permitLimit;
    private readonly int[] _segmentCounts;
    private readonly long _segmentTicks;
    private readonly long _startTimestamp;
    private readonly Func<long> _getTimestamp;
    private readonly KeyValuePair<string, object?> _eventNameTag;
    private readonly object _lock = new();

    private long _state;
    private MeterCounter? _lastCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerMessageRateLimiter"/> class.
    /// </summary>
    /// <param name="permitLimit">The maximum number of log records per window.</param>
    /// <param name="windowMilliseconds">The length of the window, in milliseconds.</param>
    /// <param name="segmentsPerWindow">The number of equal segments the window is split into.</param>
    /// <param name="eventName">The event name of the logging method, used to tag the dropped log records counter.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="permitLimit"/> is less than 1 or greater than 16,777,215, <paramref name="windowMilliseconds"/> is less than 1,
    /// or <paramref name="segmentsPerWindow"/> is less than 1 or greater than <paramref name="windowMilliseconds"/>.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="eventName"/> is <see langword="null"/>.</exception>
    public LoggerMessageRateLimiter(int permitLimit, int windowMilliseconds, int segmentsPerWindow, string eventName)
        : this(permitLimit, windowMilliseconds, segmentsPerWindow, eventName, Stopwatch.GetTimestamp, Stopwatch.Frequency)
    {
    }

    internal LoggerMessageRateLimiter(
        int permitLimit,
        int windowMilliseconds,
        int segmentsPerWindow,
        string eventName,
        Func<long> getTimestamp,
        long timestampFrequency)
    {
        _permitLimit = Throw.IfOutOfRange(permitLimit, 1, MaxPermitLimit);
        _ = Throw.IfLessThan(windowMilliseconds, 1);
        _ = Throw.IfOutOfRange(segmentsPerWindow, 1, windowMilliseconds);
        _eventNameTag = new(EventNameTagName, Throw.IfNull(eventName));

        _segmentCounts = new int[segmentsPerWindow];
        _segmentTicks = Math.Max(1, windowMilliseconds * timestampFrequency / (MillisecondsPerSecond * segmentsPerWindow));
        _getTimestamp = getTimestamp;
        _startTimestamp = getTimestamp();
    }

    /// <summary>
    /// Tries to acquire permission to emit a log record.
    /// </summary>
    /// <returns><see langword="true"/> if the log record can be emitted; <see langword="false"/> if it must be dropped.</returns>
    public bool TryAcquire()
    {
        long segment = GetCurrentSegment();
        long state = Volatile.Read(ref _state);

        if (GetSegment(state) == segment && GetSum(state) >= _permitLimit)
        {
            return false;
        }

        return TryAcquireCore(segment);
    }

    /// <summary>
    /// Tries to acquire permission to emit a log record, and counts a dropped log record on the given meter.
    /// </summary>
    /// <param name="meter">The meter to count a dropped log record on, or <see langword="null"/> to not count it.</param>
    /// <returns><see langword="true"/> if the log record can be emitted; <see langword="false"/> if it must be dropped.</returns>
    public bool TryAcquire(Meter? meter)
    {
        if (TryAcquire())
        {
            return true;
        }

        if (meter is not null)
        {
            Counter<long> counter = GetDroppedCounter(meter);
            if (counter.Enabled)
            {
                RecordDropped(counter);
            }
        }

        return false;
    }

    private static long GetSegment(long state) => (long)((ulong)state >> SumBits);

    private static int GetSum(long state) => (int)(state & SumMask);

    private static Counter<long> GetOrCreateDroppedCounter(Meter meter)
    {
        lock (_droppedCounters)
        {
            if (!_droppedCounters.TryGetValue(meter, out Counter<long>? counter))
            {
                counter = meter.CreateCounter<long>(DroppedCounterName, DroppedCounterUnit, DroppedCounterDescription);
                _droppedCounters.Add(meter, counter);
            }

            return counter;
        }
    }

    private long GetCurrentSegment() => ((_getTimestamp() - _startTimestamp) / _segmentTicks) & SegmentMask;

    private int GetIndex(long segment) => (int)((segment & SegmentMask) % _segmentCounts.Length);

    private bool TryAcquireCore(long segment)
    {
        lock (_lock)
        {
            long state = _state;
            long storedSegment = GetSegment(state);
            int sum = GetSum(state);

            // Segment numbers wrap around, so compare them modulo the segment range. A difference above half the range means
            // the timestamp was read before another thread moved to a later segment; such a call counts in the later segment.
            long elapsed = (segment - storedSegment) & SegmentMask;
            if (elapsed != 0 && elapsed < HalfSegmentRange)
            {
                if (elapsed >= _segmentCounts.Length)
                {
                    Array.Clear(_segmentCounts, 0, _segmentCounts.Length);
                    sum = 0;
                }
                else
                {
                    for (long i = 1; i <= elapsed; i++)
                    {
                        int index = GetIndex(storedSegment + i);
                        sum -= _segmentCounts[index];
                        _segmentCounts[index] = 0;
                    }
                }

                storedSegment = segment;
            }

            bool acquired = sum < _permitLimit;
            if (acquired)
            {
                _segmentCounts[GetIndex(storedSegment)]++;
                sum++;
            }

            Volatile.Write(ref _state, (storedSegment << SumBits) | (long)sum);
            return acquired;
        }
    }

    private Counter<long> GetDroppedCounter(Meter meter)
    {
        MeterCounter? last = Volatile.Read(ref _lastCounter);
        if (last is not null && ReferenceEquals(last.Meter, meter))
        {
            return last.Counter;
        }

        Counter<long> counter = GetOrCreateDroppedCounter(meter);
        Volatile.Write(ref _lastCounter, new MeterCounter(meter, counter));
        return counter;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RecordDropped(Counter<long> counter) => counter.Add(1, _eventNameTag);

    private sealed class MeterCounter
    {
        public MeterCounter(Meter meter, Counter<long> counter)
        {
            Meter = meter;
            Counter = counter;
        }

        public Meter Meter { get; }

        public Counter<long> Counter { get; }
    }
}
