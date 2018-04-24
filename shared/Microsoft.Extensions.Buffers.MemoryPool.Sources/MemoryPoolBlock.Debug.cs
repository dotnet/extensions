// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG

using System.Threading;
using System.Diagnostics;

namespace System.Buffers
{
    /// <summary>
    /// Block tracking object used by the byte buffer memory pool. A slab is a large allocation which is divided into smaller blocks. The
    /// individual blocks are then treated as independent array segments.
    /// </summary>
    internal sealed class MemoryPoolBlock : MemoryManager<byte>
    {
        /// <summary>
        /// Back-reference to the memory pool which this block was allocated from. It may only be returned to this pool.
        /// </summary>
        private readonly SlabMemoryPool _pool;

        /// <summary>
        /// Back-reference to the slab from which this block was taken, or null if it is one-time-use memory.
        /// </summary>
        private readonly MemoryPoolSlab _slab;

        private readonly int _offset;
        private readonly int _length;

        private int _pinCount;

        /// <summary>
        /// This object cannot be instantiated outside of the static Create method
        /// </summary>
        internal MemoryPoolBlock(SlabMemoryPool pool, MemoryPoolSlab slab, int offset, int length)
        {
            _pool = pool;
            _slab = slab;

            _offset = offset;
            _length = length;
        }

        public override Memory<byte> Memory
        {
            get
            {
                if (!_slab.IsActive)
                {
                    MemoryPoolThrowHelper.ThrowObjectDisposedException(MemoryPoolThrowHelper.ExceptionArgument.MemoryPoolBlock);
                }

                return CreateMemory(_length);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (Volatile.Read(ref _pinCount) > 0)
            {
                MemoryPoolThrowHelper.ThrowInvalidOperationException_ReturningPinnedBlock();
            }

            _pool.Return(this);

            // Let finalizer to run in _pool.Return so we don't crash
            // process with Debug.Assert
            if (!_slab.IsActive)
            {
                MemoryPoolThrowHelper.ThrowInvalidOperationException_BlockReturnedToDisposedPool();
            }
        }

        public override Span<byte> GetSpan() => new Span<byte>(_slab.Array, _offset, _length);

        public override MemoryHandle Pin(int byteOffset = 0)
        {
            if (!_slab.IsActive)
            {
                MemoryPoolThrowHelper.ThrowObjectDisposedException(MemoryPoolThrowHelper.ExceptionArgument.MemoryPoolBlock);
            }

            if (byteOffset < 0 || byteOffset > _length)
            {
                MemoryPoolThrowHelper.ThrowArgumentOutOfRangeException(_length, byteOffset);
            }

            Interlocked.Increment(ref _pinCount);

            unsafe
            {
                return new MemoryHandle((_slab.NativePointer + _offset + byteOffset).ToPointer(), default, this);
            }
        }

        protected override bool TryGetArray(out ArraySegment<byte> segment)
        {
            segment = new ArraySegment<byte>(_slab.Array, _offset, _length);
            return true;
        }

        public override void Unpin()
        {
            if (Interlocked.Decrement(ref _pinCount) < 0)
            {
                MemoryPoolThrowHelper.ThrowInvalidOperationException_PinCountZero();
            }
        }

#if BLOCK_LEASE_TRACKING
        public bool IsLeased { get; set; }
        public string Leaser { get; set; }

        public void Lease()
        {
            Leaser = Environment.StackTrace;
            IsLeased = true;
        }
#else

        public void Lease()
        {
        }
#endif

        ~MemoryPoolBlock()
        {
            if (_slab != null && _slab.IsActive)
            {
                Debug.Assert(false, $"{Environment.NewLine}{Environment.NewLine}*** Block being garbage collected instead of returned to pool" +
#if BLOCK_LEASE_TRACKING
                                    $": {Leaser}" +
#endif
                                    $" ***{ Environment.NewLine}");
            }
        }
    }
}

#endif