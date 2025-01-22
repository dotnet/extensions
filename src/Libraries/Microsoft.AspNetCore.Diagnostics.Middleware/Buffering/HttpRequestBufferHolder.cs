// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

internal sealed class HttpRequestBufferHolder
{
    private readonly ConcurrentDictionary<string, ILoggingBuffer> _buffers = new();

    public ILoggingBuffer GetOrAdd(string category, Func<string, ILoggingBuffer> valueFactory) =>
        _buffers.GetOrAdd(category, valueFactory);

    public void Flush()
    {
        foreach (var buffer in _buffers.Values)
        {
            buffer.Flush();
        }
    }
}
