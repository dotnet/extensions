// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Logging.Testing;

public partial class FakeLogCollector
{
    private List<TaskCompletionSource<bool>> _logEnumerationWaiters = [];

    public IAsyncEnumerable<FakeLogRecord> GetLogsAsync(
        int startingIndex = 0,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        => new LogAsyncEnumerable(this, startingIndex, timeout, cancellationToken);

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
        private TaskCompletionSource<bool>? _waitingTcs;

        private FakeLogRecord? _current;
        private int _index;
        private bool _disposed;
        private bool _completed;

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
            ThrowIfDisposed();

            if (_completed)
            {
                return false;
            }

            while (true)
            {
                _masterCts.Token.ThrowIfCancellationRequested();

                TaskCompletionSource<bool> waitingTcs;
                CancellationTokenRegistration cancellationTokenRegistration;

                lock (_collector._records)
                {
                    if (_index < _collector._records.Count)
                    {
                        _current = _collector._records[_index++];
                        return true;
                    }

                    waitingTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _waitingTcs = waitingTcs;

                    // pass tcs as state to avoid closure allocation
                    cancellationTokenRegistration = _masterCts.Token.Register(
                        static state => ((TaskCompletionSource<bool>)state!).TrySetCanceled(),
                        waitingTcs);

                    // waiter needs to be added within records lock
                    // if not: more records could be added in the meantime and the waiter could be stuck waiting even though the index is behind the actual count
                    _collector._logEnumerationWaiters.Add(waitingTcs);
                }

                try
                {
                    using (cancellationTokenRegistration)
                    {
                        await waitingTcs.Task.ConfigureAwait(false);                        
                    }
                }
                catch (OperationCanceledException)
                {
                    _completed = true;
                    return false;
                }
                finally
                {
                    _waitingTcs = null;
                }
            }
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return default;
            }

            _disposed = true;

            _masterCts.Cancel();

            var waitingTcs = Interlocked.Exchange(ref _waitingTcs, null);
            if (waitingTcs is not null)
            {
                // TODO TW: explain very well how exactly is this lock is needed
                lock (_collector._records)
                {
                    _ = _collector._logEnumerationWaiters.Remove(waitingTcs);
                }
            }

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
