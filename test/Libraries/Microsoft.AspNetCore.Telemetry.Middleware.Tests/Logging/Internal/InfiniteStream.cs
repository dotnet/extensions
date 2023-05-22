// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging.Test.Internal;

internal sealed class InfiniteStream : Stream
{
    private readonly byte _charToFill;

    public InfiniteStream(char charToFill)
    {
        _charToFill = Convert.ToByte(charToFill);
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => long.MaxValue;

    public override long Position { get; set; }

    public override void Flush()
    {
        // empty by design
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        for (int i = 0; i < count; i++)
        {
            buffer[i + offset] = _charToFill;
        }

        return count;
    }

    public override long Seek(long offset, SeekOrigin origin)
        => offset;

    public override void SetLength(long value)
        => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();
}
