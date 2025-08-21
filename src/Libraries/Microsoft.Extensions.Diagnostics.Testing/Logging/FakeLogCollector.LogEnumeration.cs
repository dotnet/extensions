// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Logging.Testing;

public partial class FakeLogCollector
{
    private TaskCompletionSource<bool> _logEnumerationSharedWaiter =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int _waitingEnumeratorCount;

    public async Task<int> WaitForLogsAsync(
        Func<FakeLogRecord, bool> predicate,
        int startingIndex = 0,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(predicate);

        int index = startingIndex; // This index may be too weak
        await foreach (var item in GetLogsAsync(startingIndex, timeout, cancellationToken).ConfigureAwait(false))
        {
            if (predicate(item))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    public IAsyncEnumerable<FakeLogRecord> GetLogsAsync(
        int startingIndex = 0,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfOutOfRange(startingIndex, 0, int.MaxValue);

        return new LogAsyncEnumerable(this, startingIndex, timeout, cancellationToken);
    }

    private class LogAsyncEnumerable : IAsyncEnumerable<FakeLogRecord>
    {
        private readonly int _startingIndex;
        private readonly TimeSpan? _timeout;
        private readonly FakeLogCollector _collector;
        private readonly CancellationToken _enumerableCancellationToken;

        internal LogAsyncEnumerable(
            FakeLogCollector collector,
            int startingIndex,
            TimeSpan? timeout,
            CancellationToken enumerableCancellationToken)
        {
            _collector = collector;
            _startingIndex = startingIndex;
            _timeout = timeout;
            _enumerableCancellationToken = enumerableCancellationToken;
        }

        public IAsyncEnumerator<FakeLogRecord> GetAsyncEnumerator(CancellationToken enumeratorCancellationToken = default)
        {
            return new StreamEnumerator(
                _collector,
                _startingIndex,
                _enumerableCancellationToken,
                enumeratorCancellationToken,
                _timeout);
        }
    }

    private sealed class StreamEnumerator : IAsyncEnumerator<FakeLogRecord>
    {
        private readonly FakeLogCollector _collector;

        private readonly CancellationTokenSource _masterCts;
        private readonly CancellationTokenSource? _timeoutCts;

        private FakeLogRecord? _current;
        private int _index; // TODO TW: when the logs are cleared, this index remains at the value...
        private bool _disposed;

        // Concurrent MoveNextAsync guard
        private int _moveNextActive; // 0 = inactive, 1 = active (for net462 compatibility)

        public StreamEnumerator(
            FakeLogCollector collector,
            int startingIndex,
            CancellationToken enumerableCancellationToken,
            CancellationToken enumeratorCancellationToken,
            TimeSpan? timeout = null)
        {
            _collector = collector;
            _index = startingIndex;

            CancellationToken[] cancellationTokens;
            if (timeout.HasValue)
            {
                var timeoutCts = new CancellationTokenSource(timeout.Value);
                _timeoutCts = timeoutCts;
                cancellationTokens = [enumerableCancellationToken, enumeratorCancellationToken, timeoutCts.Token];
            }
            else
            {
                cancellationTokens = [enumerableCancellationToken, enumeratorCancellationToken];
            }

            _masterCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokens);
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
                    TaskCompletionSource<bool>? waiter = null;

                    try
                    {
                        masterCancellationToken.ThrowIfCancellationRequested();

                        lock (_collector._records)
                        {
                            if (_index < _collector._records.Count)
                            {
                                _current = _collector._records[_index++];
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
            _timeoutCts?.Dispose();

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
