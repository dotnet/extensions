using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AwaitableStream.Internal;

namespace Microsoft.Extensions.AwaitableStream
{
    /// <summary>
    /// Meant to be used with CopyToAsync for bufferless reads
    /// </summary>
    public class AwaitableStream : Stream
    {
        private static readonly Action _completed = () => { };

        // The list of unconsumed buffers
        private BufferSegment _head;
        private BufferSegment _tail;

        // The buffer that represents the current write operation
        private ArraySegment<byte> _currentWrite;

        private Action _continuation;
        private CancellationTokenRegistration _registration;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        // Set when the first read happens
        private TaskCompletionSource<object> _initialRead = new TaskCompletionSource<object>();
        private Gate _readWaiting = new Gate();

        // Set when this stream is disposed
        private TaskCompletionSource<object> _producing = new TaskCompletionSource<object>();

        // Set when consumed is called during the continuation
        private bool _consumeCalled;

        internal bool HasData => _producing.Task.IsCompleted;

        public Task Completion => _producing.Task;

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token).Token;
            // Already cancelled to just throw
            cancellation.ThrowIfCancellationRequested();

            // Cancel the WriteAsync if the provided CancellationToken is fired.
            if (cancellationToken.CanBeCanceled && _registration == default(CancellationTokenRegistration))
            {
                // We can register the very first time write is called since the same token is passed into
                // CopyToAsync
                _registration = cancellationToken.Register(state => ((AwaitableStream)state).Cancel(), this);
            }

            // Wait for the first read operation.
            // This is important because the call to write async wants to call the continuation directly
            // so that the continuation can consume the buffers directly without worrying about where
            // ownership lies. Once the call to WriteAsync returns, the caller owns the buffer so it can't
            // be stashed away without copying.
            await _initialRead.Task;

            // TODO: If segment we're appending to is owned consider appending data into that segment rather
            // than adding a new node.
            // We need to measure the difference in copying versus using the exising buffer for that
            // scenario.

            var data = new ArraySegment<byte>(buffer, offset, count);

            if (_head == null)
            {
                // The list is empty, we just store the current write
                _currentWrite = data;
            }
            else
            {
                // Otherwise add this segment to the end of the list
                var segment = new BufferSegment();
                segment.Buffer = data;
                _tail.Next = segment;
                _tail = segment;
            }

            // Call the continuation
            Complete();

            // Wait for the next read
            await _readWaiting;
            Debug.Assert(!_readWaiting.IsCompleted, "The gate didn't close behind us!");

            // Check that we haven't been cancelled
            cancellation.ThrowIfCancellationRequested();

            if (!_consumeCalled)
            {
                // Call it on the user's behalf
                Consumed(count);
            }

            // Reset the state
            _consumeCalled = false;
        }

        public StreamAwaitable ReadAsync() => new StreamAwaitable(this);

        /// <summary>
        /// Tell the awaitable stream how many bytes were consumed. This needs to be called from
        /// the continuation.
        /// </summary>
        /// <param name="count">Number of bytes consumed by the continuation</param>
        public void Consumed(int count)
        {
            _consumeCalled = true;

            // We didn't consume everything
            if (count < _currentWrite.Count)
            {
                // Make a list with the buffer in it and mark the right bytes as consumed
                if (_head == null)
                {
                    _head = new BufferSegment();
                    _head.Buffer = _currentWrite;
                    _tail = _head;
                }
            }
            else if (_head == null)
            {
                // We consumed everything and there was no list
                _currentWrite = default(ArraySegment<byte>);
                return;
            }

            var segment = _head;
            var segmentOffset = segment.Buffer.Offset;

            while (count > 0)
            {
                var consumed = Math.Min(segment.Buffer.Count, count);

                count -= consumed;
                segmentOffset += consumed;

                if (segmentOffset == segment.End && _head != _tail)
                {
                    // Move to the next node
                    segment = segment.Next;
                    segmentOffset = segment.Buffer.Offset;
                }

                // End of the list stop
                if (_head == _tail)
                {
                    break;
                }
            }

            // Reset the head to the unconsumed buffer
            _head = segment;
            _head.Buffer = new ArraySegment<byte>(segment.Buffer.Array, segmentOffset, segment.End - segmentOffset);

            // Loop from head to tail and copy unconsumed data into buffers we own, this
            // is important because after the call the WriteAsync returns, the stream can reuse these
            // buffers for anything else
            int length = 0;

            segment = _head;
            while (true)
            {
                if (!segment.Owned)
                {
                    length += segment.Buffer.Count;
                }

                if (segment == _tail)
                {
                    break;
                }

                segment = segment.Next;
            }

            // This can happen for 2 reasons:
            // 1. We consumed everything
            // 2. We own all the buffers with data, so no need to copy again.
            if (length == 0)
            {
                return;
            }

            // REVIEW: Use array pool here?
            // Possibly use fixed size blocks here and just fill them so we can avoid a byte[] per call to write
            var buffer = new byte[length];

            // This loop does 2 things
            // 1. Finds the first owned buffer in the list
            // 2. Copies data into the buffer we just allocated
            BufferSegment owned = null;
            segment = _head;
            var offset = 0;

            while (true)
            {
                if (!segment.Owned)
                {
                    Buffer.BlockCopy(segment.Buffer.Array, segment.Buffer.Offset, buffer, offset, segment.Buffer.Count);
                    offset += segment.Buffer.Count;
                }
                else if (owned == null)
                {
                    owned = segment;
                }

                if (segment == _tail)
                {
                    break;
                }

                segment = segment.Next;
            }

            var data = new BufferSegment
            {
                Buffer = new ArraySegment<byte>(buffer),
                Owned = true
            };

            // We didn't own anything in the backlog so replace the entire list
            // with the same data, but into buffers we own
            if (owned == null)
            {
                _head = data;
            }
            else
            {
                // Otherwise append the new data to the Next of the first owned block
                owned.Next = data;
            }

            // Update tail to point to data
            _tail = data;
        }

        protected override void Dispose(bool disposing)
        {
            // Tell the consumer we're done
            if (_producing.TrySetResult(null))
            {
                // Open the read waiting gate
                _readWaiting.Open();

                // Trigger the callback so user code can react to this state change
                Complete();
            }

            _registration.Dispose();

            // Cancel all ongoing/future writes
            _cancellationTokenSource.Cancel();
        }

        public void Cancel()
        {
            // Tell the consumer we're cancelled
            if (_producing.TrySetCanceled())
            {
                // Open the read waiting gate
                _readWaiting.Open();

                // Trigger the callback so user code can react to this state change
                Complete();
            }

            // Cancel all ongoing/future writes
            _cancellationTokenSource.Cancel();
        }

        internal void OnCompleted(Action continuation)
        {
            if (_continuation == _completed ||
                Interlocked.CompareExchange(ref _continuation, continuation, null) == _completed)
            {
                continuation();
            }

            // For the first read, we open the _initialRead TCS, but NOT the readWaiting gate since we want that to block
            // until the second read.
            if(!_initialRead.TrySetResult(null))
            {
                // If we're here, it means initialRead was already RanToCompletion, so we should open the ReadWaiting gate instead.
                _readWaiting.Open();
            }
        }

        private void Complete()
        {
            (_continuation ?? Interlocked.CompareExchange(ref _continuation, _completed, null))?.Invoke();
        }

        internal ByteBuffer GetBuffer()
        {
            _continuation = null;

            if (_head == null)
            {
                return new ByteBuffer(_currentWrite);
            }

            return new ByteBuffer(_head, _tail);
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }

}
