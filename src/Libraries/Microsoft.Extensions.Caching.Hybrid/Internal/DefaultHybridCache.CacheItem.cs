// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

internal partial class DefaultHybridCache
{
    internal abstract class CacheItem
    {
        private readonly long _creationTimestamp;

        protected CacheItem(long creationTimestamp, TagSet tags)
        {
            Tags = tags;
            _creationTimestamp = creationTimestamp;
        }

        private int _refCount = 1; // the number of pending operations against this cache item

        public abstract bool DebugIsImmutable { get; }

        public long CreationTimestamp => _creationTimestamp;

        public TagSet Tags { get; }

        // Note: the ref count is the number of callers anticipating this value at any given time. Initially,
        // it is one for a simple "get the value" flow, but if another call joins with us, it'll be incremented.
        // If either cancels, it will get decremented, with the entire flow being cancelled if it ever becomes
        // zero.
        // This counter also drives cache lifetime, with the cache itself incrementing the count by one. In the
        // case of mutable data, cache eviction may reduce this to zero (in cooperation with any concurrent readers,
        // who increment/decrement around their fetch), allowing safe buffer recycling.

        internal int RefCount => Volatile.Read(ref _refCount);

        internal void UnsafeSetCreationTimestamp(long timestamp)
            => Unsafe.AsRef(in _creationTimestamp) = timestamp;

        internal static readonly PostEvictionDelegate SharedOnEviction = static (key, value, reason, state) =>
        {
            if (value is CacheItem item)
            {
                _ = item.Release();
            }
        };

        public virtual bool NeedsEvictionCallback => false; // do we need to call Release when evicted?

        public abstract bool TryReserveBuffer(out BufferChunk buffer);

        /// <summary>
        /// Signal that the consumer is done with this item (ref-count decr).
        /// </summary>
        /// <returns>True if this is the final release.</returns>
        public bool Release()
        {
            int newCount = Interlocked.Decrement(ref _refCount);
            Debug.Assert(newCount >= 0, "over-release detected");
            if (newCount == 0)
            {
                // perform per-item clean-up, i.e. buffer recycling (if defensive copies needed)
                OnFinalRelease();
                return true;
            }

            return false;
        }

        public bool TryReserve()
        {
            // This is basically interlocked increment, but with a check against:
            // a) incrementing upwards from zero
            // b) overflowing *back* to zero
            int oldValue = Volatile.Read(ref _refCount);
            do
            {
                if (oldValue is 0 or -1)
                {
                    return false; // already burned, or about to roll around back to zero
                }

                var updated = Interlocked.CompareExchange(ref _refCount, oldValue + 1, oldValue);
                if (updated == oldValue)
                {
                    return true; // we exchanged
                }

                oldValue = updated; // we failed, but we have an updated state
            }
            while (true);
        }

        protected virtual void OnFinalRelease() // any required release semantics
        {
        }
    }

    internal abstract class CacheItem<T> : CacheItem
    {
        protected CacheItem(long creationTimestamp, TagSet tags)
            : base(creationTimestamp, tags)
        {
        }

        public abstract bool TryGetSize(out long size);

        // Attempt to get a value that was *not* previously reserved.
        // Note on ILogger usage: we don't want to propagate and store this everywhere.
        // It is used for reporting deserialization problems - pass it as needed.
        // (CacheItem gets into the IMemoryCache - let's minimize the onward reachable set
        // of that cache, by only handing it leaf nodes of a "tree", not a "graph" with
        // backwards access - we can also limit object size at the same time)
        public abstract bool TryGetValue(ILogger log, out T value);

        // get a value that *was* reserved, countermanding our reservation in the process
        public T GetReservedValue(ILogger log)
        {
            if (!TryGetValue(log, out var value))
            {
                Throw();
            }

            _ = Release();
            return value;

            static void Throw() => throw new ObjectDisposedException("The cache item has been recycled before the value was obtained");
        }

        internal static CacheItem<T> Create(long creationTimestamp, TagSet tags) => ImmutableTypeCache<T>.IsImmutable
            ? new ImmutableCacheItem<T>(creationTimestamp, tags) : new MutableCacheItem<T>(creationTimestamp, tags);
    }
}
