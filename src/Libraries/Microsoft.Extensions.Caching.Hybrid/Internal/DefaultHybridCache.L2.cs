// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

internal partial class DefaultHybridCache
{
    [SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Manual sync check")]
    [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "Manual sync check")]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Explicit async exception handling")]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Deliberate recycle only on success")]
    internal ValueTask<BufferChunk> GetFromL2Async(string key, CancellationToken token)
    {
        switch (GetFeatures(CacheFeatures.BackendCache | CacheFeatures.BackendBuffers))
        {
            case CacheFeatures.BackendCache: // legacy byte[]-based
                var pendingLegacy = _backendCache!.GetAsync(key, token);

#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                if (!pendingLegacy.IsCompletedSuccessfully)
#else
                if (pendingLegacy.Status != TaskStatus.RanToCompletion)
#endif
                {
                    return new(AwaitedLegacyAsync(pendingLegacy, this));
                }

                return new(GetValidPayloadSegment(pendingLegacy.Result)); // already complete

            case CacheFeatures.BackendCache | CacheFeatures.BackendBuffers: // IBufferWriter<byte>-based
                RecyclableArrayBufferWriter<byte> writer = RecyclableArrayBufferWriter<byte>.Create(MaximumPayloadBytes);
                var cache = Unsafe.As<IBufferDistributedCache>(_backendCache!); // type-checked already

                var pendingBuffers = cache.TryGetAsync(key, writer, token);
                if (!pendingBuffers.IsCompletedSuccessfully)
                {
                    return new(AwaitedBuffersAsync(pendingBuffers, writer));
                }

                BufferChunk result = pendingBuffers.GetAwaiter().GetResult()
                    ? new(writer.DetachCommitted(out var length), length, returnToPool: true)
                    : default;
                writer.Dispose(); // it is not accidental that this isn't "using"; avoid recycling if not 100% sure what happened
                return new(result);
        }

        return default; // treat as a "miss"

        static async Task<BufferChunk> AwaitedLegacyAsync(Task<byte[]?> pending, DefaultHybridCache @this)
        {
            var bytes = await pending.ConfigureAwait(false);
            return @this.GetValidPayloadSegment(bytes);
        }

        static async Task<BufferChunk> AwaitedBuffersAsync(ValueTask<bool> pending, RecyclableArrayBufferWriter<byte> writer)
        {
            BufferChunk result = await pending.ConfigureAwait(false)
                    ? new(writer.DetachCommitted(out var length), length, returnToPool: true)
                    : default;
            writer.Dispose(); // it is not accidental that this isn't "using"; avoid recycling if not 100% sure what happened
            return result;
        }
    }

    internal ValueTask SetL2Async(string key, in BufferChunk buffer, HybridCacheEntryOptions? options, CancellationToken token)
    {
        Debug.Assert(buffer.Array is not null, "array should be non-null");
        switch (GetFeatures(CacheFeatures.BackendCache | CacheFeatures.BackendBuffers))
        {
            case CacheFeatures.BackendCache: // legacy byte[]-based
                var arr = buffer.Array!;
                if (arr.Length != buffer.Length)
                {
                    // we'll need a right-sized snapshot
                    arr = buffer.ToArray();
                }

                return new(_backendCache!.SetAsync(key, arr, GetOptions(options), token));
            case CacheFeatures.BackendCache | CacheFeatures.BackendBuffers: // ReadOnlySequence<byte>-based
                var cache = Unsafe.As<IBufferDistributedCache>(_backendCache!); // type-checked already
                return cache.SetAsync(key, buffer.AsSequence(), GetOptions(options), token);
        }

        return default;
    }

    internal void SetL1<T>(string key, CacheItem<T> value, HybridCacheEntryOptions? options)
    {
        // incr ref-count for the the cache itself; this *may* be released via the NeedsEvictionCallback path
        if (value.TryReserve())
        {
            // based on CacheExtensions.Set<TItem>, but with post-eviction recycling

            // intentionally use manual Dispose rather than "using"; confusingly, it is Dispose()
            // that actually commits the add - so: if we fault, we don't want to try
            // committing a partially configured cache entry
            ICacheEntry cacheEntry = _localCache.CreateEntry(key);
            cacheEntry.AbsoluteExpirationRelativeToNow = options?.LocalCacheExpiration ?? _defaultLocalCacheExpiration;
            cacheEntry.Value = value;

            if (value.TryGetSize(out var size))
            {
                cacheEntry = cacheEntry.SetSize(size);
            }

            if (value.NeedsEvictionCallback)
            {
                cacheEntry = cacheEntry.RegisterPostEvictionCallback(CacheItem.SharedOnEviction);
            }

            // commit
            cacheEntry.Dispose();
        }
    }

    private BufferChunk GetValidPayloadSegment(byte[]? payload)
    {
        if (payload is not null)
        {
            if (payload.Length > MaximumPayloadBytes)
            {
                ThrowPayloadLengthExceeded(payload.Length);
            }

            return new(payload);
        }

        return default;
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowPayloadLengthExceeded(int size) // splitting the exception bits out to a different method
    {
        // also add via logger when possible
        throw new InvalidOperationException($"Maximum cache length ({MaximumPayloadBytes} bytes) exceeded");
    }

#if NET8_0_OR_GREATER
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "False positive from unsafe accessor")]
#endif
    private DistributedCacheEntryOptions GetOptions(HybridCacheEntryOptions? options)
    {
        DistributedCacheEntryOptions? result = null;
        if (options is not null && options.Expiration.HasValue && options.Expiration.GetValueOrDefault() != _defaultExpiration)
        {
            result = ToDistributedCacheEntryOptions(options);
        }

        return result ?? _defaultDistributedCacheExpiration;

#if NET8_0_OR_GREATER
        // internal method memoizes this allocation; since it is "init", it is immutable (outside reflection)
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(ToDistributedCacheEntryOptions))]
        extern static DistributedCacheEntryOptions? ToDistributedCacheEntryOptions(HybridCacheEntryOptions options);
#else
        // without that helper method, we'll just eat the alloc (down-level TFMs)
        static DistributedCacheEntryOptions ToDistributedCacheEntryOptions(HybridCacheEntryOptions options)
            => new() { AbsoluteExpirationRelativeToNow = options.Expiration };
#endif
    }
}
