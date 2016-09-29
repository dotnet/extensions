using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AwaitableStream.Internal
{
    /// <summary>
    /// Simple awaitable gate - intended to synchronize a single producer with a single consumer to ensure the producer doesn't
    /// produce until the consumer is ready. Similar to a <see cref="TaskCompletionSource{TResult}"/> but reusable so we don't have
    /// to keep allocating new ones every time.
    /// </summary>
    /// <remarks>
    /// The gate can be in one of two states: "Open", indicating that an await will immediately return and "Closed", meaning that an await
    /// will block until the gate is opened. The gate is initially "Closed" and can be opened by a call to <see cref="Open"/>. Upon the completion
    /// of an await, it will automatically return to the "Closed" state (this is done in the <see cref="GetResult"/> call that is injected by the
    /// compiler's async/await logic).
    /// </remarks>
    public class Gate : ICriticalNotifyCompletion
    {
        private static readonly Action _completed = () => {};

        private volatile Action _continuation;

        /// <summary>
        /// Returns a boolean indicating if the gate is "open"
        /// </summary>
        public bool IsCompleted => _continuation == _completed;

        public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

        public void OnCompleted(Action continuation)
        {
            // If we're already completed, call the continuation immediately
            if (_continuation == _completed)
            {
                continuation();
            }
            else
            {
                // Otherwise, if the current continuation is null, atomically store the new continuation in the field and return the old value
                var previous = Interlocked.CompareExchange(ref _continuation, continuation, null);
                if (previous == _completed)
                {
                    // It got completed in the time between the previous the method and the cmpexch.
                    // So call the continuation (the value of _continuation will remain _completed because cmpexch is atomic,
                    // so we didn't accidentally replace it).
                    continuation();
                }
            }
        }

        /// <summary>
        /// Resets the gate to continue blocking the waiter. This is called immediately after awaiting the signal.
        /// </summary>
        public void GetResult()
        {
            // Clear the active continuation to "reset" the state of this event
            Interlocked.Exchange(ref _continuation, null);
        }

        /// <summary>
        /// Set the gate to allow the waiter to continue.
        /// </summary>
        public void Open()
        {
            // Set the stored continuation value to a sentinel that indicates the state is completed, then call the previous value.
            var completion = Interlocked.Exchange(ref _continuation, _completed);
            completion?.Invoke();
        }

        public Gate GetAwaiter() => this;
    }
}
