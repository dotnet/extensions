// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

internal sealed class HttpRequestBufferProvider : ILoggingBufferProvider
{
    private readonly GlobalBufferProvider _globalBufferProvider;
    private readonly IHttpContextAccessor _accessor;
    private readonly ConcurrentDictionary<string, HttpRequestBuffer> _requestBuffers = new();

    public HttpRequestBufferProvider(GlobalBufferProvider globalBufferProvider, IHttpContextAccessor accessor)
    {
        _globalBufferProvider = globalBufferProvider;
        _accessor = accessor;
    }

    public ILoggingBuffer CurrentBuffer => _accessor.HttpContext is null
                ? _globalBufferProvider.CurrentBuffer
                : _requestBuffers.GetOrAdd(_accessor.HttpContext.TraceIdentifier, _accessor.HttpContext.RequestServices.GetRequiredService<HttpRequestBuffer>());

    // TO DO: Dispose request buffer when the respective HttpContext is disposed
}
#endif
