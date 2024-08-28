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
        [Conditional("DEBUG")]
        internal void DebugOnlyTrackBuffer(DefaultHybridCache cache) => DebugOnlyTrackBufferCore(cache);

        [SuppressMessage("Minor Code Smell", "S4136:Method overloads should be grouped together", Justification = "Conditional partial declaration/usage")]
        [SuppressMessage("Minor Code Smell", "S3251:Implementations should be provided for \"partial\" methods", Justification = "Intentional debug-only API")]
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Uses instance state in debug build")]
        partial void DebugOnlyDecrementOutstandingBuffers();

        [SuppressMessage("Minor Code Smell", "S4136:Method overloads should be grouped together", Justification = "Conditional partial declaration/usage")]
        [SuppressMessage("Minor Code Smell", "S3251:Implementations should be provided for \"partial\" methods", Justification = "Intentional debug-only API")]
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Uses instance state in debug build")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Uses parameter in debug build")]
        partial void DebugOnlyTrackBufferCore(DefaultHybridCache cache);

#if DEBUG
        private DefaultHybridCache? _cache; // for buffer-tracking - only enabled in DEBUG
        partial void DebugOnlyDecrementOutstandingBuffers()
        {
            if (_buffer.ReturnToPool)
            {
                _cache?.DebugOnlyDecrementOutstandingBuffers();
            }
        }

        partial void DebugOnlyTrackBufferCore(DefaultHybridCache cache)
        {
            _cache = cache;
            if (_buffer.ReturnToPool)
            {
                _cache?.DebugOnlyIncrementOutstandingBuffers();
            }
        }
#endif
    }
}
