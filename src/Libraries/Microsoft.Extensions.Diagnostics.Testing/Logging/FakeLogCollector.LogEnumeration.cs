// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Logging.Testing;

public partial class FakeLogCollector
{
    private readonly List<TaskCompletionSource<bool>> _streamWaiters = [];

    public IAsyncEnumerable<FakeLogRecord> GetLogsAsync(
        bool continueOnMultipleEnumerations = false,
        CancellationToken cancellationToken = default)
            => new LogAsyncEnumerable(continueOnMultipleEnumerations, this, cancellationToken);

    private class LogAsyncEnumerable : IAsyncEnumerable<FakeLogRecord>
    {
        internal readonly object EnumeratorLock = new();
        internal int LastIndex;
        internal StreamEnumerator? Enumerator;

        private readonly bool _continueOnMultipleEnumerations;
        private readonly FakeLogCollector _collector;
        private readonly CancellationToken _externalToken;

        internal LogAsyncEnumerable(
            bool continueOnMultipleEnumerations,
            FakeLogCollector collector,
            CancellationToken externalToken)
        {
            _continueOnMultipleEnumerations = continueOnMultipleEnumerations;
            _collector = collector;
            _externalToken = externalToken;
        }

        public IAsyncEnumerator<FakeLogRecord> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            lock (EnumeratorLock)
            {
                int startingPos = _continueOnMultipleEnumerations ? LastIndex : 0;

                var linked = CancellationTokenSource.CreateLinkedTokenSource(_externalToken, cancellationToken);
                Enumerator = new StreamEnumerator(
                    startingPos,
                    this,
                    _collector,
                    linked);
            }

            return Enumerator;
        }
    }

    private sealed class StreamEnumerator : IAsyncEnumerator<FakeLogRecord>
    {
        internal int Index;
        private readonly LogAsyncEnumerable _logAsyncEnumerable;

        private readonly FakeLogCollector _collector;
        private readonly CancellationTokenSource _cts;
        private readonly CancellationToken _cancellationToken;
        private TaskCompletionSource<bool>? _waitingTsc;
        private bool _disposed;
        private FakeLogRecord? _current;

        public StreamEnumerator(
            int startingPos,
            LogAsyncEnumerable logAsyncEnumerable,
            FakeLogCollector collector,
            CancellationTokenSource cts)
        {
            _collector = collector;
            _cts = cts;
            _cancellationToken = cts.Token;
            Index = startingPos;
            _logAsyncEnumerable = logAsyncEnumerable;
        }

        public FakeLogRecord Current => _current ?? throw new InvalidOperationException("Enumeration not started.");

        public async ValueTask<bool> MoveNextAsync()
        {
            ThrowIfDisposed();

            while (true)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                lock (_collector._records)
                {
                    if (Index < _collector._records.Count)
                    {
                        _logAsyncEnumerable.LastIndex = Index;
                        _current = _collector._records[Index++];
                        return true;
                    }

                    _waitingTsc ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _collector._streamWaiters.Add(_waitingTsc);
                }

                try
                {
                    await (_waitingTsc?.Task ?? Task.CompletedTask).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
                finally
                {
                    _waitingTsc = null;
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

            _cts.Cancel();

            if (_waitingTsc is not null)
            {
                lock (_collector._records)
                {
                    _ = _collector._streamWaiters.Remove(_waitingTsc);
                }

                _ = _waitingTsc.TrySetCanceled(_cancellationToken);
            }

            _cts.Dispose();

            lock (_logAsyncEnumerable.EnumeratorLock)
            {
                _logAsyncEnumerable.Enumerator = null;
            }

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
