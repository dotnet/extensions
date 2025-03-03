// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public class SerializerTests
{
    [Fact]
    public void RoundTripString()
    {
        IHybridCacheSerializer<string> serializer = InbuiltTypeSerializer.Instance;

        using var target = RecyclableArrayBufferWriter<byte>.Create(int.MaxValue);
        serializer.Serialize("test value", target);
        Assert.True("test value"u8.SequenceEqual(target.GetCommittedMemory().Span));
        Assert.Equal("test value", serializer.Deserialize(target.AsSequence()));

        // and deserialize with multi-chunk
        Assert.Equal("test value", serializer.Deserialize(Split(target.AsSequence())));
    }

    [Fact]
    public void RoundTripByteArray()
    {
        IHybridCacheSerializer<byte[]> serializer = InbuiltTypeSerializer.Instance;
        var value = "test value"u8.ToArray();
        using var target = RecyclableArrayBufferWriter<byte>.Create(int.MaxValue);
        serializer.Serialize(value, target);
        Assert.True("test value"u8.SequenceEqual(target.GetCommittedMemory().Span));
        Assert.Equal(value, serializer.Deserialize(target.AsSequence()));

        // and deserialize with multi-chunk
        Assert.Equal(value, serializer.Deserialize(Split(target.AsSequence())));
    }

    [Theory]
    [InlineData(JsonSerializer.None, JsonSerializer.Default)]
    [InlineData(JsonSerializer.CustomGlobal, JsonSerializer.CustomGlobal)]
    [InlineData(JsonSerializer.CustomPerType, JsonSerializer.CustomPerType)]
    [InlineData(JsonSerializer.CustomPerType | JsonSerializer.CustomGlobal, JsonSerializer.CustomPerType)]
    public void RoundTripPoco(JsonSerializer addSerializers, JsonSerializer expectedSerializer)
    {
        var obj = RoundTrip(new MyPoco { X = 42, Y = "abc" }, """{"X":42,"Y":"abc"}"""u8, expectedSerializer, addSerializers);
        Assert.Equal(42, obj.X);
        Assert.Equal("abc", obj.Y);
    }

    [Flags]
    public enum JsonSerializer
    {
        None = 0,
        CustomGlobal = 1 << 0,
        CustomPerType = 1 << 1,
        Default = 1 << 2,
        FieldEnabled = 1 << 3,
    }

    public class MyPoco
    {
        public int X { get; set; }
        public string Y { get; set; } = "";
    }

    [Theory]
    [InlineData(JsonSerializer.None, JsonSerializer.Default)]
    [InlineData(JsonSerializer.CustomGlobal, JsonSerializer.CustomGlobal)]
    [InlineData(JsonSerializer.CustomPerType, JsonSerializer.CustomPerType)]
    [InlineData(JsonSerializer.CustomPerType | JsonSerializer.CustomGlobal, JsonSerializer.CustomPerType)]
    public void RoundTripTuple(JsonSerializer addSerializers, JsonSerializer expectedSerializer)
    {
        var obj = RoundTrip(Tuple.Create(42, "abc"), """{"Item1":42,"Item2":"abc"}"""u8, expectedSerializer, addSerializers);
        Assert.Equal(42, obj.Item1);
        Assert.Equal("abc", obj.Item2);
    }

    [Theory]
    [InlineData(JsonSerializer.None, JsonSerializer.FieldEnabled)]
    [InlineData(JsonSerializer.CustomGlobal, JsonSerializer.FieldEnabled)]
    [InlineData(JsonSerializer.CustomPerType, JsonSerializer.FieldEnabled)]
    [InlineData(JsonSerializer.CustomPerType | JsonSerializer.CustomGlobal, JsonSerializer.FieldEnabled)]
    public void RoundTripValueTuple(JsonSerializer addSerializers, JsonSerializer expectedSerializer)
    {
        var obj = RoundTrip((42, "abc"), """{"Item1":42,"Item2":"abc"}"""u8, expectedSerializer, addSerializers);
        Assert.Equal(42, obj.Item1);
        Assert.Equal("abc", obj.Item2);
    }

    [Theory]
    [InlineData(JsonSerializer.None, JsonSerializer.FieldEnabled)]
    [InlineData(JsonSerializer.CustomGlobal, JsonSerializer.FieldEnabled)]
    [InlineData(JsonSerializer.CustomPerType, JsonSerializer.FieldEnabled)]
    [InlineData(JsonSerializer.CustomPerType | JsonSerializer.CustomGlobal, JsonSerializer.FieldEnabled)]
    public void RoundTripNamedValueTuple(JsonSerializer addSerializers, JsonSerializer expectedSerializer)
    {
        var obj = RoundTrip((X: 42, Y: "abc"), """{"Item1":42,"Item2":"abc"}"""u8, expectedSerializer, addSerializers);
        Assert.Equal(42, obj.X);
        Assert.Equal("abc", obj.Y);
    }

    private static T RoundTrip<T>(T value, ReadOnlySpan<byte> expectedBytes, JsonSerializer expectedJsonOptions, JsonSerializer addSerializers = JsonSerializer.None, bool binary = false)
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        JsonSerializerOptions? globalOptions = null;
        JsonSerializerOptions? perTypeOptions = null;

        if ((addSerializers & JsonSerializer.CustomGlobal) != JsonSerializer.None)
        {
            globalOptions = new();
            services.AddKeyedSingleton<JsonSerializerOptions>(typeof(IHybridCacheSerializer<>), globalOptions);
        }

        if ((addSerializers & JsonSerializer.CustomPerType) != JsonSerializer.None)
        {
            perTypeOptions = new();
            services.AddKeyedSingleton<JsonSerializerOptions>(typeof(IHybridCacheSerializer<T>), perTypeOptions);
        }

        JsonSerializerOptions? expectedOptionsObj = expectedJsonOptions switch
        {
            JsonSerializer.Default => JsonSerializerOptions.Default,
            JsonSerializer.FieldEnabled => DefaultJsonSerializerFactory.FieldEnabledJsonOptions,
            JsonSerializer.CustomGlobal => globalOptions,
            JsonSerializer.CustomPerType => perTypeOptions,
            _ => throw new ArgumentOutOfRangeException(nameof(expectedJsonOptions))
        };
        Assert.NotNull(expectedOptionsObj);

        using var provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        var serializer = cache.GetSerializer<T>();

        if (serializer is DefaultJsonSerializerFactory.DefaultJsonSerializer<T> json)
        {
            Assert.Same(expectedOptionsObj, json.Options);
        }

        using var target = RecyclableArrayBufferWriter<byte>.Create(int.MaxValue);
        serializer.Serialize(value, target);
        var actual = target.GetCommittedMemory().Span;
        if (!expectedBytes.IsEmpty && !expectedBytes.SequenceEqual(actual))
        {
            if (!binary)
            {
                Assert.Equal(FormatText(expectedBytes), FormatText(actual));
            }

            Assert.Equal(FormatBytes(expectedBytes), FormatBytes(actual));
        }

        return serializer.Deserialize(target.AsSequence());
    }

    private static string FormatText(ReadOnlySpan<byte> value)
    {
        // not concerned about efficiency - only used in failure case
        return Encoding.UTF8.GetString(value.ToArray());
    }

    private static string FormatBytes(ReadOnlySpan<byte> value)
    {
        // not concerned about efficiency - only used in failure case
        return BitConverter.ToString(value.ToArray());
    }

    private static ReadOnlySequence<byte> Split(ReadOnlySequence<byte> value)
    {
        // ensure the value is a multi-chunk segment
        if (!value.IsSingleSegment || value.Length <= 1)
        {
            // already multiple chunks, or cannot be split
            return value;
        }

        var chunk = value.First; // actually, single

        Segment first = new(chunk.Slice(0, 1), null);
        Segment second = new(chunk.Slice(1), first);
        var result = new ReadOnlySequence<byte>(first, 0, second, chunk.Length - 1);
        Assert.False(result.IsSingleSegment, "should be multi-segment");
        return result;
    }

    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory, Segment? previous)
        {
            if (previous is not null)
            {
                RunningIndex = previous.RunningIndex + previous.Memory.Length;
                previous.Next = this;
            }

            Memory = memory;
        }
    }

}
