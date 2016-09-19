using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.AwaitableStream
{
    public struct StreamAwaitable : ICriticalNotifyCompletion
    {
        private readonly AwaitableStream _channel;

        public StreamAwaitable(AwaitableStream channel)
        {
            _channel = channel;
        }
        public bool IsCompleted => _channel.HasData;

        public ByteBuffer GetResult() => _channel.GetBuffer();

        public StreamAwaitable GetAwaiter() => this;

        public void OnCompleted(Action continuation) => _channel.OnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }
    }
}
