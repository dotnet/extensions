// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// A <see cref="LogBuffer"/> implementation backed by the CCKR adaptive reservoir. It plugs into the
/// existing logging pipeline through the standard buffer seam: <see cref="TryEnqueue"/> holds an
/// admitted record in a per-category reservoir instead of writing it, and <see cref="Flush"/> emits
/// the period's kept records &#8212; each carrying its Horvitz-Thompson <c>sampling.count</c> weight
/// &#8212; through the same <see cref="IBufferedLogger"/> callback the global buffer uses.
/// </summary>
/// <remarks>
/// The paired <see cref="CckrLoggingSampler"/> makes the admission decision at the
/// <see cref="LoggingSampler"/> seam and stashes the result for this thread; <see cref="TryEnqueue"/>
/// reuses it so the reservoir is consulted once per record. When used without that sampler,
/// <see cref="TryEnqueue"/> makes the admission decision itself.
/// </remarks>
internal sealed class CckrLogBuffer : LogBuffer, IDisposable
{
    private readonly ConcurrentDictionary<string, CategoryReservoir> _categories = new(StringComparer.Ordinal);
    private readonly ReservoirSamplingConfig _config;
    private readonly TimeProvider _timeProvider;
    private readonly ThreadLocal<PendingAdmission> _pending = new();
    private readonly object _flushClock = new();

    private DateTimeOffset _nextFlush;

    public CckrLogBuffer(ReservoirSamplingConfig config, TimeProvider timeProvider)
    {
        _config = config;
        _timeProvider = timeProvider;
        _nextFlush = timeProvider.GetUtcNow() + config.FlushInterval;
    }

    /// <summary>
    /// Makes and records this thread's admission decision for a callsite. Called from the paired
    /// <see cref="CckrLoggingSampler"/> at the sampling seam, before <see cref="TryEnqueue"/>.
    /// </summary>
    /// <returns><see langword="true"/> if the record should be processed and held; otherwise <see langword="false"/>.</returns>
    public bool Admit(string category, EventId eventId)
    {
        CategoryReservoir reservoir = GetCategory(category);
        Admission admission = reservoir.Admit(eventId);
        _pending.Value = new PendingAdmission(category, eventId.Id, admission);
        return admission.Kind != AdmissionKind.Skip;
    }

    /// <inheritdoc/>
    public override bool TryEnqueue<TState>(IBufferedLogger bufferedLogger, in LogEntry<TState> logEntry)
    {
        string category = logEntry.Category;
        CategoryReservoir reservoir = GetCategory(category);

        // Reuse the admission computed by the paired sampler on this thread; otherwise decide now.
        Admission admission;
        PendingAdmission pending = _pending.Value;
        if (pending.HasValue && pending.EventId == logEntry.EventId.Id && string.Equals(pending.Category, category, StringComparison.Ordinal))
        {
            admission = pending.Admission;
            _pending.Value = default;
        }
        else
        {
            admission = reservoir.Admit(logEntry.EventId);
        }

        if (admission.Kind == AdmissionKind.Skip)
        {
            // Consumed by the reservoir (counted) but not kept: drop without writing.
            MaybeFlush();
            return true;
        }

        IReadOnlyList<KeyValuePair<string, object?>>? attributes = logEntry.State as IReadOnlyList<KeyValuePair<string, object?>>;
        if (attributes is null)
        {
            Throw.InvalidOperationException(
                $"Unsupported type of log state detected: {typeof(TState)}, expected IReadOnlyList<KeyValuePair<string, object?>>");
        }

        SerializedLogRecord record = SerializedLogRecordFactory.Create(
            logEntry.LogLevel,
            logEntry.EventId,
            _timeProvider.GetUtcNow(),
            attributes,
            logEntry.Exception,
            logEntry.Formatter(logEntry.State, logEntry.Exception));

        reservoir.Insert(bufferedLogger, logEntry.EventId, admission, record);

        MaybeFlush();
        return true;
    }

    /// <inheritdoc/>
    public override void Flush()
    {
        foreach (CategoryReservoir reservoir in _categories.Values)
        {
            reservoir.Flush();
        }

        lock (_flushClock)
        {
            _nextFlush = _timeProvider.GetUtcNow() + _config.FlushInterval;
        }
    }

    public void Dispose() => _pending.Dispose();

    private CategoryReservoir GetCategory(string category)
        => _categories.GetOrAdd(category, static (_, cfg) => new CategoryReservoir(cfg), _config);

    private void MaybeFlush()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        lock (_flushClock)
        {
            if (now < _nextFlush)
            {
                return;
            }

            _nextFlush = now + _config.FlushInterval;
        }

        foreach (CategoryReservoir reservoir in _categories.Values)
        {
            reservoir.Flush();
        }
    }

    /// <summary>
    /// This thread's admission decision, carried from the sampler seam to <see cref="TryEnqueue"/>.
    /// </summary>
    private readonly struct PendingAdmission
    {
        public PendingAdmission(string category, int eventId, Admission admission)
        {
            Category = category;
            EventId = eventId;
            Admission = admission;
        }

        public bool HasValue => Category is not null;

        public string? Category { get; }

        public int EventId { get; }

        public Admission Admission { get; }
    }

    /// <summary>
    /// One category's reservoir plus the buffered-logger callback used to emit its flushed records.
    /// </summary>
    private sealed class CategoryReservoir
    {
        private readonly Cckr<int, SerializedLogRecord> _reservoir;
        private readonly object _lock = new();
        private IBufferedLogger? _bufferedLogger;

        public CategoryReservoir(ReservoirSamplingConfig config)
        {
            _reservoir = new Cckr<int, SerializedLogRecord>(
                config.Capacity,
                config.PreserveCapacity,
                config.MinPeriodCount,
                config.UnseenWeightMode,
                seed: null);
        }

        public Admission Admit(EventId eventId)
        {
            lock (_lock)
            {
                return _reservoir.Admit(eventId.Id);
            }
        }

        public void Insert(IBufferedLogger bufferedLogger, EventId eventId, Admission admission, SerializedLogRecord record)
        {
            lock (_lock)
            {
                _bufferedLogger = bufferedLogger;
                _reservoir.Insert(eventId.Id, admission, record);
            }
        }

        public void Flush()
        {
            List<SampledRecord<int, SerializedLogRecord>> drained;
            IBufferedLogger? bufferedLogger;
            lock (_lock)
            {
                bufferedLogger = _bufferedLogger;
                drained = _reservoir.Flush();
            }

            if (bufferedLogger is null || drained.Count == 0)
            {
                return;
            }

            var records = new List<BufferedLogRecord>(drained.Count);
            foreach (SampledRecord<int, SerializedLogRecord> sampled in drained)
            {
                SerializedLogRecord serialized = sampled.Payload;

                var attributes = new List<KeyValuePair<string, object?>>(serialized.Attributes.Count + 1);
                attributes.AddRange(serialized.Attributes);
                attributes.Add(new KeyValuePair<string, object?>("sampling.count", sampled.SamplingCount));

                records.Add(new DeserializedLogRecord(
                    serialized.Timestamp,
                    serialized.LogLevel,
                    serialized.EventId,
                    serialized.Exception,
                    serialized.FormattedMessage,
                    attributes));
            }

            bufferedLogger.LogRecords(records);
        }
    }
}
#endif
