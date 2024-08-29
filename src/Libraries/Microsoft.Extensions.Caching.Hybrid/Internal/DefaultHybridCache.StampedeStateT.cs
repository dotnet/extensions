// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

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
                    var cacheItem = SetResult(await _underlying!(_state!, SharedToken).ConfigureAwait(false));

                    // note that at this point we've already released most or all of the waiting callers; everything
                    // else here is background

                    // write to L2 if appropriate
                    if ((Key.Flags & HybridCacheEntryFlags.DisableDistributedCacheWrite) == 0)
                    {
                        if (cacheItem.TryReserveBuffer(out var buffer))
                        {
                            // mutable: we've already serialized it for the shared cache item
                            await Cache.SetL2Async(Key.Key, in buffer, _options, SharedToken).ConfigureAwait(false);
                            _ = cacheItem.Release(); // because we reserved
                        }
                        else if (cacheItem.TryGetValue(out var value))
                        {
                            // immutable: we'll need to do the serialize ourselves
                            var writer = RecyclableArrayBufferWriter<byte>.Create(MaximumPayloadBytes); // note this lifetime spans the SetL2Async
                            Cache.GetSerializer<T>().Serialize(value, writer);
                            buffer = new(writer.GetBuffer(out var length), length, returnToPool: false); // writer still owns the buffer
                            await Cache.SetL2Async(Key.Key, in buffer, _options, SharedToken).ConfigureAwait(false);
                            writer.Dispose(); // recycle on success
                        }
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
                    immutable.SetValue(serializer.Deserialize(new(value.Array!, 0, value.Length)));
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

        private CacheItem<T> SetResult(T value)
        {
            // set a result from a value we calculated directly
            CacheItem<T> cacheItem;
            switch (CacheItem)
            {
                case ImmutableCacheItem<T> immutable:
                    // no serialize needed
                    immutable.SetValue(value);
                    cacheItem = immutable;
                    break;
                case MutableCacheItem<T> mutable:
                    // serialization happens here
                    mutable.SetValue(value, Cache.GetSerializer<T>(), MaximumPayloadBytes);
                    mutable.DebugOnlyTrackBuffer(Cache);
                    cacheItem = mutable;
                    break;
                default:
                    cacheItem = ThrowUnexpectedCacheItem();
                    break;
            }

            SetResult(cacheItem);
            return cacheItem;
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
