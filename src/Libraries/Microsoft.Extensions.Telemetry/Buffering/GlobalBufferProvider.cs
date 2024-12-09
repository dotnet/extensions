// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
namespace Microsoft.Extensions.Logging;

internal sealed class GlobalBufferProvider : ILoggingBufferProvider
{
    private readonly GlobalBuffer _buffer;

    public GlobalBufferProvider(GlobalBuffer buffer)
    {
        _buffer = buffer;
    }

    public ILoggingBuffer CurrentBuffer => _buffer;
}
#endif
