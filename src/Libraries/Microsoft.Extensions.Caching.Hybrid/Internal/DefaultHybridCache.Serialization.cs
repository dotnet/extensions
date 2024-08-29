// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Reflection;
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
}
