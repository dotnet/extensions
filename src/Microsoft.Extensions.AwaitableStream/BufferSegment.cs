using System;

namespace Microsoft.Extensions.AwaitableStream
{
    public class BufferSegment
    {
        public ArraySegment<byte> Buffer;
        public bool Owned;

        public BufferSegment Next;

        public int End => Buffer.Offset + Buffer.Count;
    }
}
