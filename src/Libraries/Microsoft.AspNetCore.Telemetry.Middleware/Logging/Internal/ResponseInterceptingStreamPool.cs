// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging;

internal static class ResponseInterceptingStreamPool
{
    private static readonly StreamResponseBodyFeature _dummyBodyFeature = new(Stream.Null);
    private static readonly Stream _dummyStream = Stream.Null;
    private static readonly BufferWriter<byte> _dummyBufferWriter = new();

    private static ObjectPool<ResponseInterceptingStream> StreamPool { get; }
        = PoolFactory.CreatePool<ResponseInterceptingStream>();

    private static ObjectPool<BufferWriter<byte>> BufferWriterPool { get; } = Microsoft.Shared.Pools.BufferWriterPool.SharedBufferWriterPool;

    public static ResponseInterceptingStream Get(IHttpResponseBodyFeature innerBodyFeature, int limit)
    {
        var instance = StreamPool.Get();
        var bufferWriter = BufferWriterPool.Get();

        instance.InnerBodyFeature = innerBodyFeature;
        instance.InterceptedStream = innerBodyFeature.Stream;
        instance.InterceptedValueWriteLimit = limit;
        instance.InterceptedValueBuffer = bufferWriter;

        return instance;
    }

    public static void Return(ResponseInterceptingStream stream)
    {
        stream.InnerBodyFeature = _dummyBodyFeature;
        stream.InterceptedStream = _dummyStream;
        stream.InterceptedValueWriteLimit = 0;

        BufferWriterPool.Return(stream.InterceptedValueBuffer);
        stream.InterceptedValueBuffer = _dummyBufferWriter;

        StreamPool.Return(stream);
    }
}
