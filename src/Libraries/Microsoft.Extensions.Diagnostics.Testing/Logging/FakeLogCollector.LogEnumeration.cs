// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Logging.Testing;

public partial class FakeLogCollector
{
    private volatile int _recordCollectionVersion;

    private TaskCompletionSource<object?> _logEnumerationSharedWaiter =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int _waitingEnumeratorCount;

    /// <summary>
    /// Asynchronously enumerates the <see cref="FakeLogRecord"/> instances collected by this <see cref="FakeLogCollector"/>.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the asynchronous enumeration. This token is observed while creating
    /// and iterating the asynchronous sequence.
    /// </param>
    /// <returns>
    /// An <see cref="IAsyncEnumerable{T}"/> that yields <see cref="FakeLogRecord"/> instances as they are written.
    /// The sequence does not have a completion state defined and awaits subsequent elements indefinitely, 
    /// or stops when cancellation is requested.
    /// </returns>
    /// <remarks>
    /// The returned sequence is <c>hot</c>: it streams log records as they become available and may block between
    /// elements while waiting for additional logs to be written. Multiple independent enumerations can be created
    /// by calling this method multiple times.
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the provided <paramref name="cancellationToken"/> or the enumerator's own cancellation token
    /// is canceled while waiting for the next log record.
    /// </exception>
    /// <example>
    /// The following example shows how to consume logs asynchronously:
    /// <code language="csharp"><![CDATA[
    /// var collector = new FakeLogCollector();
    /// using var cts = new CancellationTokenSource();
    /// 
    /// await foreach (var record in collector.GetLogsAsync(cts.Token))
    /// {
    ///     Console.WriteLine($"{record.Level}: {record.Message}");
    /// }
    /// ]]></code>
    /// </example>
    [Experimental(DiagnosticIds.Experiments.Telemetry)]
    public IAsyncEnumerable<FakeLogRecord> GetLogsAsync(CancellationToken cancellationToken = default)
        => new LogAsyncEnumerable(this, cancellationToken);

    private class LogAsyncEnumerable : IAsyncEnumerable<FakeLogRecord>
    {
        private readonly FakeLogCollector _collector;
        private readonly CancellationToken _enumerableCancellationToken;

        internal LogAsyncEnumerable(
            FakeLogCollector collector,
            CancellationToken enumerableCancellationToken)
        {
            _collector = collector;
            _enumerableCancellationToken = enumerableCancellationToken;
        }

        public IAsyncEnumerator<FakeLogRecord> GetAsyncEnumerator(
            CancellationToken enumeratorCancellationToken = default)
                => new StreamEnumerator(_collector, _enumerableCancellationToken, enumeratorCancellationToken);
    }

    private sealed class StreamEnumerator : IAsyncEnumerator<FakeLogRecord>
    {
        private readonly FakeLogCollector _collector;
        private readonly CancellationTokenSource _mainCts;
        private int _index;
        private int _disposed; // 0 = false, 1 = true (int type used for net462 compatibility)
        private int _observedRecordCollectionVersion;

        // Concurrent MoveNextAsync guard
        private int _moveNextActive; // 0 = inactive, 1 = active (int type used for net462 compatibility)

        public StreamEnumerator(
            FakeLogCollector collector,
            CancellationToken enumerableCancellationToken,
            CancellationToken enumeratorCancellationToken)
        {
            _collector = collector;
            _mainCts = enumerableCancellationToken.CanBeCanceled || enumeratorCancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(enumerableCancellationToken, enumeratorCancellationToken)
                : new CancellationTokenSource();
            _observedRecordCollectionVersion = collector._recordCollectionVersion;
        }

        public FakeLogRecord Current
        {
            get => field ?? throw new InvalidOperationException("Enumeration not started.");
            private set;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (Interlocked.CompareExchange(ref _moveNextActive, 1, 0) == 1)
            {
                throw new InvalidOperationException("MoveNextAsync is already in progress. Concurrent calls are not allowed.");
            }

            try
            {
                ThrowIfDisposed();

                var mainCancellationToken = _mainCts.Token;

                mainCancellationToken.ThrowIfCancellationRequested();

                while (true)
                {
                    TaskCompletionSource<object?>? waiter = null;

                    try
                    {
                        mainCancellationToken.ThrowIfCancellationRequested();

                        lock (_collector._records)
                        {
                            int currentVersion = _collector._recordCollectionVersion;
                            if (_observedRecordCollectionVersion != currentVersion)
                            {
                                _index = 0; // based on assumption that version changed on full collection clear
                                _observedRecordCollectionVersion = currentVersion;
                            }

                            if (_index < _collector._records.Count)
                            {
                                Current = _collector._records[_index++];
                                return true;
                            }

                            // waiter needs to be subscribed within records lock
                            // if not: more records could be added in the meantime and the waiter could be stuck waiting even though the index is behind the actual count
                            waiter = _collector._logEnumerationSharedWaiter;
                            _collector._waitingEnumeratorCount++;
                        }

                        // After the wait is complete in normal flow, no need to decrement because the shared waiter will be swapped and counter reset.
                        _ = await waiter.Task.WaitAsync(mainCancellationToken).ConfigureAwait(false);
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

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return default;
            }

            _mainCts.Cancel();
            _mainCts.Dispose();

            return default;
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) == 1)
            {
                throw new ObjectDisposedException(nameof(StreamEnumerator));
            }
        }
    }
}
