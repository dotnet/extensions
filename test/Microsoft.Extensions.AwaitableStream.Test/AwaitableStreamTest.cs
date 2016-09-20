using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AwaitableStream
{
    public class AwaitableStreamTest
    {
        [Fact]
        public async Task CanConsumeData()
        {
            var stream = new CallbackStream(async (s, token) =>
            {
                var sw = new StreamWriter(s);
                await sw.WriteAsync("Hello");
                await sw.FlushAsync();
                await sw.WriteAsync("World");
                await sw.FlushAsync();
            });

            var awaitableStream = new AwaitableStream();
            var ignore = Task.Run(async () =>
            {
                using (awaitableStream)
                {
                    await stream.CopyToAsync(awaitableStream);
                }
            });

            int calls = 0;

            while (true)
            {
                var buffer = await awaitableStream.ReadAsync();
                calls++;
                if (buffer.IsEmpty && awaitableStream.Completion.IsCompleted)
                {
                    // Done
                    break;
                }

                var segment = buffer.GetArraySegment();

                var data = Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);
                if (calls == 1)
                {
                    Assert.Equal("Hello", data);
                }
                else
                {
                    Assert.Equal("World", data);
                }

                awaitableStream.Consumed(segment.Count);
            }
        }

        [Fact]
        public async Task CanCancelConsumingData()
        {
            var cts = new CancellationTokenSource();
            var stream = new CallbackStream(async (s, token) =>
            {
                var hello = Encoding.UTF8.GetBytes("Hello");
                var world = Encoding.UTF8.GetBytes("World");
                await s.WriteAsync(hello, 0, hello.Length, token);
                cts.Cancel();
                await s.WriteAsync(world, 0, world.Length, token);
            });

            var awaitableStream = new AwaitableStream();
            var ignore = Task.Run(async () =>
            {
                using (awaitableStream)
                {
                    await stream.CopyToAsync(awaitableStream, 100, cts.Token);
                }
            });

            int calls = 0;

            while (true)
            {
                var buffer = await awaitableStream.ReadAsync();
                calls++;
                if (buffer.IsEmpty && awaitableStream.Completion.IsCompleted)
                {
                    // Done
                    break;
                }

                if (awaitableStream.Completion.IsCanceled)
                {
                    break;
                }

                var segment = buffer.GetArraySegment();

                var data = Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);
                Assert.Equal("Hello", data);

                awaitableStream.Consumed(segment.Count);
            }

            Assert.Equal(2, calls);
            Assert.True(awaitableStream.Completion.IsCanceled);
        }

        [Fact]
        public async Task SameBuffersReceivedIfConsumed()
        {
            var hello = Encoding.UTF8.GetBytes("Hello");
            var world = Encoding.UTF8.GetBytes("World");
            byte[][] buffers = new byte[2][] { hello, world };

            var stream = new CallbackStream(async (s, token) =>
            {
                await s.WriteAsync(hello, 0, hello.Length, token);
                await s.WriteAsync(world, 0, world.Length, token);
            });

            var awaitableStream = new AwaitableStream();
            var ignore = Task.Run(async () =>
            {
                using (awaitableStream)
                {
                    await stream.CopyToAsync(awaitableStream);
                }
            });

            int calls = 0;

            while (true)
            {
                var buffer = await awaitableStream.ReadAsync();
                if (buffer.IsEmpty && awaitableStream.Completion.IsCompleted)
                {
                    // Done
                    break;
                }

                var segment = buffer.GetArraySegment();
                Assert.Same(segment.Array, buffers[calls++]);
            }

            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task CanConsumeLessDataThanProduced()
        {
            var stream = new CallbackStream(async (s, token) =>
            {
                var sw = new StreamWriter(s);
                await sw.WriteAsync("Hello ");
                await sw.FlushAsync();
                await sw.WriteAsync("World");
                await sw.FlushAsync();
            });

            var awaitableStream = new AwaitableStream();
            var ignore = Task.Run(async () =>
            {
                using (awaitableStream)
                {
                    await stream.CopyToAsync(awaitableStream);
                }
            });

            int index = 0;
            var message = "Hello World";

            while (true)
            {
                var buffer = await awaitableStream.ReadAsync();

                if (buffer.IsEmpty && awaitableStream.Completion.IsCompleted)
                {
                    // Done
                    break;
                }

                var segment = buffer.GetArraySegment();
                var ch = (char)segment.Array[segment.Offset];

                Assert.Equal(message[index++], ch);

                awaitableStream.Consumed(1);
            }

            Assert.Equal(message.Length, index);
        }

        [Fact]
        public async Task CanConsumeLessDataThanProducedWithBufferReuse()
        {
            var stream = new CallbackStream(async (s, token) =>
            {
                var data = new byte[4096];
                Encoding.UTF8.GetBytes("Hello ", 0, 6, data, 0);
                await s.WriteAsync(data, 0, 6);
                Encoding.UTF8.GetBytes("World", 0, 5, data, 0);
                await s.WriteAsync(data, 0, 5);
            });

            var awaitableStream = new AwaitableStream();
            var ignore = Task.Run(async () =>
            {
                using (awaitableStream)
                {
                    await stream.CopyToAsync(awaitableStream);
                }
            });

            int index = 0;
            var message = "Hello World";

            while (true)
            {
                var buffer = await awaitableStream.ReadAsync();

                if (buffer.IsEmpty && awaitableStream.Completion.IsCompleted)
                {
                    // Done
                    break;
                }

                var segment = buffer.GetArraySegment();
                var ch = (char)segment.Array[segment.Offset];

                Assert.Equal(message[index++], ch);

                awaitableStream.Consumed(1);
            }

            Assert.Equal(message.Length, index);
        }

        [Fact]
        public async Task NotCallingConsumeWillConsumeDataAutomatically()
        {
            var stream = new CallbackStream(async (s, token) =>
            {
                var sw = new StreamWriter(s);
                await sw.WriteAsync("Hello");
                await sw.FlushAsync();
                await sw.WriteAsync("World");
                await sw.FlushAsync();
            });

            var awaitableStream = new AwaitableStream();
            var ignore = Task.Run(async () =>
            {
                using (awaitableStream)
                {
                    await stream.CopyToAsync(awaitableStream);
                }
            });

            int calls = 0;

            while (true)
            {
                var buffer = await awaitableStream.ReadAsync();
                calls++;
                if (buffer.IsEmpty && awaitableStream.Completion.IsCompleted)
                {
                    // Done
                    break;
                }

                var segment = buffer.GetArraySegment();

                var data = Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);
                if (calls == 1)
                {
                    Assert.Equal("Hello", data);
                }
                else
                {
                    Assert.Equal("World", data);
                }
            }
            Assert.Equal(3, calls);
        }

        [Fact]
        public async Task CanAwaitDuringReadAsyncCallbackBeforeConsuming()
        {
            var stream = new CallbackStream(async (s, token) =>
            {
                var sw = new StreamWriter(s);
                await sw.WriteAsync("Hello");
                await sw.FlushAsync();
                await sw.WriteAsync("World");
                await sw.FlushAsync();
            });

            var awaitableStream = new AwaitableStream();
            var ignore = Task.Run(async () =>
            {
                using (awaitableStream)
                {
                    await stream.CopyToAsync(awaitableStream);
                }
            });

            var buffer = await awaitableStream.ReadAsync();

            await Task.Yield();

            // Consume nothing and read more data
            awaitableStream.Consumed(0);
            buffer = await awaitableStream.ReadAsync();

            // The buffer should have everything, since we consumed nothing
            var array = buffer.GetArraySegment();
            var actualStr = Encoding.UTF8.GetString(array.Array, array.Offset, array.Count);
            Assert.Equal("HelloWorld", actualStr);
        }

        [Fact]
        public async Task DisposingTheAwaitableStreamCancelsAnActiveWrite()
        {
            var cancelledTcs = new TaskCompletionSource<object>();

            var stream = new CallbackStream(async (s, token) =>
            {
                try
                {
                    var sw = new StreamWriter(s);
                    await sw.WriteAsync("Hello");
                    await sw.FlushAsync();
                    await sw.WriteAsync("World");
                    await sw.FlushAsync();
                    cancelledTcs.SetException(new Exception("CallbackStream reached the end of the callback!"));
                }
                catch (OperationCanceledException)
                {
                    cancelledTcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    cancelledTcs.SetException(ex);
                }
            });

            using (var awaitableStream = new AwaitableStream())
            {
                var ignore = Task.Run(async () =>
                {
                    using (awaitableStream)
                    {
                        await stream.CopyToAsync(awaitableStream);
                    }
                });

                var buffer = await awaitableStream.ReadAsync();

                // Now dispose of the stream, which should throw OperationCanceledException in the CallbackStream above
            }


            var completedTask = await Task.WhenAny(cancelledTcs.Task, Task.Delay(100));
            Assert.True(ReferenceEquals(cancelledTcs.Task, completedTask), "Timeout elapsed");
        }

        private class CallbackStream : Stream
        {
            private readonly Func<Stream, CancellationToken, Task> _callback;
            public CallbackStream(Func<Stream, CancellationToken, Task> callback)
            {
                _callback = callback;
            }

            public override bool CanRead
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool CanSeek
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool CanWrite
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override long Length
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                return _callback(destination, cancellationToken);
            }
        }
    }
}
