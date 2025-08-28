// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Logging.Testing;

public partial class FakeLogCollector
{
    private int _recordCollectionVersion = 0;

    private TaskCompletionSource<object?> _logEnumerationSharedWaiter =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int _waitingEnumeratorCount;

    public IAsyncEnumerable<FakeLogRecord> GetLogsAsync(
        int? maxItems = null,
        CancellationToken cancellationToken = default)
    {
        if (maxItems < 0)
        {
            Throw.ArgumentOutOfRangeException(nameof(maxItems), maxItems, "Must be null (unlimited) or non-negative integer value.");
        }

        return new LogAsyncEnumerable(this, maxItems, cancellationToken);
    }

    private class LogAsyncEnumerable : IAsyncEnumerable<FakeLogRecord>
    {
        private readonly FakeLogCollector _collector;
        private readonly int? _maxItems;
        private readonly CancellationToken _enumerableCancellationToken;

        internal LogAsyncEnumerable(
            FakeLogCollector collector,
            int? maxItems,
            CancellationToken enumerableCancellationToken)
        {
            _collector = collector;
            _maxItems = maxItems;
            _enumerableCancellationToken = enumerableCancellationToken;
        }

        public IAsyncEnumerator<FakeLogRecord> GetAsyncEnumerator(
            CancellationToken enumeratorCancellationToken = default)
                => new StreamEnumerator(_collector, _maxItems, _enumerableCancellationToken, enumeratorCancellationToken);
    }

    private sealed class StreamEnumerator : IAsyncEnumerator<FakeLogRecord>
    {
        private readonly FakeLogCollector _collector;
        private readonly int? _maxItems;
        private readonly CancellationTokenSource _masterCts;

        private FakeLogRecord? _current;
        private int _index;
        private bool _disposed;
        private int _observedRecordCollectionVersion;
        private int _returnedItemCount;

        // Concurrent MoveNextAsync guard
        private int _moveNextActive; // 0 = inactive, 1 = active (for net462 compatibility)

        public StreamEnumerator(
            FakeLogCollector collector,
            int? maxItems,
            CancellationToken enumerableCancellationToken,
            CancellationToken enumeratorCancellationToken)
        {
            _collector = collector;
            _maxItems = maxItems;
            _masterCts = enumerableCancellationToken.CanBeCanceled || enumeratorCancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource([enumerableCancellationToken, enumeratorCancellationToken])
                : new CancellationTokenSource();
            _observedRecordCollectionVersion = collector._recordCollectionVersion;
        }

        public FakeLogRecord Current => _current ?? throw new InvalidOperationException("Enumeration not started.");

        public async ValueTask<bool> MoveNextAsync()
        {
            if (Interlocked.CompareExchange(ref _moveNextActive, 1, 0) == 1)
            {
                throw new InvalidOperationException("MoveNextAsync is already in progress. Concurrent calls are not allowed.");
            }

            try
            {
                ThrowIfDisposed();

                var masterCancellationToken = _masterCts.Token;

                masterCancellationToken.ThrowIfCancellationRequested();

                while (true)
                {
                    TaskCompletionSource<object?>? waiter = null;

                    try
                    {
                        masterCancellationToken.ThrowIfCancellationRequested();

                        lock (_collector._records)
                        {
                            if (_observedRecordCollectionVersion != _collector._recordCollectionVersion)
                            {
                                _index = 0; // based on assumption that version changed on full collection clear
                                _observedRecordCollectionVersion = _collector._recordCollectionVersion;
                            }

                            if (_maxItems.HasValue && _returnedItemCount >= _maxItems)
                            {
                                _current = null;
                                return false;
                            }

                            if (_index < _collector._records.Count)
                            {
                                _current = _collector._records[_index++];
                                _returnedItemCount++;
                                return true;
                            }

                            // waiter needs to be subscribed within records lock
                            // if not: more records could be added in the meantime and the waiter could be stuck waiting even though the index is behind the actual count
                            waiter = _collector._logEnumerationSharedWaiter;
                            _collector._waitingEnumeratorCount++;
                        }

                        // Compatibility path for net462: emulate Task.WaitAsync(cancellationToken).
                        // After the wait is complete in normal flow, no need to decrement because the shared waiter will be swapped and counter reset.
                        await AwaitWithCancellationAsync(waiter.Task, masterCancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        if (waiter is not null)
                        {
                            lock (_collector._records)
                            {
                                if (
                                    _collector._waitingEnumeratorCount > 0 // counter can be zero during the cancellation path
                                    && waiter == _collector._logEnumerationSharedWaiter // makes sure we adjust the counter for the same shared waiting session
                                )
                                {
                                    _collector._waitingEnumeratorCount--;
                                }
                            }
                        }

                        throw;
                    }
                }

            }
            finally
            {
                Volatile.Write(ref _moveNextActive, 0);
            }
        }

        private static async Task AwaitWithCancellationAsync(Task task, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled || task.IsCompleted)
            {
                await task.ConfigureAwait(false);
                return;
            }

            var cancelTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration ctr = default;
            try
            {
                ctr = cancellationToken.Register(static s =>
                    ((TaskCompletionSource<bool>)s!).TrySetCanceled(), cancelTcs);

                var completed = await Task.WhenAny(task, cancelTcs.Task).ConfigureAwait(false);
                await completed.ConfigureAwait(false);
            }
            finally
            {
                ctr.Dispose();
            }
        }

        // TODO TW: consider pending wait and exception handling
        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return default;
            }

            _disposed = true;

            _masterCts.Cancel();
            _masterCts.Dispose();

            return default;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(StreamEnumerator));
            }
        }
    }
}
