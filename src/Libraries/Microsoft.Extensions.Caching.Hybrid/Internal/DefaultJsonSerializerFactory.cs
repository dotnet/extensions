// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

internal sealed class DefaultJsonSerializerFactory : IHybridCacheSerializerFactory
{
    internal static JsonSerializerOptions FieldEnabledJsonOptions { get; } = new() { IncludeFields = true };

    internal JsonSerializerOptions Options { get; }

    public DefaultJsonSerializerFactory([FromKeyedServices(typeof(HybridCache))] JsonSerializerOptions? options = null)
    {
#pragma warning disable IDE0079 // unnecessary suppression: TFM-dependent
#pragma warning disable IL2026, IL3050 // AOT bits
        Options = options ?? JsonSerializerOptions.Default;
#pragma warning restore IL2026, IL3050
#pragma warning restore IDE0079
    }

    public bool TryCreateSerializer<T>([NotNullWhen(true)] out IHybridCacheSerializer<T>? serializer)
    {
        // no restriction - accept any type

        var options = Options;
        if (IsValueTuple(typeof(T)) && !options.IncludeFields)
        {
            // value-tuples expose fields, not properties; special-case this as a common scenario
            options = FieldEnabledJsonOptions;
        }

        serializer = new DefaultJsonSerializer<T>(options);
        return true;
    }

    private static bool IsValueTuple(Type type)
        => type.IsValueType && (type.FullName ?? string.Empty).StartsWith("System.ValueTuple`", StringComparison.Ordinal);

    internal sealed class DefaultJsonSerializer<T> : IHybridCacheSerializer<T>
    {
        internal JsonSerializerOptions Options { get; }

        public DefaultJsonSerializer(JsonSerializerOptions options)
        {
            Options = options;
        }

#pragma warning disable IDE0079 // unnecessary suppression: TFM-dependent
#pragma warning disable IL2026, IL3050 // AOT bits
        T IHybridCacheSerializer<T>.Deserialize(ReadOnlySequence<byte> source)
        {
            var reader = new Utf8JsonReader(source);
            return JsonSerializer.Deserialize<T>(ref reader, Options)!;

        }

        void IHybridCacheSerializer<T>.Serialize(T value, IBufferWriter<byte> target)
        {
            using var writer = new Utf8JsonWriter(target);

            JsonSerializer.Serialize<T>(writer, value, Options);
        }
#pragma warning restore IL2026, IL3050
#pragma warning restore IDE0079
    }

}
