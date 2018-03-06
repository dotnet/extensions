// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO.Pipelines;

namespace System.Buffers
{
    /// <summary>
    /// Block tracking object used by the byte buffer memory pool. A slab is a large allocation which is divided into smaller blocks. The
    /// individual blocks are then treated as independent array segments.
    /// </summary>
    internal class MemoryPoolBlock : OwnedMemory<byte>
    {
        private readonly int _offset;
        private readonly int _length;
        private int _referenceCount;
        private bool _disposed;

        /// <summary>
        /// This object cannot be instantiated outside of the static Create method
        /// </summary>
        protected MemoryPoolBlock(SlabMemoryPool pool, MemoryPoolSlab slab, int offset, int length)
        {
            _offset = offset;
            _length = length;

            Pool = pool;
            Slab = slab;
        }

        /// <summary>
        /// Back-reference to the memory pool which this block was allocated from. It may only be returned to this pool.
        /// </summary>
        public SlabMemoryPool Pool { get; }

        /// <summary>
        /// Back-reference to the slab from which this block was taken, or null if it is one-time-use memory.
        /// </summary>
        public MemoryPoolSlab Slab { get; }

        public override int Length => _length;

        public override Span<byte> Span
        {
            get
            {
                if (IsDisposed) ThrowHelper.ThrowObjectDisposedException(ExceptionArgument.MemoryPoolBlock);
                return new Span<byte>(Slab.Array, _offset, _length);
            }
        }

#if BLOCK_LEASE_TRACKING
        public bool IsLeased { get; set; }
        public string Leaser { get; set; }
#endif

        ~MemoryPoolBlock()
        {
            if (Slab != null && Slab.IsActive)
            {
#if DEBUG
                Debug.Assert(false, $"{Environment.NewLine}{Environment.NewLine}*** Block being garbage collected instead of returned to pool" +
#if BLOCK_LEASE_TRACKING
                    $": {Leaser}" +
#endif
                    $" ***{ Environment.NewLine}");
#endif

                // Need to make a new object because this one is being finalized
                Pool.Return(new MemoryPoolBlock(Pool, Slab, _offset, _length));
            }
        }

        internal static MemoryPoolBlock Create(
            int offset,
            int length,
            SlabMemoryPool pool,
            MemoryPoolSlab slab)
        {
            return new MemoryPoolBlock(pool, slab, offset, length);
        }

        protected void OnZeroReferences()
        {
            Pool.Return(this);
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
        }

        public override void Retain()
        {
            while (true)
            {
                int currentCount = Volatile.Read(ref _referenceCount);
                if (currentCount <= 0) ThrowHelper.ThrowObjectDisposedException(ExceptionArgument.MemoryPoolBlock);
                if (Interlocked.CompareExchange(ref _referenceCount, currentCount + 1, currentCount) == currentCount) break;
            }
        }

        public override bool Release()
        {
            while (true)
            {
                int currentCount = Volatile.Read(ref _referenceCount);
                if (currentCount <= 0) ThrowHelper.ThrowInvalidOperationException_ReferenceCountZero();
                if (Interlocked.CompareExchange(ref _referenceCount, currentCount - 1, currentCount) == currentCount)
                {
                    if (currentCount == 1)
                    {
                        OnZeroReferences();
                        return false;
                    }
                    return true;
                }
            }
        }

        protected override bool IsRetained => _referenceCount > 0;
        public override bool IsDisposed => _disposed;

        // In kestrel both MemoryPoolBlock and OwnedMemory end up in the same assembly so
        // this method access modifiers need to be `protected internal`
        protected override bool TryGetArray(out ArraySegment<byte> arraySegment)
        {
            if (IsDisposed) ThrowHelper.ThrowObjectDisposedException(ExceptionArgument.MemoryPoolBlock);
            arraySegment = new ArraySegment<byte>(Slab.Array, _offset, _length);
            return true;
        }

        public override MemoryHandle Pin(int byteOffset = 0)
        {
            Retain();   // checks IsDisposed
            if (byteOffset < 0 || byteOffset > _length) ThrowHelper.ThrowArgumentOutOfRangeException(_length, byteOffset);
            unsafe
            {
                return new MemoryHandle(this, (Slab.NativePointer + _offset + byteOffset).ToPointer());
            }
        }
    }
}
