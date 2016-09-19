using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.AwaitableStream
{
    public struct ByteBuffer
    {
        private readonly BufferSegment _head;
        private readonly BufferSegment _tail;
        private readonly ArraySegment<byte> _data;

        public bool IsEmpty => _head == _tail && (_head?.Buffer.Offset == _head?.End);

        private bool IsSingleBuffer => _head == _tail;

        public int Length
        {
            get
            {
                // TODO: Cache
                int length = 0;
                var segment = _head;

                if (segment == null)
                {
                    return _data.Count;
                }

                while (true)
                {
                    length += segment.Buffer.Count;
                    if (segment == _tail)
                    {
                        break;
                    }
                }
                return length;
            }
        }

        public ByteBuffer(ArraySegment<byte> data)
        {
            _data = data;
            _head = null;
            _tail = null;
        }

        public ByteBuffer(BufferSegment head, BufferSegment tail)
        {
            _data = default(ArraySegment<byte>);
            _head = head;
            _tail = tail;
        }

        public int IndexOf(byte data)
        {
            return IndexOf(data, 0);
        }

        // TODO: Support start
        public int IndexOf(byte value, int start)
        {
            var segment = _head;

            if (segment == null)
            {
                // Just return the data directly
                return Array.IndexOf(_data.Array, value, _data.Offset, _data.Count);
            }

            int count = 0;

            while (true)
            {
                int index = Array.IndexOf(segment.Buffer.Array, value, segment.Buffer.Offset, segment.Buffer.Count);

                if (index == -1)
                {
                    count += segment.Buffer.Count;
                }
                else
                {
                    count += index;
                    return count;
                }

                if (segment == _tail)
                {
                    break;
                }

                segment = segment.Next;
            }

            return -1;
        }

        public ByteBuffer Slice(int offset, int length)
        {
            return default(ByteBuffer);
        }

        public ArraySegment<byte> GetArraySegment()
        {
            if (_head == null)
            {
                return _data;
            }

            List<ArraySegment<byte>> buffers = null;
            var length = 0;

            foreach (var span in this)
            {
                if (IsSingleBuffer)
                {
                    return span;
                }
                else
                {
                    if (buffers == null)
                    {
                        buffers = new List<ArraySegment<byte>>();
                    }
                    buffers.Add(span);
                    length += span.Count;
                }
            }

            var data = new byte[length];
            int offset = 0;
            foreach (var span in buffers)
            {
                Buffer.BlockCopy(span.Array, span.Offset, data, offset, span.Count);
                offset += span.Count;
            }

            return new ArraySegment<byte>(data, 0, length);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_head, _tail, _data);
        }

        public struct Enumerator : IEnumerator<ArraySegment<byte>>
        {
            private BufferSegment _head;
            private readonly BufferSegment _tail;
            private ArraySegment<byte> _current;
            private ArraySegment<byte> _data;
            private int _offset;

            public Enumerator(BufferSegment head, BufferSegment tail, ArraySegment<byte> data)
            {
                _head = head;
                _tail = tail;
                _current = default(ArraySegment<byte>);
                _data = data;
                _offset = head?.Buffer.Offset ?? data.Offset;
            }

            public ArraySegment<byte> Current => _current;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_head == null)
                {
                    if (_data.Array != null)
                    {
                        _current = _data;
                        _data = default(ArraySegment<byte>);
                        return true;
                    }

                    return false;
                }

                if (_head == _tail && _offset == (_tail.Buffer.Offset + _tail.Buffer.Count))
                {
                    return false;
                }

                _current = _head.Buffer;

                if (_head != _tail)
                {
                    _head = _head.Next;
                }
                else
                {
                    _offset = _tail.Buffer.Offset + _tail.Buffer.Count;
                }

                return true;
            }

            public void Reset()
            {

            }
        }
    }
}
