// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace System.Buffers
{
    /// <summary>
    /// Used to allocate and distribute re-usable blocks of memory.
    /// </summary>
    internal class DiagnosticMemoryPool : MemoryPool<byte>
    {
        private readonly MemoryPool<byte> _pool;

        private readonly bool _allowLateReturn;

        private readonly object _syncObj;

        private readonly HashSet<DiagnosticPoolBlock> _blocks;

        private readonly List<Exception> _blockAccessExceptions;

        private readonly TaskCompletionSource<object> _allBlocksRetuned;

        private int _totalBlocks;

        /// <summary>
        /// This default value passed in to Rent to use the default value for the pool.
        /// </summary>
        private const int AnySize = -1;

        public DiagnosticMemoryPool(MemoryPool<byte> pool, bool allowLateReturn = false)
        {
            _pool = pool;
            _allowLateReturn = allowLateReturn;
            _blocks = new HashSet<DiagnosticPoolBlock>();
            _syncObj = new object();
            _allBlocksRetuned = new TaskCompletionSource<object>();
            _blockAccessExceptions = new List<Exception>();
        }

        public bool IsDisposed { get; private set; }

        public override IMemoryOwner<byte> Rent(int size = AnySize)
        {
            lock (_syncObj)
            {
                if (IsDisposed)
                {
                    MemoryPoolThrowHelper.ThrowObjectDisposedException(MemoryPoolThrowHelper.ExceptionArgument.MemoryPool);
                }

                var diagnosticPoolBlock = new DiagnosticPoolBlock(this, _pool.Rent(size));
                _totalBlocks++;
                _blocks.Add(diagnosticPoolBlock);
                return diagnosticPoolBlock;
            }
        }

        public override int MaxBufferSize => _pool.MaxBufferSize;

        internal void Return(DiagnosticPoolBlock block)
        {
            bool returnedAllBlocks;
            lock (_syncObj)
            {
                _blocks.Remove(block);
                returnedAllBlocks = IsDisposed && _blocks.Count == 0;
            }

            if (returnedAllBlocks)
            {
                SetAllBlocksReturned();
            }
        }

        internal void ReportException(Exception exception)
        {
            lock (_syncObj)
            {
                _blockAccessExceptions.Add(exception);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                MemoryPoolThrowHelper.ThrowInvalidOperationException_DoubleDispose();
            }

            bool allBlocksReturned = false;
            try
            {
                lock (_syncObj)
                {
                    IsDisposed = true;
                    allBlocksReturned = _blocks.Count == 0;
                    if (!allBlocksReturned && !_allowLateReturn)
                    {
                        MemoryPoolThrowHelper.ThrowInvalidOperationException_DisposingPoolWithActiveBlocks(
                            _totalBlocks - _blocks.Count, _totalBlocks);
                    }

                    if (_blockAccessExceptions.Any())
                    {
                        throw CreateAccessExceptions();
                    }
                }
            }
            finally
            {
                if (allBlocksReturned)
                {
                    SetAllBlocksReturned();
                }
            }
        }

        private void SetAllBlocksReturned()
        {
            if (_blockAccessExceptions.Any())
            {
                _allBlocksRetuned.SetException(CreateAccessExceptions());
            }
            else
            {
                _allBlocksRetuned.SetResult(null);
            }
        }

        private AggregateException CreateAccessExceptions()
        {
            return new AggregateException("Exceptions occurred while accessing blocks", _blockAccessExceptions.ToArray());
        }
    }
}
