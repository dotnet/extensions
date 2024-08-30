// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Extensions.Caching.Hybrid.Internal.DefaultHybridCache;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

internal partial class DefaultHybridCache
{
    internal sealed class StampedeState<TState, T> : StampedeState
    {
        [DoesNotReturn]
        private static CacheItem<T> ThrowUnexpectedCacheItem() => throw new InvalidOperationException("Unexpected cache item");

        private readonly TaskCompletionSource<CacheItem<T>>? _result;
        private TState? _state;
        private Func<TState, CancellationToken, ValueTask<T>>? _underlying; // main data factory
        private HybridCacheEntryOptions? _options;
        private Task<T>? _sharedUnwrap; // allows multiple non-cancellable callers to share a single task (when no defensive copy needed)

        // ONLY set the result, without any other side-effects
        internal void SetResultDirect(CacheItem<T> value)
            => _result?.TrySetResult(value);

        public StampedeState(DefaultHybridCache cache, in StampedeKey key, bool canBeCanceled)
            : base(cache, key, CacheItem<T>.Create(), canBeCanceled)
        {
            _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public StampedeState(DefaultHybridCache cache, in StampedeKey key, CancellationToken token)
            : base(cache, key, CacheItem<T>.Create(), token)
        {
            // no TCS in this case - this is for SetValue only
        }

        public override Type Type => typeof(T);

        public void QueueUserWorkItem(in TState state, Func<TState, CancellationToken, ValueTask<T>> underlying, HybridCacheEntryOptions? options)
        {
            Debug.Assert(_underlying is null, "should not already have factory field");
            Debug.Assert(underlying is not null, "factory argument should be meaningful");

            // initialize the callback state
            _state = state;
            _underlying = underlying;
            _options = options;

#if NETCOREAPP3_0_OR_GREATER
            ThreadPool.UnsafeQueueUserWorkItem(this, false);
#else
            ThreadPool.UnsafeQueueUserWorkItem(SharedWaitCallback, this);
#endif
        }

        [SuppressMessage("Resilience", "EA0014:The async method doesn't support cancellation", Justification = "Cancellation is handled separately via SharedToken")]
        public Task ExecuteDirectAsync(in TState state, Func<TState, CancellationToken, ValueTask<T>> underlying, HybridCacheEntryOptions? options)
        {
            Debug.Assert(_underlying is null, "should not already have factory field");
            Debug.Assert(underlying is not null, "factory argument should be meaningful");

            // initialize the callback state
            _state = state;
            _underlying = underlying;
            _options = options;

            return BackgroundFetchAsync();
        }

        public override void Execute() => _ = BackgroundFetchAsync();

        public override void SetCanceled() => _result?.TrySetCanceled(SharedToken);

        [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "Custom task management")]
        public ValueTask<T> JoinAsync(CancellationToken token)
        {
            // If the underlying has already completed, and/or our local token can't cancel: we
            // can simply wrap the shared task; otherwise, we need our own cancellation state.
            return token.CanBeCanceled && !Task.IsCompleted ? WithCancellationAsync(this, token) : UnwrapReservedAsync();

            static async ValueTask<T> WithCancellationAsync(StampedeState<TState, T> stampede, CancellationToken token)
            {
                var cancelStub = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                using var reg = token.Register(static obj =>
                {
                    _ = ((TaskCompletionSource<bool>)obj!).TrySetResult(true);
                }, cancelStub);

                CacheItem<T> result;
                try
                {
                    var first = await System.Threading.Tasks.Task.WhenAny(stampede.Task, cancelStub.Task).ConfigureAwait(false);
                    if (ReferenceEquals(first, cancelStub.Task))
                    {
                        // we expect this to throw, because otherwise we wouldn't have gotten here
                        token.ThrowIfCancellationRequested(); // get an appropriate exception
                    }

                    Debug.Assert(ReferenceEquals(first, stampede.Task), "should not be cancelled");

                    // this has already completed, but we'll get the stack nicely
                    result = await stampede.Task.ConfigureAwait(false);
                }
                catch
                {
                    stampede.CancelCaller();
                    throw;
                }

                // outside the catch, so we know we only decrement one way or the other
                return result.GetReservedValue();
            }
        }

        [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "Reliability")]
        public Task<CacheItem<T>> Task
        {
            get
            {
                Debug.Assert(_result is not null, "result should be assigned");
                return _result is null ? InvalidAsync() : _result.Task;

                static Task<CacheItem<T>> InvalidAsync() => System.Threading.Tasks.Task.FromException<CacheItem<T>>(
                    new InvalidOperationException("Task should not be accessed for non-shared instances"));
            }
        }

        [SuppressMessage("Resilience", "EA0014:The async method doesn't support cancellation", Justification = "No cancellable operation")]
        [SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Checked manual unwrap")]
        [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "Checked manual unwrap")]
        [SuppressMessage("Major Code Smell", "S1121:Assignments should not be made from within sub-expressions", Justification = "Unusual, but legit here")]
        internal ValueTask<T> UnwrapReservedAsync()
        {
            var task = Task;
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (task.IsCompletedSuccessfully)
#else
            if (task.Status == TaskStatus.RanToCompletion)
#endif
            {
                return new(task.Result.GetReservedValue());
            }

            // if the type is immutable, callers can share the final step too (this may leave dangling
            // reservation counters, but that's OK)
            var result = ImmutableTypeCache<T>.IsImmutable ? (_sharedUnwrap ??= AwaitedAsync(Task)) : AwaitedAsync(Task);
            return new(result);

            static async Task<T> AwaitedAsync(Task<CacheItem<T>> task)
                => (await task.ConfigureAwait(false)).GetReservedValue();
        }

        [SuppressMessage("Resilience", "EA0014:The async method doesn't support cancellation", Justification = "In this case the cancellation token is provided internally via SharedToken")]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception is passed through to faulted task result")]
        private async Task BackgroundFetchAsync()
        {
            try
            {
                // read from L2 if appropriate
                if ((Key.Flags & HybridCacheEntryFlags.DisableDistributedCacheRead) == 0)
                {
                    var result = await Cache.GetFromL2Async(Key.Key, SharedToken).ConfigureAwait(false);

                    if (result.Array is not null)
                    {
                        SetResultAndRecycleIfAppropriate(ref result);
                        return;
                    }
                }

                // nothing from L2; invoke the underlying data store
                if ((Key.Flags & HybridCacheEntryFlags.DisableUnderlyingData) == 0)
                {
                    // invoke the callback supplied by the caller
                    var newValue = await _underlying!(_state!, SharedToken).ConfigureAwait(false);

                    // If we're writing this value *anywhere*, we're going to need to serialize; this is obvious
                    // in the case of L2, but we also need it for L1, because MemoryCache might be enforcing
                    // SizeLimit (we can't know - it is an abstraction), and for *that* we need to know the item size.
                    // Likewise, if we're writing to a MutableCacheItem, we'll be serializing *anyway* for the payload.
                    //
                    // Rephrasing that: the only scenario in which we *do not* need to serialize is if:
                    // - it is an ImmutableCacheItem
                    // - we're writing neither to L1 nor L2

                    const HybridCacheEntryFlags DisableL1AndL2 = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite;
                    var cacheItem = CacheItem;
                    bool skipSerialize = cacheItem is ImmutableCacheItem<T> && (Key.Flags & DisableL1AndL2) == DisableL1AndL2;

                    if (skipSerialize)
                    {
                        SetImmutableResultWithoutSerialize(newValue);
                    }
                    else if (cacheItem.TryReserve())
                    {
                        // ^^^ The first thing we need to do is make sure we're not getting into a thread race over buffer disposal.
                        // In particular, if this cache item is somehow so short-lived that the buffers would be released *before* we're
                        // done writing them to L2, which happens *after* we've provided the value to consumers.
                        var writer = RecyclableArrayBufferWriter<byte>.Create(MaximumPayloadBytes); // note this lifetime spans the SetL2Async
                        var serializer = Cache.GetSerializer<T>();
                        serializer.Serialize(newValue, writer);
                        BufferChunk buffer = new(writer.DetachCommitted(out var length), length, returnToPool: true); // remove buffer ownership from the writer
                        writer.Dispose(); // we're done with the writer

                        // protect "buffer" (this is why we "reserved"); we don't want SetResult to nuke our local
                        var snapshot = buffer;
                        SetResultPreSerialized(newValue, ref snapshot, serializer);

                        // Note that at this point we've already released most or all of the waiting callers. Everything
                        // from this point onwards happens in the background, from the perspective of the calling code.

                        // Write to L2 if appropriate.
                        if ((Key.Flags & HybridCacheEntryFlags.DisableDistributedCacheWrite) == 0)
                        {
                            // We already have the payload serialized, so this is trivial to do.
                            await Cache.SetL2Async(Key.Key, in buffer, _options, SharedToken).ConfigureAwait(false);
                        }

                        // Release our hook on the CacheItem (only really important for "mutable").
                        _ = cacheItem.Release();

                        // Finally, recycle whatever was left over from SetResultPreSerialized; using "snapshot"
                        // here is NOT a typo; if SetResultPreSerialized left this value alone (immutable), then
                        // this is our recycle step; if SetResultPreSerialized transferred ownership to the (mutable)
                        // CacheItem, then this becomes a no-op, and the buffer only gets fully recycled when the
                        // CacheItem itself is fully clear.
                        snapshot.RecycleIfAppropriate();
                    }
                    else
                    {
                        throw new InvalidOperationException("Internal HybridCache failure: unable to reserve cache item to assign result");
                    }
                }
                else
                {
                    // can't read from data store; implies we shouldn't write
                    // back to anywhere else, either
                    SetDefaultResult();
                }
            }
            catch (Exception ex)
            {
                SetException(ex);
            }
        }

        private void SetException(Exception ex)
        {
            if (_result is not null)
            {
                Cache.RemoveStampedeState(in Key);
                _ = _result.TrySetException(ex);
            }
        }

        private void SetDefaultResult()
        {
            // note we don't store this dummy result in L1 or L2
            if (_result is not null)
            {
                Cache.RemoveStampedeState(in Key);
                _ = _result.TrySetResult(ImmutableCacheItem<T>.GetReservedShared());
            }
        }

        private void SetResultAndRecycleIfAppropriate(ref BufferChunk value)
        {
            // set a result from L2 cache
            Debug.Assert(value.Array is not null, "expected buffer");

            var serializer = Cache.GetSerializer<T>();
            CacheItem<T> cacheItem;
            switch (CacheItem)
            {
                case ImmutableCacheItem<T> immutable:
                    // deserialize; and store object; buffer can be recycled now
                    immutable.SetValue(serializer.Deserialize(new(value.Array!, 0, value.Length)), value.Length);
                    value.RecycleIfAppropriate();
                    cacheItem = immutable;
                    break;
                case MutableCacheItem<T> mutable:
                    // use the buffer directly as the backing in the cache-item; do *not* recycle now
                    mutable.SetValue(ref value, serializer);
                    mutable.DebugOnlyTrackBuffer(Cache);
                    cacheItem = mutable;
                    break;
                default:
                    cacheItem = ThrowUnexpectedCacheItem();
                    break;
            }

            SetResult(cacheItem);
        }

        private void SetImmutableResultWithoutSerialize(T value)
        {
            // set a result from a value we calculated directly
            CacheItem<T> cacheItem;
            switch (CacheItem)
            {
                case ImmutableCacheItem<T> immutable:
                    // no serialize needed
                    immutable.SetValue(value, size: -1);
                    cacheItem = immutable;
                    break;
                default:
                    cacheItem = ThrowUnexpectedCacheItem();
                    break;
            }

            SetResult(cacheItem);
        }

        private void SetResultPreSerialized(T value, ref BufferChunk buffer, IHybridCacheSerializer<T> serializer)
        {
            // set a result from a value we calculated directly that
            // has ALREADY BEEN SERIALIZED (we can optionally consume this buffer)
            CacheItem<T> cacheItem;
            switch (CacheItem)
            {
                case ImmutableCacheItem<T> immutable:
                    // no serialize needed
                    immutable.SetValue(value, size: buffer.Length);
                    cacheItem = immutable;

                    // (but leave the buffer alone)
                    break;
                case MutableCacheItem<T> mutable:
                    mutable.SetValue(ref buffer, serializer);
                    mutable.DebugOnlyTrackBuffer(Cache);
                    cacheItem = mutable;
                    break;
                default:
                    cacheItem = ThrowUnexpectedCacheItem();
                    break;
            }

            SetResult(cacheItem);
        }

        private void SetResult(CacheItem<T> value)
        {
            if ((Key.Flags & HybridCacheEntryFlags.DisableLocalCacheWrite) == 0)
            {
                Cache.SetL1(Key.Key, value, _options); // we can do this without a TCS, for SetValue
            }

            if (_result is not null)
            {
                Cache.RemoveStampedeState(in Key);
                _ = _result.TrySetResult(value);
            }
        }
    }
}
