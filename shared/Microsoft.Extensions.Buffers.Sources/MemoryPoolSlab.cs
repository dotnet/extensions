// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Threading;

namespace System.Buffers
{
    /// <summary>
    /// Slab tracking object used by the byte buffer memory pool. A slab is a large allocation which is divided into smaller blocks. The
    /// individual blocks are then treated as independant array segments.
    /// </summary>
    internal class MemoryPoolSlab : IDisposable
    {
        /// <summary>
        /// This handle pins the managed array in memory until the slab is disposed. This prevents it from being
        /// relocated and enables any subsections of the array to be used as native memory pointers to P/Invoked API calls.
        /// </summary>
        private readonly GCHandle _gcHandle;
        private readonly IntPtr _nativePointer;
        private byte[] _data;

        private bool _isActive;
        private bool _disposedValue;
        private int _ownedBlocks;

        public MemoryPoolSlab(byte[] data)
        {
            _data = data;
            _gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            _nativePointer = _gcHandle.AddrOfPinnedObject();
            _isActive = true;
        }

        /// <summary>
        /// True as long as the blocks from this slab are to be considered returnable to the pool. In order to shrink the
        /// memory pool size an entire slab must be removed. That is done by (1) setting IsActive to false and removing the
        /// slab from the pool's _slabs collection, (2) as each block currently in use is Return()ed to the pool it will
        /// be allowed to be garbage collected rather than re-pooled, and (3) when all block tracking objects are garbage
        /// collected and the slab is no longer references the slab will be garbage collected and the memory unpinned will
        /// be unpinned by the slab's Dispose.
        /// </summary>
        public bool IsActive => _isActive;

        public IntPtr NativePointer => _nativePointer;

        public byte[] Array => _data;

        public int Length => _data.Length;

        internal int OwnedBlocks
        {
            set
            {
                _ownedBlocks = value;
            }
        }

        public static MemoryPoolSlab Create(int length)
        {
            // allocate and pin requested memory length
            var array = new byte[length];

            // allocate and return slab tracking object
            return new MemoryPoolSlab(array);
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // N/A: dispose managed state (managed objects).
                }

                _isActive = false;

                _disposedValue = true;
            }
        }

        internal void Return(MemoryPoolBlock block)
        {
#if BLOCK_LEASE_TRACKING
            Debug.Assert(block.Slab == this, "Returned block was not this slab");
            Debug.Assert(block.IsLeased, $"Block being returned to pool twice: {block.Leaser}{Environment.NewLine}");
            block.IsLeased = false;
#endif
            GC.SuppressFinalize(block);

            var remaining = Interlocked.Decrement(ref _ownedBlocks);

            if (remaining < 0)
            {
                ThrowHelper.ThrowObjectDisposedException(ExceptionArgument.MemoryPoolBlock);
            }
            else if (remaining == 0)
            {
                // No more blocks outstanding, unpin

                if (_gcHandle.IsAllocated)
                {
                    _gcHandle.Free();
                }

                // set large fields to null.
                _data = null;
            }
        }

        // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~MemoryPoolSlab()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
    }
}
