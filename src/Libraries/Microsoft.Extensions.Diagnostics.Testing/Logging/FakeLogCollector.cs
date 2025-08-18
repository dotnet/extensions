// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Logging.Testing;

/// <summary>
/// Collects log records sent to the fake logger.
/// </summary>
[DebuggerDisplay("Count = {Count}, LatestRecord = {LatestRecord}")]
[DebuggerTypeProxy(typeof(FakeLogCollectorDebugView))]
public class FakeLogCollector
{
    /// <summary>
    /// Arbitrary low number threshold for stack allocation path to avoid stack overflow.
    /// </summary>
    private const int StackAllocThreshold = 100;

    private readonly List<FakeLogRecord> _records = [];
    private readonly FakeLogCollectorOptions _options;

    private readonly List<Waiter> _waiters = []; // modify only under _records lock

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeLogCollector"/> class.
    /// </summary>
    /// <param name="options">The options to control which log records to retain.</param>
    public FakeLogCollector(IOptions<FakeLogCollectorOptions> options)
    {
        _options = Throw.IfNullOrMemberNull(options, options?.Value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeLogCollector"/> class.
    /// </summary>
    public FakeLogCollector()
    {
        _options = new FakeLogCollectorOptions();
    }

    /// <summary>
    /// Creates a new instance of the <see cref="FakeLogCollector"/> class.
    /// </summary>
    /// <param name="options">The options to control which log records to retain.</param>
    /// <returns>The collector.</returns>
    public static FakeLogCollector Create(FakeLogCollectorOptions options)
    {
        return new FakeLogCollector(Options.Options.Create(Throw.IfNull(options)));
    }

    /// <summary>
    /// Removes all accumulated log records from the collector.
    /// </summary>
    public void Clear()
    {
        lock (_records)
        {
            _records.Clear();
        }
    }

    /// <summary>
    /// Gets the records that are held by the collector.
    /// </summary>
    /// <param name="clear"><see langword="true" /> to atomically clear the set of accumulated log records; otherwise, <see langword="false" />.</param>
    /// <returns>
    /// The list of records tracked to date by the collector.
    /// </returns>
    public IReadOnlyList<FakeLogRecord> GetSnapshot(bool clear = false)
    {
        lock (_records)
        {
            var records = _records.ToArray();
            if (clear)
            {
                _records.Clear();
            }

            return records;
        }
    }

    /// <summary>
    /// Gets the latest record that was created.
    /// </summary>
    /// <returns>
    /// The latest log record created.
    /// </returns>
    /// <exception cref="InvalidOperationException">No records have been captured.</exception>
    public FakeLogRecord LatestRecord
    {
        get
        {
            lock (_records)
            {
                if (_records.Count == 0)
                {
                    Throw.InvalidOperationException("No records logged.");
                }

                return _records[_records.Count - 1];
            }
        }
    }

    /// <summary>
    /// Gets the number of log records captured by this collector.
    /// </summary>
    public int Count => _records.Count;

    /// <summary>
    /// Allows waiting for the point in time in which a newly processed log record fulfilled custom condition supplied by the caller.
    /// </summary>
    /// <param name="endWaiting">Custom condition terminating waiting upon fulfillment.</param>
    /// <param name="cancellationToken">Token based cancellation of the waiting.</param>
    /// <returns>Awaitable task that completes upon condition fulfillment, timeout expiration or cancellation.</returns>
    public Task WaitForLogAsync(
        Func<FakeLogRecord, bool> endWaiting,
        CancellationToken cancellationToken = default)
    {
        return WaitForLogAsync(endWaiting, null, cancellationToken);
    }

    /// <summary>
    /// Allows waiting for the point in time in which a newly processed log record fulfilled custom condition supplied by the caller.
    /// </summary>
    /// <param name="endWaiting">Custom condition terminating waiting upon fulfillment.</param>
    /// <param name="timeout">TODO TW placeholder</param>
    /// <param name="cancellationToken">Token based cancellation of the waiting.</param>
    /// <returns>Awaitable task that completes upon condition fulfillment, timeout expiration or cancellation.</returns>
    [Experimental(diagnosticId: DiagnosticIds.Experiments.TimeProvider, UrlFormat = DiagnosticIds.UrlFormat)] // TODO TW: << placeholder
    public Task<bool> WaitForLogAsync(
        Func<FakeLogRecord, bool> endWaiting,
        TimeSpan? timeout,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(endWaiting);
        _ = Throw.IfNull(cancellationToken);

        // Before we even start waiting, we check if the cancellation token is already canceled and if yes, we exit early with a canceled task
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<bool>(cancellationToken);
        }

        Waiter waiter;

        lock (_records)
        {
            // Before we even start waiting, we check if the latest record already fulfills the condition and if yes, we exit early with success 
            if (_records.Count > 0 && endWaiting(LatestRecord))
            {
                return Task.FromResult(true);
            }

            // We register the waiter
            waiter = new Waiter(this, endWaiting, timeout);
            _waiters.Add(waiter);
        }

        if (cancellationToken.CanBeCanceled)
        {
            // When the cancellation token is canceled, we resolve the waiter and cancel the awaited task
            _ = cancellationToken.Register(() =>
            {
                waiter.RemoveFromWaiting();

                // trigger the task from outside the lock
                // TODO TW: I don't see it
                waiter.ResolveByCancellation(cancellationToken);
            });
        }

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        return waiter.Task;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
    }

    internal void AddRecord(FakeLogRecord record)
    {
        if (_options.FilteredLevels.Count > 0 && !_options.FilteredLevels.Contains(record.Level))
        {
            // level not being collected
            return;
        }

        if (_options.FilteredCategories.Count > 0)
        {
            if (record.Category == null || !_options.FilteredCategories.Contains(record.Category))
            {
                // no category specified, or not in the list of allowed categories
                return;
            }
        }

        if (!record.LevelEnabled && !_options.CollectRecordsForDisabledLogLevels)
        {
            // record is not enabled and we're not collecting disabled records
            return;
        }

        var customFilter = _options.CustomFilter;
        if (customFilter is not null && !customFilter(record))
        {
            // record was filtered out by a custom filter
            return;
        }

        List<Waiter>? waitersToWakeUp;

        lock (_records)
        {
            _records.Add(record);

            Span<bool> waitersToWakeUpOrderedByIndices = _waiters.Count < StackAllocThreshold
                ? stackalloc bool[_waiters.Count]
                : new bool[_waiters.Count];
            CheckWaiting(record, waitersToWakeUpOrderedByIndices, out waitersToWakeUp);
            for (var i = 0; i < waitersToWakeUpOrderedByIndices.Length; i++)
            {
                if (waitersToWakeUpOrderedByIndices[i])
                {
                    _waiters[i].RemoveFromWaiting(false);
                }
            }
        }

        if (waitersToWakeUp is not null)
        {
            foreach (var waiterToWakeUp in waitersToWakeUp)
            {
                // trigger the task from outside the lock
                waiterToWakeUp.ResolveByResult(true);
            }
        }

        _options.OutputSink?.Invoke(_options.OutputFormatter(record));
    }

    // Must be called inside lock(_records)
    private void CheckWaiting(FakeLogRecord currentlyLoggedRecord, Span<bool> waitersToRemoveOrderedByIndices, out List<Waiter>? waitersToWakeUp)
    {
        waitersToWakeUp = null;

        for (var waiterIndex = _waiters.Count - 1; waiterIndex >= 0; waiterIndex--)
        {
            var waiter = _waiters[waiterIndex];
            if (!waiter.ShouldEndWaiting(currentlyLoggedRecord))
            {
                continue;
            }

            waitersToWakeUp ??= [];
            waitersToWakeUp.Add(waiter);

            waitersToRemoveOrderedByIndices[waiterIndex] = true;
        }
    }

    internal TimeProvider TimeProvider => _options.TimeProvider;

    // TODO TW: I don't think we need/want record struct for this
    // A) it is put into List so we don't benefit from structs ability to live on the stack
    // B) I don't want to compare by fields/field-values, but rather by instance, so don't need record for this (or suboptimal struct for that matter)
    private sealed class Waiter
    {
        public Task<bool> Task => _taskSource.Task;
        public Func<FakeLogRecord, bool> ShouldEndWaiting { get; }

        // TODO TW: I don't see it
        // NOTE: In order to avoid potential deadlocks, this task should
        // be completed when the main lock is not being held. Otherwise,
        // application code being woken up by the task could potentially
        // call back into the FakeLogCollector code and thus trigger a deadlock.
        private readonly TaskCompletionSource<bool> _taskSource;

        private readonly FakeLogCollector _fakeLogCollector;

        private readonly object _timerLock = new();
        private ITimer? _timeoutTimer;

        public Waiter(FakeLogCollector fakeLogCollector, Func<FakeLogRecord, bool> shouldEndWaiting, TimeSpan? timeout)
        {
            ShouldEndWaiting = shouldEndWaiting;
            _fakeLogCollector = fakeLogCollector;
            _taskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _timeoutTimer = timeout.HasValue ? CreateTimoutTimer(fakeLogCollector, timeout.Value) : null;
        }

        public void RemoveFromWaiting(bool performUnderLock = true)
        {
            if (performUnderLock)
            {
                lock(_fakeLogCollector._records)
                {
                    RemoveFromWaitingInternal();
                }

                return;
            }

            RemoveFromWaitingInternal();
        }

        public void ResolveByResult(bool result)
        {
            StopTimer();
            _ = _taskSource.TrySetResult(result);
        }

        public void ResolveByCancellation(CancellationToken cancellationToken)
        {
            StopTimer();
            _ = _taskSource.TrySetCanceled(cancellationToken);
        }

        private void RemoveFromWaitingInternal() => _ = _fakeLogCollector._waiters.Remove(this);

        private void StopTimer()
        {
            lock (_timerLock)
            {
                if (_timeoutTimer is null)
                {
                    return;
                }

                try
                {
                    _timeoutTimer.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // Timer was already disposed
                }
                finally
                {
                    _timeoutTimer = null;
                }
            }
        }

        private ITimer CreateTimoutTimer(FakeLogCollector fakeLogCollector, TimeSpan timeout)
        {
            return fakeLogCollector.TimeProvider
                .CreateTimer(
                    _ =>
                    {
                        RemoveFromWaiting();

                        // trigger the task from outside the lock
                        ResolveByResult(false);
                    },
                    null,
                    timeout, // perform after
                    Timeout.InfiniteTimeSpan // don't repeat
                );
        }
    }
}
