// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Logging.Testing;

/// <summary>
/// Collects log records sent to the fake logger.
/// </summary>
[DebuggerDisplay("Count = {Count}, LatestRecord = {LatestRecord}")]
[DebuggerTypeProxy(typeof(FakeLogCollectorDebugView))]
public class FakeLogCollector
{
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


    // TODO TW: add documentation
    /// <summary>
    /// 
    /// </summary>
    /// <param name="endWaiting"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Task which is completed when the condition is fulfilled or when the cancellation is invoked</returns>
    public Task WaitForLogAsync(Func<FakeLogRecord, bool> endWaiting, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(endWaiting);

        Waiter waiter;

        lock (_records)
        {
            if (_records.Count > 0 && endWaiting(LatestRecord))
            {
                return Task.CompletedTask;
            }

            waiter = new Waiter(endWaiting);
            _waiters.Add(waiter);
        }

        bool isCancelled = false;

        if (cancellationToken.CanBeCanceled)
        {
            lock (_records)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    isCancelled = true;
                    _ = _waiters.Remove(waiter);
                }
            }

            _ = cancellationToken.Register(() =>
            {
                lock (_records)
                {
                    _ = _waiters.Remove(waiter);
                }

                // trigger the task from outside the lock
                _ = waiter.TaskSource.TrySetCanceled(cancellationToken);
            });
        }

        if (isCancelled)
        {
            // trigger the task from outside the lock
            _ = waiter.TaskSource.TrySetCanceled(cancellationToken); // TODO TW: <<<< is this correct?
        }

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        return waiter.TaskSource.Task;
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

        List<Waiter>? waitersToWakeUp = null;

        lock (_records)
        {
            _records.Add(record);

            // TODO TW: To consider: 
            GatherWaitersForWaking(ref waitersToWakeUp, record);
        }

        if (waitersToWakeUp is not null)
        {
            foreach (var waiterToWakeUp in waitersToWakeUp)
            {
                // trigger the task from outside the lock
                waiterToWakeUp.TaskSource.SetResult(true);
            }
        }

        _options.OutputSink?.Invoke(_options.OutputFormatter(record));
    }

    private void GatherWaitersForWaking(ref List<Waiter>? waitersToWakeUp, FakeLogRecord currentlyLoggedRecord)
    {
        for (var waiterIndex = _waiters.Count - 1; waiterIndex >= 0; waiterIndex--)
        {
            var waiter = _waiters[waiterIndex];
            if (!waiter.ShouldEndWaiting(currentlyLoggedRecord))
            {
                continue;
            }

            waitersToWakeUp ??= [];
            waitersToWakeUp.Add(waiter);
            _ = _waiters.Remove(waiter);
        }
    }

    internal TimeProvider TimeProvider => _options.TimeProvider;

    private readonly record struct Waiter(Func<FakeLogRecord, bool> ShouldEndWaiting)
    {
        public Func<FakeLogRecord, bool> ShouldEndWaiting { get; } = ShouldEndWaiting;

        // NOTE: In order to avoid potential dead locks, this task should
        // be completed when the main lock is not being held. Otherwise,
        // application code being woken up by the task could potentially
        // call back into the MetricCollector code and thus trigger a deadlock.
        public TaskCompletionSource<bool> TaskSource { get; } = new();
    }
}
