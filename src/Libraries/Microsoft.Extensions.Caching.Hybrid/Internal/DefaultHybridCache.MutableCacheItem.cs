// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

internal partial class DefaultHybridCache
{
    private sealed partial class MutableCacheItem<T> : CacheItem<T> // used to hold types that require defensive copies
    {
        private IHybridCacheSerializer<T> _serializer = null!; // deferred until SetValue
        private BufferChunk _buffer;

        public override bool NeedsEvictionCallback => _buffer.ReturnToPool;

        public override bool DebugIsImmutable => false;

        public void SetValue(ref BufferChunk buffer, IHybridCacheSerializer<T> serializer)
        {
            _serializer = serializer;
            _buffer = buffer;
            buffer = default; // we're taking over the lifetime; the caller no longer has it!
        }

        public override bool TryGetValue(out T value)
        {
            // only if we haven't already burned
            if (TryReserve())
            {
                try
                {
                    value = _serializer.Deserialize(_buffer.AsSequence());
                    return true;
                }
                finally
                {
                    _ = Release();
                }
            }

            value = default!;
            return false;
        }

        public override bool TryGetSize(out long size)
        {
            // only if we haven't already burned
            if (TryReserve())
            {
                size = _buffer.Length;
                _ = Release();
                return true;
            }

            size = 0;
            return false;
        }

        public override bool TryReserveBuffer(out BufferChunk buffer)
        {
            // only if we haven't already burned
            if (TryReserve())
            {
                buffer = _buffer.DoNotReturnToPool(); // not up to them!
                return true;
            }

            buffer = default;
            return false;
        }

        protected override void OnFinalRelease()
        {
            DebugOnlyDecrementOutstandingBuffers();
            _buffer.RecycleIfAppropriate();
        }
    }
}
