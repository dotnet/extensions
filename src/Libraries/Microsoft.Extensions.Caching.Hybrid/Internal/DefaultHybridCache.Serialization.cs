// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

internal partial class DefaultHybridCache
{
    // Per instance cache of typed serializers; each serializer is a
    // IHybridCacheSerializer<T> for the corresponding Type, but we can't
    // know which here - and undesirable to add an artificial non-generic
    // IHybridCacheSerializer base that serves no other purpose.
    private readonly ConcurrentDictionary<Type, object> _serializers = new();

    internal int MaximumPayloadBytes { get; }

    internal IHybridCacheSerializer<T> GetSerializer<T>()
    {
        return _serializers.TryGetValue(typeof(T), out var serializer)
            ? Unsafe.As<IHybridCacheSerializer<T>>(serializer) : ResolveAndAddSerializer(this);

        static IHybridCacheSerializer<T> ResolveAndAddSerializer(DefaultHybridCache @this)
        {
            // It isn't critical that we get only one serializer instance during start-up; what matters
            // is that we don't get a new serializer instance *every time*.
            var serializer = @this._services.GetService<IHybridCacheSerializer<T>>();
            if (serializer is null)
            {
                foreach (var factory in @this._serializerFactories)
                {
                    if (factory.TryCreateSerializer<T>(out var current))
                    {
                        serializer = current;
                        break; // we've already reversed the factories, so: the first hit is what we want
                    }
                }
            }

            if (serializer is null)
            {
                throw new InvalidOperationException($"No {nameof(IHybridCacheSerializer<T>)} configured for type '{typeof(T).Name}'");
            }

            // store the result so we don't repeat this in future
            @this._serializers[typeof(T)] = serializer;
            return serializer;
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Intentional for logged failure mode")]
    private bool TrySerialize<T>(T value, out BufferChunk buffer, out IHybridCacheSerializer<T>? serializer)
    {
        // note: also returns the serializer we resolved, because most-any time we want to serialize, we'll also want
        // to make sure we use that same instance later (without needing to re-resolve and/or store the entire HC machinery)

        RecyclableArrayBufferWriter<byte>? writer = null;
        buffer = default;
        try
        {
            writer = RecyclableArrayBufferWriter<byte>.Create(MaximumPayloadBytes); // note this lifetime spans the SetL2Async
            serializer = GetSerializer<T>();

            serializer.Serialize(value, writer);

            buffer = new(writer.DetachCommitted(out var length), 0, length, returnToPool: true); // remove buffer ownership from the writer
            writer.Dispose(); // we're done with the writer
            return true;
        }
        catch (Exception ex)
        {
            bool knownCause = false;

            // ^^^ if we know what happened, we can record directly via cause-specific events
            // and treat as a handled failure (i.e. return false) - otherwise, we'll bubble
            // the fault up a few layers *in addition to* logging in a failure event

            if (writer is not null)
            {
                if (writer.QuotaExceeded)
                {
                    _logger.MaximumPayloadBytesExceeded(ex, MaximumPayloadBytes);
                    knownCause = true;
                }

                writer.Dispose();
            }

            if (!knownCause)
            {
                _logger.SerializationFailure(ex);
                throw;
            }

            buffer = default;
            serializer = null;
            return false;
        }
    }
}
