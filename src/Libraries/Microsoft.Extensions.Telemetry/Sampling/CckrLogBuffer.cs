// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// Buffers records admitted by the CCKR sampler and emits weighted records at period boundaries.
/// </summary>
/// <remarks>
/// Configuration is read through <see cref="IOptionsMonitor{TOptions}"/>. Retain-all policies bypass
/// this buffer so protected records continue through the ordinary logging providers unchanged.
/// </remarks>
[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "The thread-local values do not own disposable resources and remain available for shutdown-time logging.")]
internal sealed class CckrLogBuffer : LogBuffer, IDisposable
{
    private readonly ConcurrentDictionary<string, CategoryReservoir> _categories = new(StringComparer.Ordinal);
    private readonly IDisposable? _optionsChangeToken;
    private readonly TimeProvider _timeProvider;
    private readonly ThreadLocal<PendingAdmission> _pending = new();
    private readonly object _flushClock = new();

    private volatile ReservoirSamplingConfig _currentOptions;
    private DateTimeOffset _lastFlush;
    private int _disposed;

    public CckrLogBuffer(IOptionsMonitor<ReservoirSamplingConfig> options, TimeProvider timeProvider)
    {
        _ = Throw.IfNull(options);
        _timeProvider = Throw.IfNull(timeProvider);
        _currentOptions = Throw.IfMemberNull(options, options.CurrentValue);
        _optionsChangeToken = options.OnChange(OnOptionsChanged);
        _lastFlush = timeProvider.GetUtcNow();
    }

    public CckrLogBuffer(ReservoirSamplingConfig config, TimeProvider timeProvider)
        : this(new FixedOptionsMonitor(Throw.IfNull(config)), timeProvider)
    {
    }

    /// <summary>
    /// Determines whether a record should proceed through the logging pipeline and stores the
    /// decision for the paired buffer operation.
    /// </summary>
    /// <param name="category">The logger category.</param>
    /// <param name="logLevel">The log level.</param>
    /// <param name="eventId">The event identifier.</param>
    /// <returns><see langword="true"/> when the record must continue through the pipeline.</returns>
    public bool Admit(string category, LogLevel logLevel, EventId eventId)
    {
        ReservoirSamplingConfig options = _currentOptions;
        MaybeFlush(options.FlushInterval);

        PendingAdmission pending = CreateAdmission(category, logLevel, eventId, options);
        _pending.Value = pending;

        return pending.Bypass || pending.Admission.Admission.Kind != AdmissionKind.Skip;
    }

    /// <summary>
    /// Determines whether an information-level record should proceed through the logging pipeline.
    /// </summary>
    /// <param name="category">The logger category.</param>
    /// <param name="eventId">The event identifier.</param>
    /// <returns><see langword="true"/> when the record must continue through the pipeline.</returns>
    public bool Admit(string category, EventId eventId)
        => Admit(category, LogLevel.Information, eventId);

    /// <inheritdoc/>
    public override bool TryEnqueue<TState>(IBufferedLogger bufferedLogger, in LogEntry<TState> logEntry)
    {
        PendingAdmission pending = _pending.Value;
        _pending.Value = default;

        if (!pending.Matches(logEntry.Category, logEntry.LogLevel, logEntry.EventId))
        {
            ReservoirSamplingConfig options = _currentOptions;
            MaybeFlush(options.FlushInterval);
            pending = CreateAdmission(logEntry.Category, logEntry.LogLevel, logEntry.EventId, options);
        }

        if (pending.Bypass)
        {
            return false;
        }

        if (pending.Admission.Admission.Kind == AdmissionKind.Skip)
        {
            return true;
        }

        IReadOnlyList<KeyValuePair<string, object?>>? attributes =
            logEntry.State as IReadOnlyList<KeyValuePair<string, object?>>;
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

        if (!pending.Reservoir!.Insert(bufferedLogger, pending.Admission, record))
        {
            SerializedLogRecordFactory.Return(record);
        }

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
            _lastFlush = _timeProvider.GetUtcNow();
        }
    }

    /// <summary>
    /// Flushes retained records and releases per-thread admission state.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        Flush();
        _optionsChangeToken?.Dispose();
    }

    private static bool MatchesCategory(string category, string pattern)
    {
        int wildcard = pattern.IndexOf("*", StringComparison.Ordinal);
        if (wildcard < 0)
        {
            return string.Equals(category, pattern, StringComparison.OrdinalIgnoreCase);
        }

        return category.Length >= pattern.Length - 1
            && category.AsSpan().StartsWith(pattern.AsSpan(0, wildcard), StringComparison.OrdinalIgnoreCase)
            && category.AsSpan().EndsWith(pattern.AsSpan(wildcard + 1), StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesAnyCategory(string category, IList<string> patterns)
    {
        foreach (string pattern in patterns)
        {
            if (MatchesCategory(category, pattern))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldBypass(
        string category,
        LogLevel logLevel,
        EventId eventId,
        ReservoirSamplingConfig options)
    {
        if (!options.Enabled
            || options.RetainAllLogLevels.Contains(logLevel)
            || options.RetainAllEventIds.Contains(eventId.Id)
            || MatchesAnyCategory(category, options.RetainAllCategories))
        {
            return true;
        }

        return options.SampledCategories.Count > 0
            && !MatchesAnyCategory(category, options.SampledCategories);
    }

    private PendingAdmission CreateAdmission(
        string category,
        LogLevel logLevel,
        EventId eventId,
        ReservoirSamplingConfig options)
    {
        if (ShouldBypass(category, logLevel, eventId, options))
        {
            return PendingAdmission.CreateBypass(category, logLevel, eventId.Id);
        }

        CategoryReservoir reservoir = GetCategory(category);
        CckrAdmission admission = reservoir.Admit(eventId, options);
        return PendingAdmission.CreateAdaptive(category, logLevel, eventId.Id, reservoir, admission);
    }

    private CategoryReservoir GetCategory(string category)
        => _categories.GetOrAdd(category, static _ => new CategoryReservoir());

    private void OnOptionsChanged(ReservoirSamplingConfig? options, string? name)
    {
        if (options is not null && string.IsNullOrEmpty(name))
        {
            _currentOptions = options;
        }
    }

    private void MaybeFlush(TimeSpan flushInterval)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        lock (_flushClock)
        {
            if (now < _lastFlush + flushInterval)
            {
                return;
            }

            _lastFlush = now;
        }

        foreach (CategoryReservoir reservoir in _categories.Values)
        {
            reservoir.Flush();
        }
    }

    private readonly struct CckrAdmission
    {
        public CckrAdmission(Admission admission, long generation)
        {
            Admission = admission;
            Generation = generation;
        }

        public Admission Admission { get; }

        public long Generation { get; }
    }

    private readonly struct PendingAdmission
    {
        private PendingAdmission(
            string category,
            LogLevel logLevel,
            int eventId,
            bool bypass,
            CategoryReservoir? reservoir,
            CckrAdmission admission)
        {
            Category = category;
            LogLevel = logLevel;
            EventId = eventId;
            Bypass = bypass;
            Reservoir = reservoir;
            Admission = admission;
        }

        public CckrAdmission Admission { get; }

        public bool Bypass { get; }

        public string? Category { get; }

        public int EventId { get; }

        public LogLevel LogLevel { get; }

        public CategoryReservoir? Reservoir { get; }

        public static PendingAdmission CreateAdaptive(
            string category,
            LogLevel logLevel,
            int eventId,
            CategoryReservoir reservoir,
            CckrAdmission admission)
            => new(category, logLevel, eventId, false, reservoir, admission);

        public static PendingAdmission CreateBypass(string category, LogLevel logLevel, int eventId)
            => new(category, logLevel, eventId, true, null, default);

        public bool Matches(string category, LogLevel logLevel, EventId eventId)
            => Category is not null
                && EventId == eventId.Id
                && LogLevel == logLevel
                && string.Equals(Category, category, StringComparison.Ordinal);
    }

    private sealed class CategoryReservoir
    {
        private readonly object _lock = new();
        private Cckr<int, SerializedLogRecord>? _reservoir;
        private IBufferedLogger? _bufferedLogger;
        private AlgorithmConfiguration _configuration;
        private long _generation;

        public CckrAdmission Admit(EventId eventId, ReservoirSamplingConfig options)
        {
            List<SampledRecord<int, SerializedLogRecord>>? drained = null;
            IBufferedLogger? bufferedLogger = null;
            Admission admission;
            long generation;

            lock (_lock)
            {
                AlgorithmConfiguration configuration = new(options);
                if (_reservoir is null || !_configuration.Equals(configuration))
                {
                    if (_reservoir is not null)
                    {
                        drained = _reservoir.Flush();
                        bufferedLogger = _bufferedLogger;
                    }

                    _configuration = configuration;
                    _reservoir = configuration.CreateReservoir();
                    _generation++;
                }

                admission = _reservoir.Admit(eventId.Id);
                generation = _generation;
            }

            Emit(bufferedLogger, drained);
            return new CckrAdmission(admission, generation);
        }

        public bool Insert(
            IBufferedLogger bufferedLogger,
            CckrAdmission pending,
            SerializedLogRecord record)
        {
            lock (_lock)
            {
                if (pending.Generation != _generation || pending.Admission.Kind == AdmissionKind.Skip)
                {
                    return false;
                }

                _bufferedLogger = bufferedLogger;
                _reservoir!.Insert(record.EventId.Id, pending.Admission, record);
                return true;
            }
        }

        public void Flush()
        {
            List<SampledRecord<int, SerializedLogRecord>>? drained;
            IBufferedLogger? bufferedLogger;

            lock (_lock)
            {
                if (_reservoir is null)
                {
                    return;
                }

                bufferedLogger = _bufferedLogger;
                drained = _reservoir.Flush();
                _generation++;
            }

            Emit(bufferedLogger, drained);
        }

        private static void Emit(
            IBufferedLogger? bufferedLogger,
            List<SampledRecord<int, SerializedLogRecord>>? drained)
        {
            if (bufferedLogger is null || drained is null || drained.Count == 0)
            {
                return;
            }

            var records = new List<BufferedLogRecord>(drained.Count);
            foreach (SampledRecord<int, SerializedLogRecord> sampled in drained)
            {
                SerializedLogRecord serialized = sampled.Payload;
                var attributes = new List<KeyValuePair<string, object?>>(serialized.Attributes.Count + 1);

                int originalFormatIndex = serialized.Attributes.Count;
                for (int i = 0; i < serialized.Attributes.Count; i++)
                {
                    if (string.Equals(serialized.Attributes[i].Key, "{OriginalFormat}", StringComparison.Ordinal))
                    {
                        originalFormatIndex = i;
                        break;
                    }
                }

                for (int i = 0; i < originalFormatIndex; i++)
                {
                    attributes.Add(serialized.Attributes[i]);
                }

                attributes.Add(new KeyValuePair<string, object?>("sampling.count", sampled.SamplingCount));

                for (int i = originalFormatIndex; i < serialized.Attributes.Count; i++)
                {
                    attributes.Add(serialized.Attributes[i]);
                }

                records.Add(new DeserializedLogRecord(
                    serialized.Timestamp,
                    serialized.LogLevel,
                    serialized.EventId,
                    serialized.Exception,
                    serialized.FormattedMessage,
                    attributes));
            }

            try
            {
                bufferedLogger.LogRecords(records);
            }
            finally
            {
                foreach (SampledRecord<int, SerializedLogRecord> sampled in drained)
                {
                    SerializedLogRecordFactory.Return(sampled.Payload);
                }
            }
        }
    }

    private readonly struct AlgorithmConfiguration : IEquatable<AlgorithmConfiguration>
    {
        public AlgorithmConfiguration(ReservoirSamplingConfig options)
        {
            Capacity = options.Capacity;
            PreserveCapacity = options.PreserveCapacity;
            MinPeriodCount = options.MinPeriodCount;
            UnseenWeightMode = options.UnseenWeightMode;
        }

        public int Capacity { get; }

        public long MinPeriodCount { get; }

        public int PreserveCapacity { get; }

        public UnseenWeightMode UnseenWeightMode { get; }

        public Cckr<int, SerializedLogRecord> CreateReservoir()
            => new(Capacity, PreserveCapacity, MinPeriodCount, UnseenWeightMode, seed: null);

        public bool Equals(AlgorithmConfiguration other)
            => Capacity == other.Capacity
                && PreserveCapacity == other.PreserveCapacity
                && MinPeriodCount == other.MinPeriodCount
                && UnseenWeightMode == other.UnseenWeightMode;

        public override bool Equals(object? obj)
            => obj is AlgorithmConfiguration other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(Capacity, PreserveCapacity, MinPeriodCount, UnseenWeightMode);
    }

    private sealed class FixedOptionsMonitor : IOptionsMonitor<ReservoirSamplingConfig>
    {
        public FixedOptionsMonitor(ReservoirSamplingConfig value)
        {
            CurrentValue = value;
        }

        public ReservoirSamplingConfig CurrentValue { get; }

        public ReservoirSamplingConfig Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<ReservoirSamplingConfig, string?> listener) => null;
    }
}
#endif
