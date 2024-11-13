// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#if DEBUG
using System.Threading;
#endif

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

internal partial class DefaultHybridCache
{
    /// <summary>
    /// Auxiliary API for testing purposes, allowing confirmation of the internal state independent of the public API.
    /// </summary>
    internal bool DebugTryGetCacheItem(string key, [NotNullWhen(true)] out CacheItem? value)
    {
        if (_localCache.TryGetValue(key, out var untyped) && untyped is CacheItem typed)
        {
            value = typed;
            return true;
        }

        value = null;
        return false;
    }

#if DEBUG // enable ref-counted buffers

    private int _outstandingBufferCount;

    internal int DebugOnlyGetOutstandingBuffers(bool flush = false)
                => flush ? Interlocked.Exchange(ref _outstandingBufferCount, 0) : Volatile.Read(ref _outstandingBufferCount);

    [Conditional("DEBUG")]
    internal void DebugOnlyDecrementOutstandingBuffers()
    {
        _ = Interlocked.Decrement(ref _outstandingBufferCount);
    }

    [Conditional("DEBUG")]
    internal void DebugOnlyIncrementOutstandingBuffers()
    {
        _ = Interlocked.Increment(ref _outstandingBufferCount);
    }
#endif

    private partial class MutableCacheItem<T>
    {
#if DEBUG
        private DefaultHybridCache? _cache; // for buffer-tracking - only needed in DEBUG
#endif

        [Conditional("DEBUG")]
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance state used in debug")]
        internal void DebugOnlyTrackBuffer(DefaultHybridCache cache)
        {
#if DEBUG
            _cache = cache;
            if (_buffer.ReturnToPool)
            {
                _cache?.DebugOnlyIncrementOutstandingBuffers();
            }
#endif
        }

        [Conditional("DEBUG")]
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance state used in debug")]
        private void DebugOnlyDecrementOutstandingBuffers()
        {
#if DEBUG
            if (_buffer.ReturnToPool)
            {
                _cache?.DebugOnlyDecrementOutstandingBuffers();
            }
#endif
        }
    }
}
